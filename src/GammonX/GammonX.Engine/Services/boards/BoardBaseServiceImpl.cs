using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    /// <summary>
    /// Provides base implementation for board services used in all variants.
    /// </summary>
    internal abstract class BoardBaseServiceImpl : IBoardService
    {
        // <inheritdoc />
        public abstract GameModus Modus { get; }

        // <inheritdoc />
        public abstract IBoardModel CreateBoard();

        // <inheritdoc />
        public ValueTuple<int, int>[] GetLegalMoves(IBoardModel model, bool isWhite, params int[] rolls)
        {
            // TODO: UNIT TESTS
            var legalMoves = new List<(int from, int to)>();
            // get only points with relevance for the current player
            var playerPoints = new List<int>();
            var homebarCount = GetHomeBarCount(model, isWhite);
            if (homebarCount > 0 && model is IHomeBarModel homeBarModel)
            {
                // if the player has pieces in the home bar, they can only move those pieces
                int startPoint = isWhite ? homeBarModel.StartIndexWhite : homeBarModel.StartIndexBlack;
                playerPoints.Add(startPoint);
            }
            else
            {
                for (int i = 0; i < model.Fields.Length; i++)
                {
                    int pointValue = model.Fields[i];
                    if ((isWhite && pointValue < 0) || (!isWhite && pointValue > 0))
                    {
                        playerPoints.Add(i);
                    }
                }
            }

            foreach (var roll in rolls)
            {
                foreach (var from in playerPoints)
                {
                    int to = model.MoveOperator(isWhite, from, roll);
                    if (CanMoveChecker(model, from, roll, isWhite))
                    {
                        legalMoves.Add((from, to));
                    }
                    if (model.CanBearOff(from, roll, isWhite))
                    {
                        var toBearOff = isWhite ? 24 : -1;
                        legalMoves.Add((from, to));
                    }
                }
            }

            return legalMoves.ToArray();
        }

        // <inheritdoc />
        public bool CanMoveChecker(IBoardModel model, int from, int roll, bool isWhite)
        {
            // TODO :: what happens when checker on bar?
            // TODO :: fevga start condition?
            var newPosition = model.MoveOperator(isWhite, from, roll);
            return model.CanMove(from, newPosition, isWhite);
        }

        // <inheritdoc />
        public virtual void MoveTo(IBoardModel model, int from, int to, bool isWhite)
        {
            // we check it here because most of the variants actually use hitting
            var homeBarModel = model as IHomeBarModel;
            if (homeBarModel != null)
            {
                // we check if we would hit an opponents checker with this move
                EvaluateHittedCheckers(model, from, to, isWhite);
            }

            if (isWhite)
            {           
                // remove a negative checker from the old position
                model.Fields.SetValue(model.Fields[from] += 1, from);
                // add a negative checker to the new position
                model.Fields.SetValue(model.Fields[to] -= 1, to);
            }
            else
            {
                // remove a positive checker from the old position
                model.Fields.SetValue(model.Fields[from] -= 1, from);
                // add a positive checker to the new position
                model.Fields.SetValue(model.Fields[to] += 1, to);
            }
        }

        // <inheritdoc />
        public bool MoveRoll(IBoardModel model, int from, int roll, bool isWhite)
        {
            // we probably can remove this later on
            if (!CanMoveChecker(model, from, roll, isWhite))
                throw new InvalidOperationException("Cannot move checker to the specified position.");

            try
            {
                var newPosition = model.MoveOperator(isWhite, from, roll);
                MoveTo(model, from, newPosition, isWhite);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving checker: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Evaluates if a checker on the target field is hit by the current move.
        /// </summary>
        /// <remarks>
        /// Can be overridden in derived classes to implement specific logic for determining a hit.
        /// </remarks>
        /// <param name="model">Model to operate on.</param>
        /// <param name="from">From move position.</param>
        /// <param name="to">To move position. Can contain an opponents checker.</param>
        /// <param name="isWhite">Indicates if white or black checker.</param>
        protected virtual void EvaluateHittedCheckers(IBoardModel model, int from, int to, bool isWhite)
        {
            // we know that the opponents checker can be hit, otherwise the move could not have been made
            if (isWhite)
            {
                // we detect a black checker on the target field
                if (model.Fields[to] > 0)
                {
                    HitChecker(model, to, isWhite);
                }
            }
            else
            {
                // we detect a white checker on the target field
                if (model.Fields[to] < 0)
                {
                    HitChecker(model, to, isWhite);
                }
            }
        }

        /// <summary>
        /// Is executed when a checker is hit by the current move.
        /// </summary>
        /// <remarks>
        /// Can be overridden in derived classes to implement specific logic for hitting a checker.
        /// </remarks>
        /// <param name="model">Model to operate on.</param>
        /// <param name="fieldIndex">Field index where the hit occurs.</param>
        /// <param name="isWhite">Indicates if white or black checker.</param>
        /// <exception cref="InvalidOperationException">Throws if given model does not support hitting.</exception>
        protected virtual void HitChecker(IBoardModel model, int fieldIndex, bool isWhite)
        {
            if (model is IHomeBarModel homeBarModel)
            {
                if (isWhite)
                {
                    // we remove the black checker from the moveable fields array
                    model.Fields.SetValue(model.Fields[fieldIndex] -= 1, fieldIndex);
                    // and move it to the black home bar
                    homeBarModel.AddToHomeBar(!isWhite, 1);
                }
                else
                {
                    // we remove the white checker from the moveable fields array
                    model.Fields.SetValue(model.Fields[fieldIndex] += 1, fieldIndex);
                    // and move it to the white home bar
                    homeBarModel.AddToHomeBar(!isWhite, 1);
                }
            }
            else
            {
                throw new InvalidOperationException("Model does not support hitting checkers.");
            }
        }

        private static int GetHomeBarCount(IBoardModel model, bool isWhite)
        {
            if (model is IHomeBarModel homebarBoard)
            {
                return isWhite ? homebarBoard.HomeBarCountWhite : homebarBoard.HomeBarCountBlack;
            }
            return 0;
        }
    }
}
