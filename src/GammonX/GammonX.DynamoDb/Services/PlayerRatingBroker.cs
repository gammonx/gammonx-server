using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.DynamoDb.Stats;

namespace GammonX.DynamoDb.Services
{
    public static class PlayerRatingBroker
    {
        public static async Task<PlayerRatingItem> UpdatePlayerRatingAsync(this IDynamoDbRepository repo, Guid playerId, MatchItem wonMatch, MatchItem lostMatch)
        {
            // won and lost match have the same variant, mouds and type
            var variant = wonMatch.Variant;
            var modus = wonMatch.Modus;
            var type = wonMatch.Type;

            var ratingFactory = ItemFactoryCreator.Create<PlayerRatingItem>();
            var sk = string.Format(ratingFactory.SKFormat, variant);

            // we check if the calling player already has a rating for the given variant
            var playerRating = await repo.GetItemAsync<PlayerRatingItem>(playerId, sk);
            if (playerRating == null)
            {
                playerRating = new PlayerRatingItem()
                {
                    PlayerId = playerId,
                    Variant = variant,
                    Modus = modus,
                    Type = type
                };
            }
            var playerGlicko = Glicko2Rating.From(playerRating);

            // we check if the opponent player already has a rating for the given variant
            var opponentId = wonMatch.PlayerId == playerId ? lostMatch.PlayerId : wonMatch.PlayerId;
            var opponentRating = await repo.GetItemAsync<PlayerRatingItem>(opponentId, sk);
            if (opponentRating == null)
            {
                opponentRating = new PlayerRatingItem()
                {
                    PlayerId = opponentId,
                    Variant = variant,
                    Modus = modus,
                    Type = type
                };
            }
            var opponentGlicko = Glicko2Rating.From(opponentRating);

            // we calculate the match score
            var matchScore = MatchScoreCalculator.Build(playerId, wonMatch, lostMatch);

            var updatedPlayerRating = Glicko2RatingCalculator.Update(playerGlicko, opponentGlicko, matchScore);

            // we convert back to some ordinary values
            var newRating = Glicko2RatingCalculator.FromMu(updatedPlayerRating.Mu);
            var newRatingDeviation = Glicko2RatingCalculator.FromPhi(updatedPlayerRating.Phi);

            // we update the player rating
            playerRating.Rating = newRating;
            playerRating.RatingDeviation = newRatingDeviation;
            playerRating.Sigma = updatedPlayerRating.Sigma;

            if (newRating > playerRating.HighestRating)
            {
                playerRating.HighestRating = newRating;
            }
            if (newRating < playerRating.LowestRating)
            {
                playerRating.LowestRating = newRating;
            }

            await repo.SaveAsync(playerRating);

            return playerRating;
        }
    }
}
