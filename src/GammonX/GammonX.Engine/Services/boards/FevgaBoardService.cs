using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class FevgaBoardService : IBoardService
    {
        // <inheritdoc />
        public GameModus Modus => GameModus.Fevga;

        // <inheritdoc />
        public IBoardModel CreateBoard()
        {
            return new FevgaBoardModelImpl();
        }
    }
}