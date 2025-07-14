using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class PortesBoardService : IBoardService
    {
        // <inheritdoc />
        public GameModus Modus => GameModus.Portes;

        // <inheritdoc />
        public IBoardModel CreateBoard()
        {
            return new PortesBoardModelImpl();
        }
    }
}