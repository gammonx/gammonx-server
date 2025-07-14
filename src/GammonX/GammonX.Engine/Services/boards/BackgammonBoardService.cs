using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class BackgammonBoardService : IBoardService
    {
        // <inheritdoc />
        public GameModus Modus => GameModus.Backgammon;

        // <inheritdoc />
        public IBoardModel CreateBoard()
        {
            return new BackgammonBoardModelImpl();
        }
    }
}