using GammonX.Engine.Services;

namespace GammonX.Engine.Tests
{
    public class DiceServiceTests
    {
        private readonly IDiceServiceFactory _factory = new DiceServiceFactory();

        [Fact]
        public void ThrowsErrorIfDiceCountIsZero()
        {
            var diceService = _factory.Create();
            Assert.Throws<ArgumentOutOfRangeException>(() => diceService.Roll(0, 6));
        }

        [Fact]
        public void ThrowsErrorIfSidesPerDiceIsLowerThanOne()
        {
            var diceService = _factory.Create();
            Assert.Throws<ArgumentOutOfRangeException>(() => diceService.Roll(1, 0));
        }

        [Fact]
        public void ThrowsErrorIfSidesPerDiceIsLowerThanTwo()
        {
            var diceService = _factory.Create();
            Assert.Throws<ArgumentOutOfRangeException>(() => diceService.Roll(1, 1));
        }

        [Fact]
        public void RollsDiceWithCorrectNumberOfSides()
        {
            var diceService = _factory.Create();
            var result = diceService.Roll(2, 6);
            Assert.Equal(2, result.Length);
            foreach (var roll in result)
            {
                Assert.InRange(roll, 1, 6);
            }
        }
    }
}