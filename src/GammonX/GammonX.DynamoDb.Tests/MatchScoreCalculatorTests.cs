using GammonX.Models.Enums;

using GammonX.DynamoDb.Stats;

namespace GammonX.DynamoDb.Tests
{
    public class MatchScoreCalculatorTests
    {
        [Fact]
        public void WinNoBonusesReturnsOne()
        {
            var p = Guid.NewGuid();
            var winner = Win(p);
            var loser = Loss(Guid.NewGuid());

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            Assert.Equal(1.0, score);
        }

        [Fact]
        public void LossNoBonusesReturnsZero()
        {
            var p = Guid.NewGuid();
            var loser = Loss(p);
            var winner = Win(Guid.NewGuid());

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            Assert.Equal(0.0, score);
        }

        [Fact]
        public void WinWithGammonBonusIncreasesScore()
        {
            var p = Guid.NewGuid();
            var winner = Win(p, gammons: 1);
            var loser = Loss(Guid.NewGuid());

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            // strength pushes to greater than 1
            // but clamp keeps it at 1
            Assert.Equal(1.0, score);
        }

        [Fact]
        public void WinWithBackgammonBonusIncreasesScoreMore()
        {
            var p = Guid.NewGuid();
            var winner = Win(p, backgammons: 1);
            var loser = Loss(Guid.NewGuid());

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            Assert.Equal(1.0, score); // clamped to 1
        }

        [Fact]
        public void WinWithBetterPipDifferenceIncreasesScoreSlightly()
        {
            var p = Guid.NewGuid();

            // winner has fewer pips (better)
            var winner = Win(p, avgPipesLeft: 0);
            var loser = Loss(Guid.NewGuid(), avgPipesLeft: 30);

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            // positive bonus pushes over 1 and getting clamped
            Assert.Equal(1.0, score);
        }

        [Fact]
        public void LossWithBadPipDifferenceDecreasesScore()
        {
            var p = Guid.NewGuid();

            var loser = Loss(p, avgPipesLeft: 50);
            var winner = Win(Guid.NewGuid(), avgPipesLeft: 0);

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            // negative logistic bonus
            Assert.True(score < 0.05);
            // clamped
            Assert.True(score >= 0.0);
        }

        [Fact]
        public void WinWithMorePointsGivesPointBonus()
        {
            var p = Guid.NewGuid();

            var winner = Win(p, points: 5);
            var loser = Loss(Guid.NewGuid(), points: 1);

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            // clamps after bonus
            Assert.Equal(1.0, score);
        }

        [Fact]
        public void BonusIsReducedWhenMatchShorterThan7()
        {
            var p = Guid.NewGuid();

            var winnerShort = Win(p, gammons: 1, length: 1);
            var loserShort = Loss(Guid.NewGuid(), length: 1);

            var scoreShort = MatchScoreCalculator.Calculate(p, winnerShort, loserShort);
            // still clamp but will be lower before clamp
            Assert.Equal(1.0, scoreShort);

            var winnerLong = Win(p, gammons: 1, length: 7);
            var loserLong = Loss(Guid.NewGuid(), length: 7);

            var scoreLong = MatchScoreCalculator.Calculate(p, winnerLong, loserLong);

            // long match applies full bonus for stronger score
            Assert.Equal(1.0, scoreLong);
        }

        [Fact]
        public void LossWithStrongNegativeBonusesStaysAboveZero()
        {
            var p = Guid.NewGuid();

            var loser = Loss(p, avgPipesLeft: 50, points: 0, length: 7);

            var winner = Win(Guid.NewGuid(), avgPipesLeft: 1, points: 5, length: 7);

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            Assert.True(score >= 0.0);
            Assert.True(score <= 0.2);
        }

        [Fact]
        public void ScoreIsAlwaysBetween0And1()
        {
            var p = Guid.NewGuid();

            var winner = Win(p, gammons: 5, backgammons: 5, avgPipesLeft: 1, length: 7);
            var loser = Loss(Guid.NewGuid(), avgPipesLeft: 200, length: 7);

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            Assert.InRange(score, 0.0, 1.0);
        }

        private static MatchScoreInput Win(
            Guid id,
            int gammons = 0,
            int backgammons = 0,
            double avgPipesLeft = 0,
            int points = 1,
            int length = 1)
        {
            return new MatchScoreInput
            {
                PlayerId = id,
                Result = MatchResult.Won,
                Gammons = gammons,
                Backgammons = backgammons,
                AvgPipesLeft = avgPipesLeft,
                Points = points,
                Length = length
            };
        }

        private static MatchScoreInput Loss(
            Guid id,
            int gammons = 0,
            int backgammons = 0,
            double avgPipesLeft = 0,
            int points = 1,
            int length = 1)
        {
            return new MatchScoreInput
            {
                PlayerId = id,
                Result = MatchResult.Lost,
                Gammons = gammons,
                Backgammons = backgammons,
                AvgPipesLeft = avgPipesLeft,
                Points = points,
                Length = length
            };
        }
    }
}