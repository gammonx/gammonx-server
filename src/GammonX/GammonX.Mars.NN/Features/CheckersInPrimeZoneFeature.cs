using GammonX.Engine.Models;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Counts the amount of checkers for the given player in the prime zone (e.g. mid board)
    /// </summary>
    /// <remarks>
    /// Only applicable for Fevga at the moment based on the prime zone definition.
    /// </remarks>
    public class CheckersInPrimeZoneFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            if (board.Modus == GameModus.Fevga)
            {
                var primeZoneStart = isWhite
                    ? board.StartRangeBlack.Start.Value
                    : board.StartRangeWhite.Start.Value;
                var primeZoneEnd = isWhite
                    ? board.StartRangeBlack.End.Value + 6
                    : board.StartRangeWhite.End.Value + 6;
                return SumCheckersInRange(board, isWhite, primeZoneStart, primeZoneEnd);
            }

            var standardZoneStart = board.StartRangeWhite.End.Value + 1;
            var standardZoneEnd = board.StartRangeBlack.End.Value - 1;
            return SumCheckersInRange(board, isWhite, standardZoneStart, standardZoneEnd);
        }

        private static int SumCheckersInRange(IBoardModel board, bool isWhite, int start, int end)
        {
            var checkers = 0;
            for (var index = start; index <= end; index++)
            {
                var field = board.Fields[index];
                if (isWhite && field < 0)
                {
                    checkers += field;
                }
                else if (!isWhite && field > 0)
                {
                    checkers += field;
                }
            }

            return isWhite ? Math.Abs(checkers) : checkers;
        }
    }
}
