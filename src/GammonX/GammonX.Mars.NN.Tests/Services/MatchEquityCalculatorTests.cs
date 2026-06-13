using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;

namespace GammonX.Mars.NN.Tests.Services
{
    public class MatchEquityCalculatorTests
    {
        [Fact]
        public void GetMETZeroAwayForPlayerReturnsOne()
        {
            Assert.Equal(1.0, MatchEquityCalculator.GetMET(0, 5));
            Assert.Equal(1.0, MatchEquityCalculator.GetMET(-1, 5));
        }

        [Fact]
        public void GetMETZeroAwayForOpponentReturnsZero()
        {
            Assert.Equal(0.0, MatchEquityCalculator.GetMET(5, 0));
            Assert.Equal(0.0, MatchEquityCalculator.GetMET(5, -1));
        }

        [Fact]
        public void GetMETLooksUpCorrectTableValue()
        {
            Assert.Equal(0.50, MatchEquityCalculator.GetMET(1, 1), 10);
            Assert.Equal(0.75, MatchEquityCalculator.GetMET(1, 3), 10);
            Assert.Equal(0.50, MatchEquityCalculator.GetMET(5, 5), 10);
            Assert.Equal(0.44, MatchEquityCalculator.GetMET(8, 7), 10);
        }

        [Fact]
        public void GetMETClampsToTableSize()
        {
            var expected = MatchEquityCalculator.GetMET(15, 15);

            Assert.Equal(expected, MatchEquityCalculator.GetMET(16, 15), 10);
            Assert.Equal(expected, MatchEquityCalculator.GetMET(15, 16), 10);
            Assert.Equal(expected, MatchEquityCalculator.GetMET(100, 100), 10);
        }

        [Fact]
        public void CalculateEquityPureSingleWinUsesWinningMET()
        {
            var gameOutcome = new[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var equityModel = new GameEquityModel(outcome);

            var equity = MatchEquityCalculator.CalculateEquity(
                equityModel,
                pointsAway: 5,
                pointsAwayOpp: 5,
                cubeValue: 1);

            Assert.Equal(
                MatchEquityCalculator.GetMET(4, 5),
                equity,
                10);
        }

        [Fact]
        public void CalculateEquityPureSingleLossUsesLosingMET()
        {
            var gameOutcome = new[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);


            var equityModel = new GameEquityModel(outcome);

            var equity = MatchEquityCalculator.CalculateEquity(
                equityModel,
                pointsAway: 5,
                pointsAwayOpp: 5,
                cubeValue: 1);

            Assert.Equal(
                MatchEquityCalculator.GetMET(5, 4),
                equity,
                10);
        }

        [Fact]
        public void CalculateEquityPureGammonWinUsesTwoCubePoints()
        {
            var gameOutcome = new[] { 1.0f, 1.0f, 0.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var equityModel = new GameEquityModel(outcome);

            var equity = MatchEquityCalculator.CalculateEquity(
                equityModel,
                pointsAway: 5,
                pointsAwayOpp: 5,
                cubeValue: 1);

            Assert.Equal(
                MatchEquityCalculator.GetMET(3, 5),
                equity,
                10);
        }

        [Fact]
        public void CalculateEquityPureBackgammonWinUsesThreeCubePoints()
        {
            var gameOutcome = new[] { 1.0f, 1.0f, 1.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var equityModel = new GameEquityModel(outcome);

            var equity = MatchEquityCalculator.CalculateEquity(
                equityModel,
                pointsAway: 5,
                pointsAwayOpp: 5,
                cubeValue: 1);

            Assert.Equal(
                MatchEquityCalculator.GetMET(2, 5),
                equity,
                10);
        }

        [Fact]
        public void CalculateEquityWinningMatchReturnsFullEquity()
        {
            var gameOutcome = new[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var equityModel = new GameEquityModel(outcome);

            var equity = MatchEquityCalculator.CalculateEquity(
                equityModel,
                pointsAway: 1,
                pointsAwayOpp: 10,
                cubeValue: 1);

            Assert.Equal(1.0, equity, 10);
        }

        [Fact]
        public void CalculateEquityMixedOutcomesComputesWeightedAverage()
        {
            var gameOutcome = new[] { 0.7f, 0.2f, 0.05f, 0.1f, 0.02f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var model = new GameEquityModel(outcome);

            var equity = MatchEquityCalculator.CalculateEquity(
                model,
                pointsAway: 5,
                pointsAwayOpp: 5,
                cubeValue: 1);

            var expected =
                0.50 * MatchEquityCalculator.GetMET(4, 5) +
                0.15 * MatchEquityCalculator.GetMET(3, 5) +
                0.05 * MatchEquityCalculator.GetMET(2, 5) +
                0.20 * MatchEquityCalculator.GetMET(5, 4) +
                0.08 * MatchEquityCalculator.GetMET(5, 3) +
                0.02 * MatchEquityCalculator.GetMET(5, 2);

            Assert.Equal(expected, equity, 5);
        }

        [Fact]
        public void PassEquityWhenCubeWinsMatchReturnsOne()
        {
            var equityPass =
                MatchEquityCalculator.GetMET(
                    playerAway: 1 - 1,
                    opponentAway: 7);

            Assert.Equal(1.0, equityPass);
        }

        [Fact]
        public void ZeroPointsAwayReturnsProperResult()
        {
            var opponentWon = MatchEquityCalculator.GetMET(1, 0);
            Assert.Equal(0.0, opponentWon);
            var playerWon = MatchEquityCalculator.GetMET(0, 1);
            Assert.Equal(1.0, playerWon);
            var playerLostEquity = MatchEquityCalculator.CalculateEquity(null!, 1, 0, 1);
            Assert.Equal(0.0, playerLostEquity);
            var playerWonEquity = MatchEquityCalculator.CalculateEquity(null!, 0, 1, 1);
            Assert.Equal(1.0, playerWonEquity);
        }

        [Fact]
        public void CalculateEquityDoubleTakeUsesDoubledCubeValue()
        {
            var gameOutcome = new[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var model = new GameEquityModel(outcome);

            var cube1 = MatchEquityCalculator.CalculateEquity(
                model,
                pointsAway: 5,
                pointsAwayOpp: 5,
                cubeValue: 1);

            var cube2 = MatchEquityCalculator.CalculateEquity(
                model,
                pointsAway: 5,
                pointsAwayOpp: 5,
                cubeValue: 2);

            Assert.NotEqual(cube1, cube2);
            Assert.True(cube2 > cube1);
        }

        [Fact]
        public void DoubleThatWinsMatchHasFullMatchEquity()
        {
            var gameOutcome = new[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var model = new GameEquityModel(outcome);

            var equity = MatchEquityCalculator.CalculateEquity(
                model,
                pointsAway: 2,
                pointsAwayOpp: 8,
                cubeValue: 2);

            Assert.Equal(1.0, equity);
        }
    }
}
