namespace GammonX.Mars.Training.Generator
{
    /// <summary>
    /// Specifies the exploration choice for a given turn.
    /// </summary>
    public enum ExplorationChoice
    {
        /// <summary>
        /// Selects the greedy move (the highest-ranked move).
        /// </summary>
        Greedy,
        /// <summary>
        /// Selects a move from the top-ranked moves.
        /// </summary>
        Ranked,
        /// <summary>
        /// Selects a move from all legal moves, regardless of rank.
        /// </summary>
        AllLegal
    }
}
