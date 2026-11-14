using GammonX.DynamoDb.Items;

using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Stats
{
    internal static class MatchScoreCalculator
    {
        public static double Calculate(Guid playerId, MatchScoreInput wonMatchItem, MatchScoreInput lostMatchItem)
        {
            var playersMatch = GetPlayersMatch(playerId, wonMatchItem, lostMatchItem);
            bool playerWon = playersMatch.Result == MatchResult.Won;
            double winTypeBonus = wonMatchItem.Gammons * 0.15 + wonMatchItem.Backgammons * 0.30;

            // we calculate pip difference logistic (output between 0 and 0.10)
            var pipDifference = lostMatchItem.AvgPipesLeft - wonMatchItem.AvgPipesLeft;
            double pipBonus = 0.10 * Math.Max(0.0, 2.0 * Sigmoid(pipDifference / 10.0) - 1.0);

            // we calculate point difference bonus. Typical matches have small effect
            var pointDifference = wonMatchItem.Points - lostMatchItem.Points;
            double pointBonus = Math.Min(0.10, Math.Abs(pointDifference) * 0.05);

            // we calculate the match length dampening. Longer matches are more stable
            var matchLength = Math.Min(wonMatchItem.Length, lostMatchItem.Length);
            double lengthFactor = Math.Clamp(matchLength / 7.0, 0.0, 1.0);

            // Reserve headroom above a base win score, keeping bonuses effective without saturation.
            // Both players use the winners performance so their normalized scores sum to one.
            double strengthBonus = Math.Max(0.0, (winTypeBonus + pipBonus + pointBonus) * lengthFactor);
            double winnerScore = 0.75 + 0.25 * strengthBonus / (1.0 + strengthBonus);
            return playerWon ? winnerScore : 1.0 - winnerScore;
        }

        public static MatchScoreInput From(this MatchItem item)
        {
            return new MatchScoreInput()
            {
                PlayerId = item.PlayerId,
                Result = item.Result,
                Gammons = item.Gammons,
                Backgammons = item.Backgammons,
                AvgPipesLeft = item.AvgPipesLeft,
                Points = item.Points,
                Length = item.Length
            };
        }

        private static double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));

        private static MatchScoreInput GetPlayersMatch(Guid playerId, params MatchScoreInput[] matches)
        {
            foreach (var match in matches)
            {
                if (match.PlayerId == playerId)
                {
                    return match;
                }
            }
            throw new ArgumentException("Player match not found", nameof(playerId));
        }
    }
}
