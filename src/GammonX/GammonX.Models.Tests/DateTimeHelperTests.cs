using GammonX.Models.Helpers;

namespace GammonX.Models.Tests
{
    public class DateTimeHelperTests
    {
        [Theory]
        [InlineData("01/12/2025 15:30:45")]
        [InlineData("1/2/2025 5:6:7")]
        [InlineData("12/01/2025 15:30:45")]
        [InlineData("2025-12-01T15:30:45")]
        [InlineData("20251201153045")]
        [InlineData("01.12.2025 15:30:45")]
        public void TryParseFlexibleParsesCommonFormatsCorrectly(string input)
        {
            // parse input using flexible parser
            var success = DateTimeHelper.TryParseFlexible(input, out var result);

            Assert.True(success);
            Assert.NotEqual(default, result);
        }

        [Theory]
        [InlineData("2025-12-01 15:30:45")]
        [InlineData("2025/12/01 15:30:45")]
        [InlineData("12-01-2025 15:30:45")]
        public void TryParseFlexibleParsesGeneralFallbackFormats(string input)
        {
            // parse input using flexible parser fallback
            var success = DateTimeHelper.TryParseFlexible(input, out var result);

            Assert.True(success);
            Assert.NotEqual(default, result);
        }

        [Fact]
        public void TryParseFlexibleReturnsFalseForInvalidInput()
        {
            var invalid = "not a date";

            var success = DateTimeHelper.TryParseFlexible(invalid, out var result);

            Assert.False(success);
            Assert.Equal(default, result);
        }

        [Fact]
        public void ParseFlexibleReturnsDateTimeForValidInput()
        {
            var input = "01/12/2025 15:30:45";

            var result = DateTimeHelper.ParseFlexible(input);

            Assert.Equal(2025, result.Year);
            Assert.Equal(12, result.Month);
            Assert.Equal(1, result.Day);
            Assert.Equal(15, result.Hour);
            Assert.Equal(30, result.Minute);
            Assert.Equal(45, result.Second);
        }

        [Fact]
        public void ParseFlexibleThrowsFormatExceptionForInvalidInput()
        {
            var invalid = "not a date";

            Assert.Throws<FormatException>(() => DateTimeHelper.ParseFlexible(invalid));
        }

        [Fact]
        public void TryParseFlexibleHandlesExtremeDates()
        {
            // test minimum value
            var min = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss");
            var successMin = DateTimeHelper.TryParseFlexible(min, out var dtMin);
            Assert.True(successMin);
            Assert.Equal(DateTime.MinValue, dtMin);

            // test maximum value
            var max = DateTime.MaxValue.ToString("yyyy-MM-dd HH:mm:ss");
            var successMax = DateTimeHelper.TryParseFlexible(max, out var dtMax);
            Assert.True(successMax);
            // not exactly max but almost (good enough)
            Assert.Equal(DateTime.Parse("9999-12-31T23:59:59.0000000"), dtMax);
        }

        [Fact]
        public void TryParseFlexibleHandlesNullOrEmptyStrings()
        {
            var successNull = DateTimeHelper.TryParseFlexible(null!, out var dtNull);
            Assert.False(successNull);
            Assert.Equal(default, dtNull);

            var successEmpty = DateTimeHelper.TryParseFlexible(string.Empty, out var dtEmpty);
            Assert.False(successEmpty);
            Assert.Equal(default, dtEmpty);
        }
    }
}
