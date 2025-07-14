namespace GammonX.Engine.Models
{
    /// <summary>
    /// Represents an extension for variants which do block instead of hitting pieces.
    /// </summary>
    public interface IBlockedModel
    {
        /// <summary>
        /// Gets an array representing the points on the board which are blocked by the opponent.
        /// </summary>
        /// <remarks>
        /// Negative numbers in the array represent the number of pieces for the white player, 
        /// while positive numbers represent the peices for the black player.
        /// </remarks>
        int[] BlockedPoints { get; }
    }
}
