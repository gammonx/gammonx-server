using GammonX.Models.Enums;

using GammonX.DynamoDb.Stats;

namespace GammonX.DynamoDb.Tests.Rating
{
    public class MatchScoreCalculatorTests
    {
        [Fact]
        public void WinNoBonusesReservesHeadroomForStrengthBonuses()
        {
            var p = Guid.NewGuid();
            var winner = Win(p);
            var loser = Loss(Guid.NewGuid());

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            Assert.Equal(0.75, score);
        }

        [Fact]
        public void LossNoBonusesComplementsBaseWinScore()
        {
            var p = Guid.NewGuid();
            var loser = Loss(p);
            var winner = Win(Guid.NewGuid());

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            Assert.Equal(0.25, score);
        }

        [Fact]
        public void WinWithGammonBonusIncreasesScore()
        {
            var p = Guid.NewGuid();
            var winner = Win(p, gammons: 1);
            var loser = Loss(Guid.NewGuid());

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            var baseline = MatchScoreCalculator.Calculate(p, Win(p), loser);
            Assert.True(score > baseline);
        }

        [Fact]
        public void WinWithBackgammonBonusIncreasesScoreMore()
        {
            var p = Guid.NewGuid();
            var winner = Win(p, backgammons: 1);
            var loser = Loss(Guid.NewGuid());

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            var gammonScore = MatchScoreCalculator.Calculate(p, Win(p, gammons: 1), loser);
            Assert.True(score > gammonScore);
        }

        [Fact]
        public void WinWithBetterPipDifferenceIncreasesScoreSlightly()
        {
            var p = Guid.NewGuid();

            // winner has fewer pips (better)
            var winner = Win(p, avgPipesLeft: 0);
            var loser = Loss(Guid.NewGuid(), avgPipesLeft: 30);

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            var baseline = MatchScoreCalculator.Calculate(p, winner, Loss(loser.PlayerId));
            Assert.True(score > baseline);
        }

        [Fact]
        public void LossWithBadPipDifferenceDecreasesScore()
        {
            var p = Guid.NewGuid();

            var loser = Loss(p, avgPipesLeft: 50);
            var winner = Win(Guid.NewGuid(), avgPipesLeft: 0);

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            var baseline = MatchScoreCalculator.Calculate(p, winner, Loss(p));
            Assert.True(score < baseline);
            Assert.True(score > 0.0);
        }

        [Fact]
        public void WinWithMorePointsGivesPointBonus()
        {
            var p = Guid.NewGuid();

            var winner = Win(p, points: 5);
            var loser = Loss(Guid.NewGuid(), points: 1);

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            var baseline = MatchScoreCalculator.Calculate(p, Win(p, points: 1), loser);
            Assert.True(score > baseline);
        }

        [Fact]
        public void BonusIsReducedWhenMatchShorterThan7()
        {
            var p = Guid.NewGuid();

            var winnerShort = Win(p, gammons: 1, length: 1);
            var loserShort = Loss(Guid.NewGuid(), length: 1);

            var scoreShort = MatchScoreCalculator.Calculate(p, winnerShort, loserShort);

            var winnerLong = Win(p, gammons: 1, length: 7);
            var loserLong = Loss(Guid.NewGuid(), length: 7);

            var scoreLong = MatchScoreCalculator.Calculate(p, winnerLong, loserLong);

            Assert.True(scoreLong > scoreShort);
        }

        [Fact]
        public void LossWithStrongNegativeBonusesStaysAboveZero()
        {
            var p = Guid.NewGuid();

            var loser = Loss(p, avgPipesLeft: 50, points: 0, length: 7);

            var winner = Win(Guid.NewGuid(), avgPipesLeft: 1, points: 5, length: 7);

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            Assert.True(score >= 0.0);
            Assert.True(score < 0.25);
        }

        [Fact]
        public void ScoreIsAlwaysBetween0And1()
        {
            var p = Guid.NewGuid();

            var winner = Win(p, gammons: 5, backgammons: 5, avgPipesLeft: 1, length: 7);
            var loser = Loss(Guid.NewGuid(), avgPipesLeft: 200, length: 7);

            var score = MatchScoreCalculator.Calculate(p, winner, loser);

            Assert.InRange(score, 0.0, 1.0);
            Assert.Equal(1.0, score + MatchScoreCalculator.Calculate(loser.PlayerId, winner, loser), 12);
        }

        [Fact]
        public void StrengthBonusesRemainEffectiveForLargeWinCounts()
        {
            // Arrange.
            var playerId = Guid.NewGuid();
            var loser = Loss(Guid.NewGuid(), length: 7);

            // Act.
            var strong = MatchScoreCalculator.Calculate(playerId, Win(playerId, gammons: 50, length: 7), loser);
            var stronger = MatchScoreCalculator.Calculate(playerId, Win(playerId, gammons: 100, length: 7), loser);

            // Assert.
            Assert.True(stronger > strong);
            Assert.True(stronger < 1.0);
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
