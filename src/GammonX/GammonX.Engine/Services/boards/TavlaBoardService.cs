using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class TavlaBoardService : BoardBaseServiceImpl
    {
        // <inheritdoc />
        public override GameModus Modus => GameModus.Tavla;

        // <inheritdoc />
        public override IBoardModel CreateBoard()
        {
            return new TavlaBoardModelImpl();
        }
    }
}