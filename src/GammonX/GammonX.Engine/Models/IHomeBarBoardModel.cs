
namespace GammonX.Engine.Models
{
    /// <summary>
    /// Represents an extension for the homebar board in a backgammon variant.
    /// </summary>
    public interface IHomeBarBoardModel
    {
        /// <summary>
        /// Gets the amount of pieces for the white player on the home bar.
        /// </summary>
        /// <remarks>
        /// A piece is returned to the home bar when it is hit by an opponent's piece.
        /// </remarks>
        int HomeBarCountWhite { get; }

        /// <summary>
        /// Gets the amount of pieces for the black player on the home bar.
        /// </summary>
        /// <remarks>
        /// A piece is returned to the home bar when it is hit by an opponent's piece.
        /// </remarks>
        int HomeBarCountBlack { get; }

        /// <summary>
        /// Gets the start index for the white players pieces if on the bar.
        /// </summary>
        int StartIndexWhite { get; }

        /// <summary>
        /// Gets the start index for the black players pieces if on the bar.
        /// </summary>
        int StartIndexBlack { get; }
    }
}
