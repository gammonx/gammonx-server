using GammonX.Engine.Models;

using GammonX.Models.Enums;

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

        // <inheritdoc />
        public override void MoveCheckerTo(IBoardModel model, int from, int to, bool isWhite)
        {
            base.MoveCheckerTo(model, from, to, isWhite);
        }

        // <inheritdoc />
        public override bool CanMoveChecker(IBoardModel model, int from, int roll, bool isWhite)
        {
            if (model.MustEnterFromHomeBar(isWhite) && !model.EntersFromHomeBar(from, isWhite))
            {
                // if a checker is on the homebar it was removed from the playing fields
                // no other checker can be played until the homebar is empty
                return false;
            }

            return base.CanMoveChecker(model, from, roll, isWhite);
        }

        // <inheritdoc />
        protected override void EvaluateHittedCheckers(IBoardModel model, int from, int to, bool isWhite)
        {
            // portes uses the normal mechanism when hitting checkers
            base.EvaluateHittedCheckers(model, from, to, isWhite);
        }

        // <inheritdoc />
        protected override void HitChecker(IBoardModel model, int fieldIndex, bool isWhite)
        {
            // portes uses the normal mechanism when hitting checkers
            base.HitChecker(model, fieldIndex, isWhite);
        }
    }
}