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
        /// Represents the best bot level, which should be the most difficult to play against.
        /// </summary>
        Hard = 2,
        /// <summary>
        /// Unknown bot level, used for error handling and default values. Should not be used in actual bot services.
        /// </summary>
        Unknown = 99,
    }
}
