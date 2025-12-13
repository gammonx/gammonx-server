using GammonX.Engine.Services;

namespace GammonX.Engine.Tests
{
    public class DiceServiceTests
    {
        [Theory]
        [InlineData(DiceServiceType.Crypto, typeof(CryptoDiceService))]
        [InlineData(DiceServiceType.Simple, typeof(SimpleDiceService))]
        public void DiceServiceFactoryReturnsCorrectType(DiceServiceType serviceType, Type expectedType)
        {
            var factory = new DiceServiceFactory();
            var service = factory.Create(serviceType);
            Assert.IsType(expectedType, service);
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        [InlineData(typeof(SimpleDiceService))]
        public void RollSingleDieReturnsValueWithinBounds(Type serviceType)
        {
            var service = (IDiceService)Activator.CreateInstance(serviceType)!;
            var results = Enumerable.Range(0, 1000)
                .SelectMany(_ => service.Roll(1, 6))
                .ToArray();
            // all dice results must be between 1 and 6 inclusive
            Assert.All(results, value => Assert.InRange(value, 1, 6));
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        [InlineData(typeof(SimpleDiceService))]
        public void RollReturnsCorrectNumberOfDice(Type serviceType)
        {
            var service = (IDiceService)Activator.CreateInstance(serviceType)!;
            // roll a defined amount of dice
            var result = service.Roll(5, 6);
            // returned array length must match requested dice count
            Assert.Equal(5, result.Length);
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        [InlineData(typeof(SimpleDiceService))]
        public void RollMultipleDiceProducesIndependentValues(Type serviceType)
        {
            var service = (IDiceService)Activator.CreateInstance(serviceType)!;
            // roll multiple dice at once
            var result = service.Roll(10, 6);
            // ensure not all dice are the same value which would indicate a logic error
            Assert.True(result.Distinct().Count() > 1);
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        [InlineData(typeof(SimpleDiceService))]
        public void RollRejectsZeroDice(Type serviceType)
        {
            var service = (IDiceService)Activator.CreateInstance(serviceType)!;
            // zero dice is an invalid request
            Assert.Throws<ArgumentOutOfRangeException>(() => service.Roll(0, 6));
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        [InlineData(typeof(SimpleDiceService))]
        public void RollRejectsNegativeDiceCount(Type serviceType)
        {
            var service = (IDiceService)Activator.CreateInstance(serviceType)!;
            // negative dice count must throw
            Assert.Throws<ArgumentOutOfRangeException>(() => service.Roll(-3, 6));
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        public void RollRejectsNonSixSidedDice(Type serviceType)
        {
            var service = (IDiceService)Activator.CreateInstance(serviceType)!;
            // only six sided dice are supported by design
            Assert.Throws<ArgumentOutOfRangeException>(() => service.Roll(2, 4));
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        [InlineData(typeof(SimpleDiceService))]
        public void RollProducesDifferentSequencesAcrossCalls(Type serviceType)
        {
            var service = (IDiceService)Activator.CreateInstance(serviceType)!;
            // roll two independent sequences
            var first = service.Roll(20, 6);
            var second = service.Roll(20, 6);
            // extremely unlikely both sequences are identical unless broken
            Assert.NotEqual(first, second);
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        [InlineData(typeof(SimpleDiceService))]
        public void RollLargeAmountOfDiceDoesNotThrow(Type serviceType)
        {
            var service = (IDiceService)Activator.CreateInstance(serviceType)!;
            // large roll size to validate stability and performance
            var result = service.Roll(10_000, 6);
            // ensure all dice were rolled
            Assert.Equal(10_000, result.Length);
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        [InlineData(typeof(SimpleDiceService))]
        public void ThrowsErrorIfDiceCountIsZero(Type serviceType)
        {
            var diceService = (IDiceService)Activator.CreateInstance(serviceType)!;
            Assert.Throws<ArgumentOutOfRangeException>(() => diceService.Roll(0, 6));
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        [InlineData(typeof(SimpleDiceService))]
        public void ThrowsErrorIfSidesPerDiceIsLowerThanOne(Type serviceType)
        {
            var diceService = (IDiceService)Activator.CreateInstance(serviceType)!;
            Assert.Throws<ArgumentOutOfRangeException>(() => diceService.Roll(1, 0));
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        [InlineData(typeof(SimpleDiceService))]
        public void ThrowsErrorIfSidesPerDiceIsLowerThanTwo(Type serviceType)
        {
            var diceService = (IDiceService)Activator.CreateInstance(serviceType)!;
            Assert.Throws<ArgumentOutOfRangeException>(() => diceService.Roll(1, 1));
        }

        [Theory]
        [InlineData(typeof(CryptoDiceService))]
        [InlineData(typeof(SimpleDiceService))]
        public void RollsDiceWithCorrectNumberOfSides(Type serviceType)
        {
            var diceService = (IDiceService)Activator.CreateInstance(serviceType)!;
            var result = diceService.Roll(2, 6);
            Assert.Equal(2, result.Length);
            foreach (var roll in result)
            {
                Assert.InRange(roll, 1, 6);
            }
        }
    }
}