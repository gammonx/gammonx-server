using GammonX.DynamoDb.Items;

using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Stats
{
    internal static class MatchScoreCalculator
    {
        public static double Build(Guid playerId, MatchItem wonMatchItem, MatchItem lostMatchItem)
        {
            var playersMatch = GetPlayersMatch(playerId, wonMatchItem, lostMatchItem);
            bool playerWon = playersMatch.Result == MatchResult.Won;
            double baseScore = playerWon ? 1.0 : 0.0;

            // we apply negative bonuses if player lostv
            int sign = playerWon ? 1 : -1;

            // calculate the win type bonus
            double winTypeBonus = 0.0;
            double gammonMultiplier = 0.15;
            double backgammonMultiplier = 0.30;
            switch (playersMatch.Result)
            {
                case MatchResult.Won:
                    var gammonBonus = playersMatch.Gammons * gammonMultiplier;
                    var backGammonsBonus = playersMatch.Backgammons * backgammonMultiplier;
                    winTypeBonus = gammonBonus + backGammonsBonus;
                    break;
                case MatchResult.Lost:
                    winTypeBonus = 0.00;
                    break;
            }

            // we calculate pip difference logistic (output between 0 and 0.10)
            var pipDifference = lostMatchItem.AvgPipesLeft - wonMatchItem.AvgPipesLeft;
            double pipBonus = 0.10 * Sigmoid(pipDifference / 10.0);

            // we calculate point difference bonus. Typical matches have small effect
            var pointDifference = wonMatchItem.Points - lostMatchItem.Points;
            double pointBonus = Math.Min(0.10, Math.Abs(pointDifference) * 0.05);

            // we calculate the match length dampening. Longer matches are more stable
            var matchLength = Math.Min(wonMatchItem.Length, lostMatchItem.Length);
            double lengthFactor = Math.Min(1.0, matchLength / 7.0); // 7pt match gives full effect

            // we calculate the strength bonus
            double strengthBonus = (winTypeBonus + pipBonus + pointBonus) * sign * lengthFactor;

            // we clamp final score into [0, 1]
            double finalScore = Math.Clamp(baseScore + strengthBonus, 0.0, 1.0);

            return finalScore;
        }

        private static double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));

        private static MatchItem GetPlayersMatch(Guid playerId, params MatchItem[] matches)
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
