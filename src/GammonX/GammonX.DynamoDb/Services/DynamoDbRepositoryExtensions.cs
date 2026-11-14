using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.DynamoDb.Stats;

using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Services
{
    public static class DynamoDbRepositoryExtensions
    {
        /// <summary>
        /// Updates the player with the given <paramref name="playerId"/> based on the <paramref name="wonMatch"/> and
        /// <paramref name="lostMatch"/>. The Glicko2 rating mechanism is used for its calculation.
        /// </summary>
        /// <remarks>
        /// The rating is not persisted yet to the database. Instead it just returns the updated rating instance.
        /// Both players must first be evaluated before their rating period item can be committed.
        /// The returned period retains both players pre-match rating inputs. Only the returned
        /// player rating item contains this matchs updated rating.
        /// For a paired commit, use CalculateMatchRatingsAsync so both results share the same snapshots.
        /// </remarks>
        /// <param name="repo">Repo to operate on.</param>
        /// <param name="playerId">Player rating to update.</param>
        /// <param name="wonMatch">Won match item.</param>
        /// <param name="lostMatch">Lost match item.</param>
        /// <returns>Returns updated player rating item and the related rating period item.</returns>
        public static async Task<PlayerRatingResult> CalculatePlayerRatingAsync(this IDynamoDbRepository repo, Guid playerId, MatchItem wonMatch, MatchItem lostMatch)
        {
            if (playerId != wonMatch.PlayerId && playerId != lostMatch.PlayerId)
            {
                throw new ArgumentException("Player match not found", nameof(playerId));
            }

            var (winner, loser) = await repo.CalculateMatchRatingsAsync(wonMatch, lostMatch);
            return playerId == wonMatch.PlayerId ? winner : loser;
        }

        /// <summary>
        /// Calculates both players updated ratings and pre-match periods from shared rating snapshots.
        /// </summary>
        /// <remarks>
        /// Reads each player once. The caller must atomically commit both results with revision checks
        /// and recalculate the pair after a conflict. Inputs are not mutated. Inactivity is applied to
        /// both snapshots at the same UTC time before either match result is calculated.
        /// </remarks>
        /// <param name="repo">Repository supplying the current ratings.</param>
        /// <param name="wonMatch">Winners completed ranked match.</param>
        /// <param name="lostMatch">Losers record for the same match.</param>
        /// <param name="timeProvider">Clock used for inactivity and update timestamps; defaults to the system clock.</param>
        /// <returns>The winner and loser rating/period pairs, ready for an atomic commit.</returns>
        public static async Task<(PlayerRatingResult Winner, PlayerRatingResult Loser)>CalculateMatchRatingsAsync(
            this IDynamoDbRepository repo, 
            MatchItem wonMatch, 
            MatchItem lostMatch, 
            TimeProvider? timeProvider = null)
        {
            if (wonMatch.Modus != MatchModus.Ranked || lostMatch.Modus != MatchModus.Ranked)
            {
                throw new InvalidOperationException("Player rating updates are only supported for Ranked matches.");
            }
            if (wonMatch.Id != lostMatch.Id || wonMatch.PlayerId == lostMatch.PlayerId
                || wonMatch.Variant != lostMatch.Variant || wonMatch.Type != lostMatch.Type
                || wonMatch.Result != MatchResult.Won || lostMatch.Result != MatchResult.Lost)
            {
                throw new ArgumentException("Rating updates require matching winner and loser records for two distinct players.");
            }

            var ratingFactory = ItemFactoryCreator.Create<PlayerRatingItem>();
            var sk = string.Format(ratingFactory.SKFormat, wonMatch.Variant, wonMatch.Type);
            var winner = await GetRatingSnapshotAsync(repo, wonMatch.PlayerId, sk, wonMatch);
            var loser = await GetRatingSnapshotAsync(repo, lostMatch.PlayerId, sk, wonMatch);
            var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;

            ApplyInactivity(winner, now);
            ApplyInactivity(loser, now);

            return (
                CalculateRating(winner, loser, wonMatch, lostMatch, now),
                CalculateRating(loser, winner, wonMatch, lostMatch, now));
        }

        private static async Task<PlayerRatingItem> GetRatingSnapshotAsync(
            IDynamoDbRepository repo,
            Guid playerId,
            string sk,
            MatchItem match)
        {
            var rating = (await GetRatingItemsAsync<PlayerRatingItem>(repo, playerId, sk)).SingleOrDefault()
                ?? PlayerRatingItemFactory.CreateInitial(playerId, match.Variant, match.Type);

            return CopyRating(rating);
        }

        private static void ApplyInactivity(PlayerRatingItem rating, DateTime now)
        {
            if (rating.LastRatedAt.HasValue)
            {
                var periods = Math.Floor((now - rating.LastRatedAt.Value).TotalDays / Glicko2Constants.InactivityPeriodDays);
                if (periods > 0)
                {
                    // Repeating the no-game Glicko-2 update adds sigma squared once per inactive period.
                    var phi = Glicko2RatingCalculator.ToPhi(rating.RatingDeviation);
                    rating.RatingDeviation = Math.Min(Glicko2Constants.DefaultRD, Glicko2RatingCalculator.FromPhi(Math.Sqrt(phi * phi + periods * rating.Sigma * rating.Sigma)));
                }
            }
        }

        private static PlayerRatingResult CalculateRating(
            PlayerRatingItem player,
            PlayerRatingItem opponent,
            MatchItem wonMatch,
            MatchItem lostMatch,
            DateTime now)
        {
            var currentRatingPeriod = new RatingPeriodItem()
            {
                MatchId = wonMatch.Id,
                MatchScore = MatchScoreCalculator.Calculate(player.PlayerId, wonMatch.From(), lostMatch.From()),
                PlayerId = player.PlayerId,
                OpponentId = opponent.PlayerId,
                Variant = wonMatch.Variant,
                Modus = wonMatch.Modus,
                Type = wonMatch.Type,
                PlayerRating = player.Rating,
                PlayerRatingDeviation = player.RatingDeviation,
                PlayerSigma = player.Sigma,
                OpponentRating = opponent.Rating,
                OpponentRatingDeviation = opponent.RatingDeviation,
                OpponentSigma = opponent.Sigma,
                CreatedAt = now,
            };

            var currentPlayerRating = CopyRating(player);
            var updatedPlayerRating = Glicko2RatingCalculator.Calculate(Glicko2Rating.From(player), currentRatingPeriod);

            // we convert back to some ordinary values
            var newRating = Glicko2RatingCalculator.FromMu(updatedPlayerRating.Mu);
            var newRatingDeviation = Glicko2RatingCalculator.FromPhi(updatedPlayerRating.Phi);

            // we update the player rating
            currentPlayerRating.Rating = newRating;
            currentPlayerRating.RatingDeviation = newRatingDeviation;
            currentPlayerRating.Sigma = updatedPlayerRating.Sigma;

            if (newRating > currentPlayerRating.HighestRating)
            {
                currentPlayerRating.HighestRating = newRating;
            }
            if (newRating < currentPlayerRating.LowestRating)
            {
                currentPlayerRating.LowestRating = newRating;
            }

            // we increase the amount of matches played by 1
            currentPlayerRating.MatchesPlayed += 1;
            currentPlayerRating.Revision += 1;
            currentPlayerRating.LastRatedAt = now;

            return new PlayerRatingResult(currentPlayerRating, currentRatingPeriod);
        }

        private static PlayerRatingItem CopyRating(PlayerRatingItem rating) => new()
        {
            PlayerId = rating.PlayerId,
            Variant = rating.Variant,
            Modus = rating.Modus,
            Type = rating.Type,
            Rating = rating.Rating,
            RatingDeviation = rating.RatingDeviation,
            Sigma = rating.Sigma,
            HighestRating = rating.HighestRating,
            LowestRating = rating.LowestRating,
            MatchesPlayed = rating.MatchesPlayed,
            Revision = rating.Revision,
            LastRatedAt = rating.LastRatedAt
        };

        // <inheritdoc />
        public static async Task DeletePlayerAsync(this IDynamoDbRepository repo, Guid playerId, bool recursive = false)
        {
            if (recursive)
            {
                if (repo is not IDynamoDbBatchWriter batchWriter)
                {
                    throw new InvalidOperationException("Recursive player deletion requires batch-write support.");
                }

                var playerRatings = await repo.GetItemsAsync<PlayerRatingItem>(playerId);
                var playerStats = await repo.GetItemsAsync<PlayerStatsItem>(playerId);
                var ratingPeriods = await repo.GetItemsAsync<RatingPeriodItem>(playerId);
                var playerRatingKeys = playerRatings.Select(item => (PkId: item.PlayerId, Sk: item.SK)).ToList();
                var playerStatsKeys = playerStats.Select(item => (PkId: item.PlayerId, Sk: item.SK)).ToList();
                var ratingPeriodKeys = ratingPeriods.Select(item => (PkId: item.PlayerId, Sk: item.SK)).ToList();

                await Task.WhenAll(
                    batchWriter.BatchDeleteAsync<PlayerRatingItem>(playerRatingKeys),
                    batchWriter.BatchDeleteAsync<PlayerStatsItem>(playerStatsKeys),
                    batchWriter.BatchDeleteAsync<RatingPeriodItem>(ratingPeriodKeys));
            }

            var playerItemFactory = ItemFactoryCreator.Create<PlayerItem>();
            var deleted = await repo.DeleteAsync<PlayerItem>(playerId, playerItemFactory.SKPrefix);
            if (!deleted)
                throw new InvalidOperationException($"Failed to delete player '{playerId}'.");
        }

        private static Task<IEnumerable<T>> GetRatingItemsAsync<T>(IDynamoDbRepository repo, Guid playerId, string sk)
        {
            return repo is IDynamoDbConsistentReader consistentReader
                ? consistentReader.GetItemsConsistentlyAsync<T>(playerId, sk)
                : repo.GetItemsAsync<T>(playerId, sk);
        }
    }
}
