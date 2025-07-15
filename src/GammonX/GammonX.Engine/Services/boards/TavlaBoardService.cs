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

        // <inheritdoc />
        protected override void EvaluateHittedCheckers(IBoardModel model, int from, int to, bool isWhite)
        {
            // tavla uses the normal mechanism when hitting checkers
            base.EvaluateHittedCheckers(model, from, to, isWhite);
        }

        // <inheritdoc />
        protected override void HitChecker(IBoardModel model, int fieldIndex, bool isWhite)
        {
            // tavla uses the normal mechanism when hitting checkers
            base.HitChecker(model, fieldIndex, isWhite);
        }
    }
}