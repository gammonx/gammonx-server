namespace GammonX.Server.Extensions
{
    public static class TimeSpanExtensions
    {
        /// <summary>
        /// Gets the maximum of two TimeSpan values.
        /// </summary>
        /// <param name="a">Time span value A.</param>
        /// <param name="b">Time span value B.</param>
        /// <returns>Max value of the two given time spans.</returns>
        public static TimeSpan Max(this TimeSpan a, TimeSpan b) => a > b ? a : b;
    }
}
