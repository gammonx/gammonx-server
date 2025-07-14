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
        int[] Points { get; }

        /// <summary>
        /// Gets the index of the home point for the white player.
        /// </summary>
        Range WhiteHome { get; }

        /// <summary>
        /// Gets the index of the home point for the black player.
        /// </summary>
        Range BlackHome { get; }

        /// <summary>
        /// Gets the amount of pieces for the white player that are currently borne off.
        /// </summary>
        int BearOffWhite { get; }

        /// <summary>
        /// Gets the amount of pieces for the black player that are currently borne off.
        /// </summary>
        int BearOffBlack { get; }

        /// <summary>
        /// Operator function for moving a piece for the white player.
        /// First int represents the current position of the piece,
        /// Second int represents the dice roll or move distance.
        /// Returns the new position of the piece after the move.
        /// </summary>
        Func<int, int, int> WhiteMoveOperator { get; }

        /// <summary>
        /// Operator function for moving a piece for the black player.
        /// First int represents the current position of the piece,
        /// Second int represents the dice roll or move distance.
        /// Returns the new position of the piece after the move.
        /// </summary>
        Func<int, int, int> BlackMoveOperator { get; }
    }
}
