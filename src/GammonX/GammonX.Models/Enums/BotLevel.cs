namespace GammonX.Models.Enums
{
    /// <summary>
    /// Describes the difficulty playing level of a bot service.
    /// </summary>
    public enum BotLevel
    {
        /// <summary>
        /// Represents the easiest bot level.
        /// </summary>
        Easy = 0,
        /// <summary>
        /// Represents the intermediate bot level.
        /// </summary>
        Medium = 1,
        /// <summary>
        /// Represents the hard bot level.
        /// </summary>
        Hard = 2,
        /// <summary>
        /// Represents the bot level which makes use of 2ply deep search.
        /// </summary>
        TwoPly = 3,
        /// <summary>
        /// Unknown bot level, used for error handling and default values. Should not be used in actual bot services.
        /// </summary>
        Unknown = 99,
    }
}
