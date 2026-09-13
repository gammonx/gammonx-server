using GammonX.Models.Helpers;

using System.Globalization;

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
        public void TryParseMatUtcParsesCommonFormatsCorrectly(string input)
        {
            var success = DateTimeHelper.TryParseMatUtc(input, out var result);

            Assert.True(success);
            Assert.NotEqual(default, result);
        }

        [Theory]
        [InlineData("2025-12-01 15:30:45")]
        [InlineData("2025/12/01 15:30:45")]
        [InlineData("12-01-2025 15:30:45")]
        public void TryParseMatUtcParsesAdditionalFormats(string input)
        {
            var success = DateTimeHelper.TryParseMatUtc(input, out var result);

            Assert.True(success);
            Assert.NotEqual(default, result);
        }

        [Fact]
        public void TryParseMatUtcReturnsFalseForInvalidInput()
        {
            var invalid = "not a date";

            var success = DateTimeHelper.TryParseMatUtc(invalid, out var result);

            Assert.False(success);
            Assert.Equal(default, result);
        }

        [Fact]
        public void ParseMatUtcReturnsDateTimeForValidInput()
        {
            var input = "01/12/2025 15:30:45";

            var result = DateTimeHelper.ParseMatUtc(input);

            Assert.Equal(2025, result.Year);
            Assert.Equal(12, result.Month);
            Assert.Equal(1, result.Day);
            Assert.Equal(15, result.Hour);
            Assert.Equal(30, result.Minute);
            Assert.Equal(45, result.Second);
        }

        [Fact]
        public void ParseMatUtcThrowsFormatExceptionForInvalidInput()
        {
            var invalid = "not a date";

            Assert.Throws<FormatException>(() => DateTimeHelper.ParseMatUtc(invalid));
        }

        [Fact]
        public void TryParseMatUtcHandlesExtremeDates()
        {
            // test minimum value
            var min = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss");
            var successMin = DateTimeHelper.TryParseMatUtc(min, out var dtMin);
            Assert.True(successMin);
            Assert.Equal(DateTime.MinValue, dtMin);

            // test maximum value
            var max = DateTime.MaxValue.ToString("yyyy-MM-dd HH:mm:ss");
            var successMax = DateTimeHelper.TryParseMatUtc(max, out var dtMax);
            Assert.True(successMax);
            // not exactly max but almost (good enough)
            Assert.Equal(DateTime.Parse("9999-12-31T23:59:59.0000000"), dtMax);
        }

        [Fact]
        public void TryParseMatUtcHandlesNullOrEmptyStrings()
        {
            var successNull = DateTimeHelper.TryParseMatUtc(null!, out var dtNull);
            Assert.False(successNull);
            Assert.Equal(default, dtNull);

            var successEmpty = DateTimeHelper.TryParseMatUtc(string.Empty, out var dtEmpty);
            Assert.False(successEmpty);
            Assert.Equal(default, dtEmpty);
        }

        [Fact]
        public void FormatUtcUsesCanonicalInvariantFormat()
        {
            var value = new DateTime(2026, 9, 10, 16, 56, 33, 224, DateTimeKind.Utc).AddTicks(5744);

            var result = DateTimeHelper.FormatUtc(value);

            Assert.Equal("2026-09-10T16:56:33.2245744Z", result);
        }

        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void FormatUtcIsIndependentOfCurrentCulture(string cultureName)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

                var value = new DateTime(2026, 9, 10, 16, 56, 33, 224, DateTimeKind.Utc).AddTicks(5744);

                Assert.Equal("2026-09-10T16:56:33.2245744Z", DateTimeHelper.FormatUtc(value));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Fact]
        public void FormatUtcRejectsUnspecifiedKind()
        {
            var value = new DateTime(2026, 9, 10, 16, 56, 33, DateTimeKind.Unspecified);

            Assert.Throws<ArgumentException>(() => DateTimeHelper.FormatUtc(value));
        }

        [Theory]
        [InlineData("2026-09-10T16:56:33.2245744Z", 16, 56)]
        [InlineData("2026-09-10T18:56:33.2245744+02:00", 16, 56)]
        public void ParseUtcReturnsUtcInstant(string input, int expectedHour, int expectedMinute)
        {
            var result = DateTimeHelper.ParseUtc(input);

            Assert.Equal(DateTimeKind.Utc, result.Kind);
            Assert.Equal(new DateTime(2026, 9, 10, expectedHour, expectedMinute, 33, DateTimeKind.Utc).AddTicks(2245744), result);
        }

        [Fact]
        public void ParseMatUtcTreatsTimezoneLessInputAsUtc()
        {
            var result = DateTimeHelper.ParseMatUtc("29.11.2025 09:33:41");

            Assert.Equal(DateTimeKind.Utc, result.Kind);
            Assert.Equal(new DateTime(2025, 11, 29, 9, 33, 41, DateTimeKind.Utc), result);
        }

        [Fact]
        public void DurationTicksRoundTripPreservesSubMillisecondPrecision()
        {
            var value = TimeSpan.FromDays(2) + TimeSpan.FromMilliseconds(1.3333333);

            var serialized = DateTimeHelper.FormatDurationTicks(value);
            var result = DateTimeHelper.ParseDurationTicks(serialized);

            Assert.Equal(value, result);
            Assert.Equal(value.Ticks.ToString(CultureInfo.InvariantCulture), serialized);
        }

        [Fact]
        public void ParseDurationTicksRejectsNegativeValues()
        {
            Assert.Throws<FormatException>(() => DateTimeHelper.ParseDurationTicks("-1"));
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("de-DE")]
        public void MatParsingIsIndependentOfCurrentCulture(string cultureName)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

                var result = DateTimeHelper.ParseMatUtc("29.11.2025 09:33:41");

                Assert.Equal(new DateTime(2025, 11, 29, 9, 33, 41, DateTimeKind.Utc), result);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }
    }
}
