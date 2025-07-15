using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class BackgammonBoardService : BoardBaseServiceImpl
    {
        // <inheritdoc />
        public override GameModus Modus => GameModus.Backgammon;

        // <inheritdoc />
        public override IBoardModel CreateBoard()
        {
            return new BackgammonBoardModelImpl();
        }

        // <inheritdoc />
        protected override void EvaluateHittedCheckers(IBoardModel model, int from, int to, bool isWhite)
        {
            // backgammon uses the normal mechanism when hitting checkers
            base.EvaluateHittedCheckers(model, from, to, isWhite);
        }

        // <inheritdoc />
        protected override void HitChecker(IBoardModel model, int fieldIndex, bool isWhite)
        {
            // backgammon uses the normal mechanism when hitting checkers
            base.HitChecker(model, fieldIndex, isWhite);
        }
    }
}