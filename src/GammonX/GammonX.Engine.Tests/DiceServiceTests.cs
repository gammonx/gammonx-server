
namespace GammonX.Engine.Tests
{
    public class DiceServiceTests
    {
        [Fact]
        public void ThrowsErrorIfDiceCountIsZero()
        {
            var diceService = DiceServiceFactory.Create();
            Assert.Throws<ArgumentOutOfRangeException>(() => diceService.Roll(0, 6));
        }

        [Fact]
        public void ThrowsErrorIfSidesPerDiceIsLowerThanOne()
        {
            var diceService = DiceServiceFactory.Create();
            Assert.Throws<ArgumentOutOfRangeException>(() => diceService.Roll(1, 0));
        }

        [Fact]
        public void ThrowsErrorIfSidesPerDiceIsLowerThanTwo()
        {
            var diceService = DiceServiceFactory.Create();
            Assert.Throws<ArgumentOutOfRangeException>(() => diceService.Roll(1, 1));
        }

        [Fact]
        public void RollsDiceWithCorrectNumberOfSides()
        {
            var diceService = DiceServiceFactory.Create();
            var result = diceService.Roll(2, 6);
            Assert.Equal(2, result.Length);
            foreach (var roll in result)
            {
                Assert.InRange(roll, 1, 6);
            }
        }
    }
}