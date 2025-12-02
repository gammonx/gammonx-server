using GammonX.DynamoDb.Stats;

namespace GammonX.DynamoDb.Tests
{
    public class StatsAggregatorTests
    {
        private record ValueWeight(double Value, double Weight);

        private record TimeSpanWeight(TimeSpan Time, double Weight);

        [Fact]
        public void WeightedAverageDoubleNormalCaseReturnsCorrectValue()
        {
            var items = new[] { new ValueWeight(10, 1), new ValueWeight(20, 2), new ValueWeight(40, 3) };
            double expected = (10 + 40 + 120) / 6.0;
            double result = StatsAggregator.WeightedAverage(items, v => v.Value, v => v.Weight);
            Assert.Equal(expected, result, precision: 6);
        }

        [Fact]
        public void WeightedAverageDoubleAllWeightsZeroReturnsZero()
        {
            var items = new[] { new ValueWeight(10, 0), new ValueWeight(20, 0) };
            double result = StatsAggregator.WeightedAverage(items, v => v.Value, v => v.Weight);
            Assert.Equal(0, result);
        }

        [Fact]
        public void WeightedAverage_Double_EmptyList_ReturnsZero()
        {
            var items = Array.Empty<ValueWeight>();
            double result = StatsAggregator.WeightedAverage(items, v => v.Value, v => v.Weight);
            Assert.Equal(0, result);
        }

        [Fact]
        public void WeightedAverageDoubleSingleItemReturnsValue()
        {
            var items = new[] { new ValueWeight(50, 10) };
            double result = StatsAggregator.WeightedAverage(items, v => v.Value, v => v.Weight);
            Assert.Equal(50, result);
        }

        [Fact]
        public void WeightedAverageTimeSpanNormalCaseReturnsCorrectValue()
        {
            var items = new[]
            {
                new TimeSpanWeight(TimeSpan.FromMinutes(10), 1),
                new TimeSpanWeight(TimeSpan.FromMinutes(20), 2),
                new TimeSpanWeight(TimeSpan.FromMinutes(40), 3)
            };
            // expected = (10*1 + 20*2 + 40*3) / 6 = 170 / 6 ≈ 28.333 minutes
            var expectedMinutes = 170.0 / 6.0;
            var expected = TimeSpan.FromMinutes(expectedMinutes);

            TimeSpan result = StatsAggregator.WeightedAverage(items, v => v.Time, v => v.Weight);
            Assert.Equal(expected.Ticks, result.Ticks);
        }

        [Fact]
        public void WeightedAverageTimeSpanAllWeightsZeroReturnsZero()
        {
            var items = new[]
            {
                new TimeSpanWeight(TimeSpan.FromMinutes(5), 0),
                new TimeSpanWeight(TimeSpan.FromMinutes(15), 0),
            };

            TimeSpan result = StatsAggregator.WeightedAverage( items, v => v.Time, v => v.Weight);
            Assert.Equal(TimeSpan.Zero, result);
        }

        [Fact]
        public void WeightedAverageTimeSpanEmptyListReturnsZero()
        {
            var items = Array.Empty<TimeSpanWeight>();
            TimeSpan result = StatsAggregator.WeightedAverage(items, v => v.Time, v => v.Weight);
            Assert.Equal(TimeSpan.Zero, result);
        }

        [Fact]
        public void WeightedAverageTimeSpanSingleItemReturnsValue()
        {
            var single = TimeSpan.FromHours(2);
            var items = new[] { new TimeSpanWeight(single, 5) };
            TimeSpan result = StatsAggregator.WeightedAverage( items, v => v.Time, v => v.Weight);
            Assert.Equal(single, result);
        }

        [Fact]
        public void WeightedAverageTimeSpanMixedWeights_eturnsCorrectValue()
        {
            var items = new[]
            {
                new TimeSpanWeight(TimeSpan.FromSeconds(0), 1),
                new TimeSpanWeight(TimeSpan.FromSeconds(100), 4)
            };
            // expected = (0*1 + 100*4) / (1+4) = 400 / 5 = 80 sec
            var expected = TimeSpan.FromSeconds(80);
            TimeSpan result = StatsAggregator.WeightedAverage(items, v => v.Time, v => v.Weight);
            Assert.Equal(expected, result);
        }
    }
}
