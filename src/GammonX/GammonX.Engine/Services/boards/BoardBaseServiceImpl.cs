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
            // TODO: how to handle blocked pieces?
            // get only points with relevance for the current player
            var playerPoints = new List<int>();
            var homebarCount = GetHomeBarCount(model, isWhite);
            if (homebarCount > 0 && model is IHomeBarBoardModel homeBarModel)
            {
                // if the player has pieces in the home bar, they can only move those pieces
                int startPoint = isWhite ? homeBarModel.StartIndexWhite : homeBarModel.StartIndexBlack;
                playerPoints.Add(startPoint);
            }
            else
            {
                for (int i = 0; i < model.Points.Length; i++)
                {
                    int pointValue = model.Points[i];
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
                    int to = isWhite ? model.WhiteMoveOperator(from, roll) : model.BlackMoveOperator(from, roll);

                    if (CanMovePiece(model, from, roll, isWhite))
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
        public bool CanMovePiece(IBoardModel model, int from, int roll, bool isWhite)
        {
            if (isWhite)
            {
                var newPosition = model.WhiteMoveOperator(from, roll);
                return model.CanMove(from, newPosition, isWhite);
            }
            else
            {
                var newPosition = model.BlackMoveOperator(from, roll);
                return model.CanMove(from, newPosition, isWhite);
            }
        }

        // <inheritdoc />
        public void MoveTo(IBoardModel model, int from, int to, bool isWhite)
        {
            // TODO: UNIT TESTS
            if (isWhite)
            {
                // remove a negative piece from the old position
                model.Points.SetValue(model.Points[from] += 1, from);
                // add a negative piece to the new position
                model.Points.SetValue(model.Points[to] -= 1, to);
            }
            else
            {
                // remove a positive piece from the old position
                model.Points.SetValue(model.Points[from] -= 1, from);
                // add a positive piece to the new position
                model.Points.SetValue(model.Points[to] += 1, to);
            }
        }

        // <inheritdoc />
        public bool MoveRoll(IBoardModel model, int from, int roll, bool isWhite)
        {
            try
            {
                if (isWhite)
                {
                    var newPosition = model.WhiteMoveOperator(from, roll);
                    MoveTo(model, from, newPosition, isWhite);
                }
                else
                {
                    var newPosition = model.BlackMoveOperator(from, roll);
                    MoveTo(model, from, newPosition, isWhite);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving piece: {ex.Message}");
                return false;
            }
        }

        private int GetHomeBarCount(IBoardModel model, bool isWhite)
        {
            if (model is IHomeBarBoardModel homebarBoard)
            {
                return isWhite ? homebarBoard.HomeBarCountWhite : homebarBoard.HomeBarCountBlack;
            }
            return 0;
        }
    }
}
