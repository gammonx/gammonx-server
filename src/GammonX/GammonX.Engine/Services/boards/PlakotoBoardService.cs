using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class PlakotoBoardService : IBoardService
    {
        // <inheritdoc />
        public GameModus Modus => GameModus.Plakoto;

        // <inheritdoc />
        public IBoardModel CreateBoard()
        {
            return new PlakotoBoardModelImpl();
        }
    }
}