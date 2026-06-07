using GammonX.Mars.NN.Models;

namespace GammonX.Mars.NN.Tests
{
    public class GameOutcomeModelTests
    {
        [Fact]
        public void ConstructorReadsAsExpected()
        {
            float[] gameOutcome = new float[] { 1.0f, 0.5f, 0.2f, 0.1f, 0.05f };
            var model = new GameOutcomeModel(gameOutcome);

            Assert.Equal(1.0f, model.WinP);
            Assert.Equal(0.5f, model.WinGammonP);
            Assert.Equal(0.2f, model.WinBackgammonP);
            Assert.Equal(0.0f, model.LoseP);
            Assert.Equal(0.1f, model.LoseGammonP);
            Assert.Equal(0.05f, model.LoseBackgammonP);
        }
    }
}
