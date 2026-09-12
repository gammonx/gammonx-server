using GammonX.DynamoDb.Stats;
using GammonX.DynamoDb.Items;

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

        [Fact]
        public void WeightedAverageDoubleIgnoresInvalidWeightsAndValues()
        {
            var items = new[]
            {
                new ValueWeight(100, -1),
                new ValueWeight(double.NaN, 2),
                new ValueWeight(20, 2)
            };

            var result = StatsAggregator.WeightedAverage(items, v => v.Value, v => v.Weight);

            Assert.Equal(20, result);
        }

        [Fact]
        public void CalculateWinStreaksIgnoresUnfinishedMatches()
        {
            var now = DateTime.UtcNow;
            var matches = new[]
            {
                new MatchItem { Result = Models.Enums.MatchResult.Won, EndedAt = now.AddDays(-3) },
                new MatchItem { Result = Models.Enums.MatchResult.Won, EndedAt = now.AddDays(-2) },
                new MatchItem { Result = Models.Enums.MatchResult.Unknown, EndedAt = now.AddDays(-1) },
                new MatchItem { Result = Models.Enums.MatchResult.Won, EndedAt = now }
            };

            var (currentStreak, longestStreak) = StatsAggregator.CalculateWinStreaks(matches);

            Assert.Equal(3, currentStreak);
            Assert.Equal(3, longestStreak);
        }

        [Fact]
        public void CalculateWinStreaksResetsOnlyAfterLosses()
        {
            var now = DateTime.UtcNow;
            var matches = new[]
            {
                new MatchItem { Result = Models.Enums.MatchResult.Won, EndedAt = now.AddDays(-3) },
                new MatchItem { Result = Models.Enums.MatchResult.Won, EndedAt = now.AddDays(-2) },
                new MatchItem { Result = Models.Enums.MatchResult.Lost, EndedAt = now.AddDays(-1) },
                new MatchItem { Result = Models.Enums.MatchResult.Won, EndedAt = now }
            };

            var (currentStreak, longestStreak) = StatsAggregator.CalculateWinStreaks(matches);

            Assert.Equal(1, currentStreak);
            Assert.Equal(2, longestStreak);
        }
    }
}
