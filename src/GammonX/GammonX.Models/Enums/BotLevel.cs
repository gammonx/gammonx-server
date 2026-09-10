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
        Expert = 3,
        /// <summary>
        /// Unknown bot level, used for error handling and default values. Should not be used in actual bot services.
        /// </summary>
        Unknown = 99,
    }

    /// <summary>
    /// Provides extension methods for the BotLevel enum.
    /// </summary>
    public static class BotLevelExtensions
    {
        /// <summary>
        /// Converts the bot level to its corresponding search depth.
        /// </summary>
        /// <param name="botLevel">The bot level to convert.</param>
        /// <returns>The corresponding search depth for the given bot level.</returns>
        public static SearchDepth ToSearchDepth(this BotLevel botLevel)
        {
            return botLevel switch
            {
                BotLevel.Easy => SearchDepth.OnePly,
                BotLevel.Medium => SearchDepth.OnePly,
                BotLevel.Hard => SearchDepth.OnePly,
                BotLevel.Expert => SearchDepth.TwoPly,
                _ => SearchDepth.Unknown,
            };
        }
    }
}