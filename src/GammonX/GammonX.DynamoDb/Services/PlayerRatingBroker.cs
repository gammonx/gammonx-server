using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.DynamoDb.Stats;

namespace GammonX.DynamoDb.Services
{
    public static class PlayerRatingBroker
    {
        /// <summary>
        /// Updates the player with the given <paramref name="playerId"/> based on the <paramref name="wonMatch"/> and
        /// <paramref name="lostMatch"/>. The Glicko2 rating mechanism is used for its calculation.
        /// </summary>
        /// <remarks>
        /// The rating is not peristed yet to the database. Instead it just returns the updated rating instance.
        /// Both players must first be evaluated before their rating period item can be committed.
        /// </remarks>
        /// <param name="repo">Repo to operate on.</param>
        /// <param name="playerId">Player rating to update.</param>
        /// <param name="wonMatch">Won match item.</param>
        /// <param name="lostMatch">Lost match item.</param>
        /// <returns>Returns updated player rating item and the related rating period item.</returns>
        public static async Task<(PlayerRatingItem, RatingPeriodItem)> CalculatePlayerRatingAsync(this IDynamoDbRepository repo, Guid playerId, MatchItem wonMatch, MatchItem lostMatch)
        {
            // won and lost match have the same variant, mouds and type
            var variant = wonMatch.Variant;
            var modus = wonMatch.Modus;
            var type = wonMatch.Type;

            var ratingFactory = ItemFactoryCreator.Create<PlayerRatingItem>();
            var sk = string.Format(ratingFactory.SKFormat, variant);

            // we check if the calling player already has a rating for the given variant
            var currentPlayerRating = (await repo.GetItemsAsync<PlayerRatingItem>(playerId, sk)).FirstOrDefault();
            if (currentPlayerRating == null)
            {
                currentPlayerRating = new PlayerRatingItem()
                {
                    PlayerId = playerId,
                    Variant = variant,
                    Modus = modus,
                    Type = type
                };
            }
            var playerGlicko = Glicko2Rating.From(currentPlayerRating);

            // we check if the opponent player already has a rating for the given variant
            var opponentId = wonMatch.PlayerId == playerId ? lostMatch.PlayerId : wonMatch.PlayerId;
            var currentOpponentRating = (await repo.GetItemsAsync<PlayerRatingItem>(opponentId, sk)).FirstOrDefault();
            if (currentOpponentRating == null)
            {
                currentOpponentRating = new PlayerRatingItem()
                {
                    PlayerId = opponentId,
                    Variant = variant,
                    Modus = modus,
                    Type = type
                };
            }

            // we get the last 9 rating periods of the given player
            var ratingPeriodFactory = ItemFactoryCreator.Create<RatingPeriodItem>();
            var sk2 = string.Format(ratingPeriodFactory.SKFormat, variant, type, modus);
            var ratingPeriods = await repo.GetItemsAsync<RatingPeriodItem>(playerId, sk2);
            var lastRatingPeriods = ratingPeriods.OrderBy(rp => rp.CreatedAt).Take(Glicko2Constants.RatingPeriod - 1).ToList();

            // we calculate the match score
            var wonMatchInput = MatchScoreCalculator.From(wonMatch);
            var lostMatchInput = MatchScoreCalculator.From(lostMatch);
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
            // we add the current rating period to the list of last rating periods
            lastRatingPeriods.Add(currentRatingPeriod);

            var updatedPlayerRating = Glicko2RatingCalculator.Calculate(playerGlicko, lastRatingPeriods.ToArray());

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
            var playerItemFactory = ItemFactoryCreator.Create<PlayerItemFactory>();
            var pk = string.Format(playerItemFactory.PKFormat, playerId);
            var sk = playerItemFactory.SKPrefix;

            var deletionTasks = new List<Task<bool>>();

            if (recursive)
            {
                // player ratings
                var playerRatings = await repo.GetItemsAsync<PlayerRatingItem>(playerId);
                foreach (var rating in playerRatings)
                {
                    deletionTasks.Add(repo.DeleteAsync<PlayerRatingItem>(rating.PlayerId));
                }
                // player stats
                var playerStats = await repo.GetItemsAsync<PlayerStatsItem>(playerId);
                foreach (var stats in playerStats)
                {
                    deletionTasks.Add(repo.DeleteAsync<PlayerStatsItem>(stats.PlayerId));
                }
                // we keep the matches, games and their history for some data mining purposes
            }

            // delete player item
            var deletePlayerTask = repo.DeleteAsync<PlayerItem>(playerId);
            deletionTasks.Add(deletePlayerTask);

            await Task.WhenAll(deletionTasks);
        }
    }
}
