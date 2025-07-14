using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class PlakotoBoardService : BoardBaseServiceImpl
    {
        // <inheritdoc />
        public override GameModus Modus => GameModus.Plakoto;

        // <inheritdoc />
        public override IBoardModel CreateBoard()
        {
            return new PlakotoBoardModelImpl();
        }
    }
}