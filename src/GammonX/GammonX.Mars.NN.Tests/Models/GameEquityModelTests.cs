using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Tests.Models
{
    public class GameEquityModelTests
    {
        [Fact]
        public void ConstructorReadsAsExpected()
        {
            var gameOutcome = new [] { 1.0f, 0.5f, 0.2f, 0.1f, 0.05f };
            var model = new GameOutcomeModel(gameOutcome);

            Assert.Equal(1.0f, model.WinP);
            Assert.Equal(0.5f, model.WinGammonP);
            Assert.Equal(0.2f, model.WinBackgammonP);
            Assert.Equal(0.0f, model.LoseP);
            Assert.Equal(0.1f, model.LoseGammonP);
            Assert.Equal(0.05f, model.LoseBackgammonP);
        }

        [Fact]
        public void PureSingleWinComputesCorrectProbabilitiesAndEquity()
        {
            var gameOutcome = new [] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var model = new GameEquityModel(outcome);

            Assert.Equal(1.0, model.WinSingleP);
            Assert.Equal(0.0, model.WinGammonP);
            Assert.Equal(0.0, model.WinBackgammonP);
            Assert.Equal(1.0, model.Equity);
        }

        [Fact]
        public void PureGammonWinComputesCorrectEquity()
        {
            var gameOutcome = new [] { 1.0f, 1.0f, 0.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var model = new GameEquityModel(outcome);

            Assert.Equal(0.0, model.WinSingleP);
            Assert.Equal(1.0, model.WinGammonP);
            Assert.Equal(0.0, model.WinBackgammonP);
            Assert.Equal(2.0, model.Equity);
        }

        [Fact]
        public void PureBackgammonWinComputesCorrectEquity()
        {
            var gameOutcome = new [] { 1.0f, 1.0f, 1.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var model = new GameEquityModel(outcome);

            Assert.Equal(0.0, model.WinSingleP);
            Assert.Equal(0.0, model.WinGammonP);
            Assert.Equal(1.0, model.WinBackgammonP);
            Assert.Equal(3.0, model.Equity);
        }

        [Fact]
        public void PureSingleLossComputesCorrectEquity()
        {
            var gameOutcome = new [] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var model = new GameEquityModel(outcome);

            Assert.Equal(1.0, model.LoseSingleP);
            Assert.Equal(-1.0, model.Equity);
        }

        [Theory]
        [InlineData(0.0f, -1.0)]
        [InlineData(0.25f, -0.5)]
        [InlineData(0.5f, 0.0)]
        [InlineData(0.75f, 0.5)]
        [InlineData(1.0f, 1.0)]
        public void WinProbabilityOnlyConvertsToSinglePointEquity(float winProbability, double expectedEquity)
        {
            var outcome = new GameOutcomeModel([winProbability, 0.0f, 0.0f, 0.0f, 0.0f]);

            var model = new GameEquityModel(outcome);

            Assert.Equal(expectedEquity, model.Equity, 10);
        }

        [Theory]
        [InlineData(1.0f, 1.0f, 1.0f, 0.0f, 0.0f)]
        [InlineData(0.0f, 0.0f, 0.0f, 1.0f, 1.0f)]
        [InlineData(0.5f, 1.0f, 1.0f, 1.0f, 1.0f)]
        [InlineData(-1.0f, 2.0f, -1.0f, 2.0f, -1.0f)]
        public void EquityRemainsWithinCubelessGamePointRange(
            float winProbability,
            float winGammonProbability,
            float winBackgammonProbability,
            float loseGammonProbability,
            float loseBackgammonProbability)
        {
            var outcome = new GameOutcomeModel(
                [winProbability, winGammonProbability, winBackgammonProbability, loseGammonProbability, loseBackgammonProbability]);

            var model = new GameEquityModel(outcome);

            Assert.InRange(model.Equity, -3.0, 3.0);
        }

        [Fact]
        public void MixedProbabilitiesComputesCorrectEquity()
        {
            var gameOutcome = new [] { 0.70f, 0.20f, 0.05f, 0.10f, 0.02f };
            var outcome = new GameOutcomeModel(gameOutcome);


            var model = new GameEquityModel(outcome);

            Assert.Equal(0.50f, model.WinSingleP, 5);
            Assert.Equal(0.15f, model.WinGammonP, 5);
            Assert.Equal(0.05f, model.WinBackgammonP, 5);

            Assert.Equal(0.20f, model.LoseSingleP, 5);
            Assert.Equal(0.08f, model.LoseGammonP, 5);
            Assert.Equal(0.02f, model.LoseBackgammonP, 5);

            var expected =
                0.50 * 1 +
                0.15 * 2 +
                0.05 * 3 -
                0.20 * 1 -
                0.08 * 2 -
                0.02 * 3;

            Assert.Equal(expected, model.Equity, 5);
        }

        [Fact]
        public void InvalidOrderingIsProjectedToAValidDistribution()
        {
            var gameOutcome = new [] { 0.50f, 0.70f, 0.20f, 0.60f, 0.10f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var model = new GameEquityModel(outcome);

            Assert.Equal(0.0f, model.WinSingleP, 5);
            Assert.Equal(0.30f, model.WinGammonP, 5);
            Assert.Equal(0.20f, model.WinBackgammonP, 5);

            Assert.Equal(0.0f, model.LoseSingleP, 5);
            Assert.Equal(0.40f, model.LoseGammonP, 5);
            Assert.Equal(0.10f, model.LoseBackgammonP, 5);
            Assert.False(model.ConstraintReport.IsValid);
            Assert.True(model.WasProjected);
            Assert.Equal(1.0, model.AtomicProbabilityMass, 5);
        }

        [Fact]
        public void AllProbabilitiesZeroEquityIsZero()
        {
            var gameOutcome = new [] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var model = new GameEquityModel(outcome);

            Assert.Equal(-1.0, model.Equity);
        }

        [Fact]
        public void ValidNetworkOutputsDecomposesProbabilitiesCorrectly()
        {
            var gameOutcome = new[] { 0.7f, 0.2f, 0.05f, 0.1f, 0.02f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var model = new GameEquityModel(outcome);

            Assert.Equal(0.50f, model.WinSingleP, 5);
            Assert.Equal(0.15f, model.WinGammonP, 5);
            Assert.Equal(0.05f, model.WinBackgammonP, 5);

            Assert.Equal(0.20f, model.LoseSingleP, 5);
            Assert.Equal(0.08f, model.LoseGammonP, 5);
            Assert.Equal(0.02f, model.LoseBackgammonP, 5);

            var total =
                model.WinSingleP +
                model.WinGammonP +
                model.WinBackgammonP +
                model.LoseSingleP +
                model.LoseGammonP +
                model.LoseBackgammonP;

            Assert.Equal(1.0f, total, 5);
        }
    }
}
