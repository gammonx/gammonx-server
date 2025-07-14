using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    /// <summary>
    /// Provies base implementation for board services used in all variants.
    /// </summary>
    internal abstract class BoardBaseServiceImpl : IBoardService
    {
        // <inheritdoc />
        public abstract GameModus Modus { get; }

        // <inheritdoc />
        public abstract IBoardModel CreateBoard();

        // <inheritdoc />
        public bool CanMovePiece(IBoardModel model, int from, int roll, bool isWhite)
        {
            if (isWhite)
            {
                var newPosition = model.WhiteMoveOperator(from, roll);
                return BoardBroker.CanMove(model, from, newPosition, isWhite);
            }
            else
            {
                var newPosition = model.BlackMoveOperator(from, roll);
                return BoardBroker.CanMove(model, from, newPosition, isWhite);
            }
        }

        // <inheritdoc />
        public bool MovePiece(IBoardModel model, int from, int roll, bool isWhite)
        {
            try
            {
                if (isWhite)
                {
                    var newPosition = model.WhiteMoveOperator(from, roll);
                    // remove a negative piece from the old position
                    model.Points.SetValue(model.Points[from] += 1, from);
                    // add a negative piece to the new position
                    model.Points.SetValue(model.Points[newPosition] -= 1, newPosition);
                }
                else
                {
                    var newPosition = model.BlackMoveOperator(from, roll);
                    // remove a positive piece from the old position
                    model.Points.SetValue(model.Points[from] -= 1, from);
                    // add a positive piece to the new position
                    model.Points.SetValue(model.Points[newPosition] += 1, newPosition);
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving piece: {ex.Message}");
                return false;
            }
        }
    }
}
