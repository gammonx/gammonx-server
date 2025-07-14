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
    }
}
