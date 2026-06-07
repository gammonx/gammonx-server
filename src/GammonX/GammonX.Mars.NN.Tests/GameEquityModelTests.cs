using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Tests
{
    public class GameEquityModelTests
    {
        [Fact]
        public void GameEquityIsCalculatedProperly()
        {
            float[] gameOutcome = new float[] { 0.9f, 0.5f, 0.2f, 0.1f, 0.05f };
            var model = new GameOutcomeModel(gameOutcome);
            var equityModel = new GameEquityModel(model);

            Assert.Equivalent(0.4f, equityModel.WinSingleP);
            Assert.Equivalent(0.3f, equityModel.WinGammonP);
            Assert.Equivalent(0.2f, equityModel.WinBackgammonP);
        }
    }
}
