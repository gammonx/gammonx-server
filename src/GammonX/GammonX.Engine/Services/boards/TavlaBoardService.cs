﻿using GammonX.Engine.Models;

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