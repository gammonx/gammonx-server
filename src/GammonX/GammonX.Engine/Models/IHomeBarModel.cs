
namespace GammonX.Engine.Models
{
    /// <summary>
    /// Represents an extension for the homebar board in a backgammon variant.
    /// </summary>
    public interface IHomeBarModel
    {
        /// <summary>
        /// Gets the amount of pieces for the white player on the home bar.
        /// </summary>
        /// <remarks>
        /// A checker is returned to the home bar when it is hit by an opponent's checker.
        /// The count for white checkers on the homebar is positive.
        /// </remarks>
        int HomeBarCountWhite { get; }

        /// <summary>
        /// Gets the amount of pieces for the black player on the home bar.
        /// </summary>
        /// <remarks>
        /// A checker is returned to the home bar when it is hit by an opponent's checker.
        /// The count for black checkers on the homebar is positive.
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

        /// <summary>
        /// Adds the given amount of pieces to the home bar for the specified player.
        /// </summary>
        /// <param name="isWhite">Indicates the player, true for white and false for black.</param>
        /// <param name="amount">Amount of checkers sent to the homebar. Normally just one.</param>
        void AddToHomeBar(bool isWhite, int amount);
    }
}
