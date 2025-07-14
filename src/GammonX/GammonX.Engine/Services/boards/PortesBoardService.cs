using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class PortesBoardService : BoardBaseServiceImpl
    {
        // <inheritdoc />
        public override GameModus Modus => GameModus.Portes;

        // <inheritdoc />
        public override IBoardModel CreateBoard()
        {
            return new PortesBoardModelImpl();
        }
    }
}