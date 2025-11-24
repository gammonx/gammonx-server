namespace GammonX.Models.Enums
{
    /// <summary>
    /// Represents the different game modes available in the GammonX engine.
    /// </summary>
    public enum GameModus
    {
        /// <summary>
        /// Classic Backgammon game mode.
        /// </summary>
        Backgammon = 0,
        /// <summary>
        /// Represents the Tavla game type. Similar to Backgammon but without doubling cube.
        /// </summary>
        Tavla = 1,
        /// <summary>
        /// Represents the first game type of a Tavlia match.
        /// </summary>
        Portes = 2,
        /// <summary>
        /// Represents the second game type of a Tavlia match.
        /// </summary>
        Plakoto = 3,
        /// <summary>
        /// Represents the third game type of a Tavlia match.
        /// </summary>
        Fevga = 4,
    }
}
