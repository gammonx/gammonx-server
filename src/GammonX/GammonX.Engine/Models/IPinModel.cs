namespace GammonX.Engine.Models
{
    /// <summary>
    /// Represents an extension for variants which do pin instead of hitting pieces.
    /// </summary>
    public interface IPinModel
    {
        /// <summary>
        /// Gets an array representing the points on the board which are pinned by the opponent.
        /// </summary>
        /// <remarks>
        /// Negative numbers in the array represent the number of pieces for the white player, 
        /// while positive numbers represent the peices for the black player.
        /// </remarks>
        int[] PinnedFields { get; }
    }
}
