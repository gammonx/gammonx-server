using GammonX.Models.Enums;

namespace GammonX.Engine.Services
{
    /// <summary>
    /// Helper service to create different board setups for the given modus.
    /// </summary>
    public static class BoardServiceFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="IBoardService"/> for the specified game mode.
        /// </summary>
        /// <param name="modus">The game mode for which to create the board service.</param>
        /// <returns>An instance of <see cref="IBoardService"/>.</returns>
        public static IBoardService Create(GameModus modus)
        {
            return modus switch
            {
                GameModus.Backgammon => new BackgammonBoardService(),
                GameModus.Tavla => new TavlaBoardService(),
                GameModus.Portes => new PortesBoardService(),
                GameModus.Plakoto => new PlakotoBoardService(),
                GameModus.Fevga => new FevgaBoardService(),
                _ => throw new ArgumentOutOfRangeException(nameof(modus), modus, "Unsupported game mode")
            };
        }
    }
}
