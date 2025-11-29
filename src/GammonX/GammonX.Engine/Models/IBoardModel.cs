using GammonX.Engine.History;

using GammonX.Models.Enums;

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
        /// Gets the history of all events happened on the given board.
        /// </summary>
        IBoardHistory History { get; }

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
		/// Gets the index range of the starting point for the white player.
		/// </summary>
		Range StartRangeWhite { get; }

		/// <summary>
		/// Gets the index range of the starting point for the black player.
		/// </summary>
		Range StartRangeBlack { get; }

		/// <summary>
		/// Gets the amount of pieces for the white player that are currently borne off.
		/// </summary>
		int BearOffCountWhite { get; }

        /// <summary>
        /// Gets the amount of pieces for the black player that are currently borne off.
        /// </summary>
        int BearOffCountBlack { get; }

		/// <summary>
		/// Gets the amount of checkers needed to borne off in order to win the game.
		/// </summary>
		int WinConditionCount { get; }

		/// <summary>
		/// Gets the amount of pieces needed in order to block a point.
		/// </summary>
		int BlockAmount { get; }

        /// <summary>
        /// Gets the remaining pip count needed for the white player to bear off all checkers.
        /// </summary>
        int PipCountWhite { get; }

		/// <summary>
		/// Gets the remaining pip count needed for the black player to bear off all checkers.
		/// </summary>
		int PipCountBlack { get; }

		/// <summary>
		/// Operator function for moving a checker for the given player.
		/// First parameter <c>true</c> for white player, <c>false</c> for black player.
		/// Second int represents the current position of the checker,
		/// Third int represents the dice roll or move distance.
		/// Returns the new position of the checker after the move.
		/// </summary>
		Func<bool, int, int, int> MoveOperator { get; }

		/// <summary>
		/// Operator function for lookup the roll for a given from/to move.
		/// First parameter <c>true</c> for white player, <c>false</c> for black player.
		/// Second int represents the from position index,
		/// Third int represents the to position index.
		/// Returns the roll value which was used for the given move.
		/// </summary>
		Func<bool, int, int, int> RecoverRollOperator { get; }

		/// <summary>
		/// Operator function to check if a checker can be borne off for the given player.
		/// First parameter <c>true</c> for white player, <c>false</c> for black player.
		/// Second int represents the current position of the checker,
		/// Third int represents the dice roll or move distance.
		/// Returns <c>true</c> if the checker can be borne off, otherwise <c>false</c>.
		/// </summary>
		Func<bool, int, int, bool> CanBearOffOperator { get; }

		/// <summary>
		/// Operator function to check if a given checker is within the home range.
		/// First parameter <c>true</c> for white player, <c>false</c> for black player.
		/// Second int represents the current position of the checker.
		/// Returns <c>true</c> if the checker is within the home range, otherwise <c>false</c>.
		/// </summary>
		Func<bool, int, bool> IsInHomeOperator { get; }

		/// <summary>
		/// Bears off a checker for the specified player.
		/// </summary>
		/// <param name="isWhite">Indicates the player, <c>true</c> for white and <c>false</c> for black.</param>
		/// <param name="amount">Amount of checkers borne off. Normally just one.</param>
		void BearOffChecker(bool isWhite, int amount);

		/// <summary>
		/// Inverts this instance of the board horizontally (white>black/black>white) for the other player.
		/// After inverting, all positions and counts from black are now white and
		/// all positions and counts from white are now black.
		/// </summary>
		/// <remarks>
		/// The engine only handles one view of the board (e.g. for player1).
        /// When the board is sent to the other player (e.g. player2) the boards gets inverted.
        /// All clients plays as white in their point of view.
		/// </remarks>
		/// <returns>An inverted board instance.</returns>
		IBoardModel InvertBoard();
	}
}
