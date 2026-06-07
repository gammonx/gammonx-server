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
        public void InvalidOrderingClampsNegativeProbabilitiesToZero()
        {
            var gameOutcome = new [] { 0.50f, 0.70f, 0.20f, 0.60f, 0.10f };
            var outcome = new GameOutcomeModel(gameOutcome);

            var model = new GameEquityModel(outcome);

            Assert.Equal(0.0f, model.WinSingleP, 5);
            Assert.Equal(0.50f, model.WinGammonP, 5);
            Assert.Equal(0.20f, model.WinBackgammonP, 5);

            Assert.Equal(0.0f, model.LoseSingleP, 5);
            Assert.Equal(0.50f, model.LoseGammonP, 5);
            Assert.Equal(0.10f, model.LoseBackgammonP, 5);
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
