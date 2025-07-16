
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
        /// <remarks>
        /// This index may not directly be applied for the fields array,normally
        /// it is outside of the array bounds and. For white checkers the value is e.g.
        /// -1 and would move to index 0 with a dice roll value of 1
        /// </remarks>
        int StartIndexWhite { get; }

        /// <summary>
        /// Gets the start index for the black players pieces if on the bar.
        /// </summary>
        /// <remarks>
        /// This index may not directly be applied for the fields array,normally
        /// it is outside of the array bounds and. For white checkers the value is e.g.
        /// 24 and would move to index 23 with a dice roll value of 1
        /// </remarks>
        int StartIndexBlack { get; }

        /// <summary>
        /// Adds the given amount of pieces to the home bar for the specified player.
        /// </summary>
        /// <param name="isWhite">Indicates the player, true for white and false for black.</param>
        /// <param name="amount">Amount of checkers sent to the homebar. Normally just one.</param>
        void AddToHomeBar(bool isWhite, int amount);

        /// <summary>
        /// Removes the given amount of pieces from the home bar for the specified player.
        /// </summary>
        /// <param name="isWhite">Indicates the player, true for white and false for black.</param>
        /// <param name="amount">Amount of checkers removed from the homebar. Normally just one.</param>
        void RemoveFromHomeBar(bool isWhite, int amount);
    }
}
