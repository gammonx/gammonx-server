using System.Globalization;

namespace GammonX.Models.Helpers
{
    /// <summary>
    /// Provides culture-independent helpers for UTC timestamps and durations.
    /// </summary>
    public static class DateTimeHelper
    {
        public const string CanonicalUtcFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

        private static readonly string[] UtcFormats =
        {
            CanonicalUtcFormat,
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.fffffffzzz",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
            "yyyy-MM-dd'T'HH:mm:sszzz"
        };

        private static readonly string[] MatFormats =
        {
            "dd/MM/yyyy HH:mm:ss",
            "d/M/yyyy H:mm:ss",
            "d/M/yyyy H:m:s",
            "MM/dd/yyyy HH:mm:ss",
            "M/d/yyyy H:mm:ss",
            "M/d/yyyy H:m:s",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
            "dd-MM-yyyy HH:mm:ss",
            "MM-dd-yyyy HH:mm:ss",
            "yyyyMMddHHmmss",
            "dd.MM.yyyy HH:mm:ss"
        };

        private const DateTimeStyles AssumeUtcStyles =
            DateTimeStyles.AllowWhiteSpaces |
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal;

        public static DateTime RequireUtc(DateTime value, string paramName = "value")
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => throw new ArgumentException("The timestamp must have DateTimeKind.Utc or DateTimeKind.Local.", paramName)
            };
        }

        public static string FormatUtc(DateTime value)
        {
            return RequireUtc(value).ToString(CanonicalUtcFormat, CultureInfo.InvariantCulture);
        }

        public static bool TryParseUtc(string? input, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (!DateTimeOffset.TryParseExact(input, UtcFormats, CultureInfo.InvariantCulture, AssumeUtcStyles, out var parsed))
                return false;

            result = parsed.UtcDateTime;
            return true;
        }

        public static DateTime ParseUtc(string input)
        {
            if (!TryParseUtc(input, out var result))
                throw new FormatException($"Unable to parse '{input}' as a canonical UTC timestamp.");
            return result;
        }

        public static bool TryParseMatUtc(string? input, out DateTime result)
        {
            if (TryParseUtc(input, out result))
                return true;

            if (string.IsNullOrWhiteSpace(input) ||
                !DateTime.TryParseExact(input, MatFormats, CultureInfo.InvariantCulture, AssumeUtcStyles, out result))
            {
                result = default;
                return false;
            }

            result = DateTime.SpecifyKind(result, DateTimeKind.Utc);
            return true;
        }

        public static DateTime ParseMatUtc(string input)
        {
            if (!TryParseMatUtc(input, out var result))
                throw new FormatException($"Unable to parse '{input}' as a MAT UTC timestamp.");
            return result;
        }

        public static string FormatDurationTicks(TimeSpan value)
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value), "A persisted duration cannot be negative.");

            return value.Ticks.ToString(CultureInfo.InvariantCulture);
        }

        public static TimeSpan ParseDurationTicks(string input)
        {
            if (!long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks) || ticks < 0)
                throw new FormatException($"Unable to parse '{input}' as a non-negative duration in ticks.");

            return TimeSpan.FromTicks(ticks);
        }

    }
}
