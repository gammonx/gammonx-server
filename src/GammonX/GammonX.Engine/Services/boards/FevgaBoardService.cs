using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class FevgaBoardService : BoardBaseServiceImpl
    {
        // <inheritdoc />
        public override GameModus Modus => GameModus.Fevga;

        // <inheritdoc />
        public override IBoardModel CreateBoard()
        {
            return new FevgaBoardModelImpl();
        }
    }
}