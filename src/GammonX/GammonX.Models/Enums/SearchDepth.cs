namespace GammonX.Models.Enums;

/// <summary>
/// Represents the search depth for the neural network in the game.
/// </summary>
public enum SearchDepth
{
    /// <summary>
    /// Represents a search depth of one ply (single move lookahead).
    /// </summary>
    OnePly = 1,
    /// <summary>
    /// Represents a search depth of two ply (two moves lookahead).
    /// </summary>
    TwoPly = 2,
    /// <summary>
    /// Represents an unknown search depth.
    /// </summary>
    Unknown = 99
}