namespace GammonX.Engine.Services
{
    /// <summary>
    /// Classifies a turn continuation for the engine's dice-use rules.
    /// </summary>
    internal enum DiceUse
    {
        /// <summary>
        /// The continuation leaves dice unused and does not spend the highest die.
        /// </summary>
        Partial,

        /// <summary>
        /// The continuation spends the highest die and leaves at least one die unused.
        /// </summary>
        Highest,

        /// <summary>
        /// The continuation spends every available die.
        /// </summary>
        All
    }
}
