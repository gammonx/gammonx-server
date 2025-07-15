namespace GammonX.Engine.Models
{
    /// <summary>
    /// Represents the basic model of a game board in a backgammon variant.
    /// </summary>
    public interface IBoardModel
    {
        /// <summary>
        /// Gets the game mode on which the board is based.
        /// </summary>
        GameModus Modus { get; }

        /// <summary>
        /// Gets an array representing the points on the board.
        /// </summary>
        /// <remarks>
        /// Negative numbers in the array represent the number of pieces for the white player, 
        /// while positive numbers represent the peices for the black player.
        /// </remarks>
        int[] Fields { get; }

        /// <summary>
        /// Gets the index range of the home point for the white player.
        /// If all checkers are within the home range, the player can bear off pieces.
        /// </summary>
        Range HomeRangeWhite { get; }

        /// <summary>
        /// Gets the index range of the home point for the black player.
        /// If all checkers are within the home range, the player can bear off pieces.
        /// </summary>
        Range HomeRangeBlack { get; }

        /// <summary>
        /// Gets the amount of pieces for the white player that are currently borne off.
        /// </summary>
        int BearOffCountWhite { get; }

        /// <summary>
        /// Gets the amount of pieces for the black player that are currently borne off.
        /// </summary>
        int BearOffCountBlack { get; }

        /// <summary>
        /// Gets the amount of pieces needed in order to block a point.
        /// </summary>
        int BlockAmount { get; }

        /// <summary>
        /// Operator function for moving a checker for the given player.
        /// First parameter true for white player, false for black player.
        /// Second int represents the current position of the checker,
        /// Third int represents the dice roll or move distance.
        /// Returns the new position of the checker after the move.
        /// </summary>
        Func<bool, int, int, int> MoveOperator { get; }

        /// <summary>
        /// Operator function to check if a checker can be borne off for the given player.
        /// First parameter true for white player, false for black player.
        /// Second int represents the current position of the checker,
        /// Third int represents the dice roll or move distance.
        /// Returns true if the checker can be borne off, otherwise false.
        /// </summary>
        Func<bool, int, int, bool> CanBearOffOperator { get; }

        /// <summary>
        /// Operator function to check if a given checker is within the home range.
        /// First parameter true for white player, false for black player.
        /// Second int represents the current position of the checker.
        /// Returns true if the checker is within the home range, otherwise false.
        /// </summary>
        Func<bool, int, bool> IsInHomeOperator { get; }
    }
}
