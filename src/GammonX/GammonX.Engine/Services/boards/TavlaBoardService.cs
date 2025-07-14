using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class TavlaBoardService : IBoardService
    {
        // <inheritdoc />
        public GameModus Modus => GameModus.Tavla;

        // <inheritdoc />
        public IBoardModel CreateBoard()
        {
            return new TavlaBoardModelImpl();
        }
    }
}