using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    /// <summary>
    /// Helper service to create different board setups for the given modus.
    /// </summary>
    public interface IBoardService
    {
        /// <summary>
        /// Gets the game mode on which the created board are based on.
        /// </summary>
        GameModus Modus { get; }

        /// <summary>
        /// Creates a new board with the initial setup for a game of the given modus.
        /// </summary>
        /// <remarks>
        /// Negative numbers in the array represent the number of pieces for the white player, 
        /// while positive numbers represent the peices for the black player.
        /// </remarks>
        /// <returns>An instance of <see cref="IBoardModel"/>.</returns>
        IBoardModel CreateBoard();

        /// <summary>
        /// Moves a piece from one position to another on the board.
        /// </summary>
        /// <param name="model">The board model to operate on.</param>
        /// <param name="from">Position to move from.</param>
        /// <param name="roll">Value of the used dice roll.</param>
        /// <param name="isWhite">Indicates if white or black pieces move.</param>
        /// <remarks>
        /// This method does not validate the move according to the game rules.
        /// All move done are validated beforehand by the game logic.
        /// Use <see cref="CanMovePiece"/> for validation.
        /// </remarks>
        /// <returns>Indicates success of the move.</returns>
        bool MovePiece(IBoardModel model, int from, int roll, bool isWhite);

        /// <summary>
        /// Checks if the given piece can be moved from to a given position based
        /// on the roll.
        /// </summary>
        /// <param name="model">The board model to operate on.</param>
        /// <param name="from">Position to move from.</param>
        /// <param name="roll">Value of the used dice roll.</param>
        /// <param name="isWhite">Indicates if the white or black pieces move.</param>
        /// <returns>Indicates if the given dice roll value can be moved from.</returns>
        bool CanMovePiece(IBoardModel model, int from, int roll, bool isWhite);
    }
}
