
namespace GammonX.Engine.Models
{
    /// <summary>
    /// Represents an extension for the bear-off board in a backgammon variant.
    /// </summary>
    public interface IBearOffBoardModel
    {
        /// <summary>
        /// Gets the amount of pieces for the white player on the home bar.
        /// </summary>
        /// <remarks>
        /// A piece is returned to the home bar when it is hit by an opponent's piece.
        /// </remarks>
        int BarWhite { get; }

        /// <summary>
        /// Gets the amount of pieces for the black player on the home bar.
        /// </summary>
        /// <remarks>
        /// A piece is returned to the home bar when it is hit by an opponent's piece.
        /// </remarks>
        int BarBlack { get; }
    }
}
