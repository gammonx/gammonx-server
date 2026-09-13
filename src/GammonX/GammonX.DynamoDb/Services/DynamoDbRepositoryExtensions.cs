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
        /// </remarks>
        /// <param name="repo">Repo to operate on.</param>
        /// <param name="playerId">Player rating to update.</param>
        /// <param name="wonMatch">Won match item.</param>
        /// <param name="lostMatch">Lost match item.</param>
        /// <returns>Returns updated player rating item and the related rating period item.</returns>
        public static async Task<(PlayerRatingItem, RatingPeriodItem)> CalculatePlayerRatingAsync(this IDynamoDbRepository repo, Guid playerId, MatchItem wonMatch, MatchItem lostMatch)
        {
            if (wonMatch.Modus != MatchModus.Ranked || lostMatch.Modus != MatchModus.Ranked)
                throw new InvalidOperationException("Player rating updates are only supported for Ranked matches.");

            // won and lost match have the same variant, modus and type
            var variant = wonMatch.Variant;
            var modus = wonMatch.Modus;
            var type = wonMatch.Type;

            var ratingFactory = ItemFactoryCreator.Create<PlayerRatingItem>();
            var sk = string.Format(ratingFactory.SKFormat, variant, type);

            // we check if the calling player already has a rating for the given variant
            var currentPlayerRating = (await repo.GetItemsAsync<PlayerRatingItem>(playerId, sk)).FirstOrDefault();
            if (currentPlayerRating == null)
            {
                currentPlayerRating = PlayerRatingItemFactory.CreateInitial(playerId, variant, type);
            }
            var playerGlicko = Glicko2Rating.From(currentPlayerRating);

            // we check if the opponent player already has a rating for the given variant
            var opponentId = wonMatch.PlayerId == playerId ? lostMatch.PlayerId : wonMatch.PlayerId;
            var currentOpponentRating = (await repo.GetItemsAsync<PlayerRatingItem>(opponentId, sk)).FirstOrDefault();
            if (currentOpponentRating == null)
            {
                currentOpponentRating = PlayerRatingItemFactory.CreateInitial(opponentId, variant, type);
            }

            // we calculate the match score
            var wonMatchInput = wonMatch.From();
            var lostMatchInput = lostMatch.From();
            var matchScore = MatchScoreCalculator.Calculate(playerId, wonMatchInput, lostMatchInput);

            // we create a rating period for the current match
            var currentRatingPeriod = new RatingPeriodItem()
            {
                MatchId = wonMatch.Id,
                MatchScore = matchScore,
                PlayerId = playerId,
                OpponentId = opponentId,
                Variant = variant,
                Modus = modus,
                Type = type,
                PlayerRating = currentPlayerRating.Rating,
                PlayerRatingDeviation = currentPlayerRating.RatingDeviation,
                PlayerSigma = currentPlayerRating.Sigma,
                OpponentRating = currentOpponentRating.Rating,
                OpponentRatingDeviation = currentOpponentRating.RatingDeviation,
                OpponentSigma = currentOpponentRating.Sigma,
                CreatedAt = DateTime.UtcNow,
            };
            
            var updatedPlayerRating = Glicko2RatingCalculator.Calculate(playerGlicko, currentRatingPeriod);

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

            return (currentPlayerRating, currentRatingPeriod);
        }

        // <inheritdoc />
        public static async Task DeletePlayerAsync(this IDynamoDbRepository repo, Guid playerId, bool recursive = false)
        {
            if (recursive)
            {
                if (repo is not IDynamoDbBatchWriter batchWriter)
                    throw new InvalidOperationException("Recursive player deletion requires batch-write support.");

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
    }
}
