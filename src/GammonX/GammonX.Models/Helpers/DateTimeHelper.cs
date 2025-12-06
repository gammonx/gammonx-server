using System.Globalization;

namespace GammonX.Models.Helpers
{
    public static class DateTimeHelper
    {
        // TODO unit tests

        private static readonly string[] CommonFormats = new[]
        {
            "dd/MM/yyyy HH:mm:ss",
            "d/M/yyyy H:mm:ss",
            "MM/dd/yyyy HH:mm:ss",
            "M/d/yyyy H:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyyMMddHHmmss",
            "dd.MM.yyyy HH:mm:ss"
            // add more formats as needed
        };

        public static bool TryParseFlexible(string input, out DateTime result)
        {
            // Try exact formats first
            if (DateTime.TryParseExact(input, CommonFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;

            // Fallback to general parse
            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return true;

            // As a last resort, try system culture (might depend on container)
            if (DateTime.TryParse(input, out result))
                return true;

            result = default;
            return false;
        }

        public static DateTime ParseFlexible(string input)
        {
            if (!TryParseFlexible(input, out var dt))
                throw new FormatException($"Unable to parse '{input}' as a DateTime.");
            return dt;
        }
    }
}
