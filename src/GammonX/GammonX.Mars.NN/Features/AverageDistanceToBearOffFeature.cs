using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Calculates the average distance to bearoff position on the board for a given player.
    /// Helps to distinguish between a good formation and a single front runner.
    /// </summary>
    public class AverageDistanceToBearOffFeature : IFeature<double>
    {
        // <inheritdoc />
        public double Eval(IBoardModel board, bool isWhite)
        {
            var totalCheckers = 0;
            var totalDistance = 0.0;
            var homeRangeEnd = isWhite
                ? board.HomeRangeWhite.End.Value
                : board.HomeRangeBlack.End.Value;

            for (var index = 0; index < board.Fields.Length; index++)
            {
                var value = board.Fields[index];
                if ((isWhite && value < 0) || (!isWhite && value > 0))
                {
                    var count = Math.Abs(value);
                    totalCheckers += count;
                    totalDistance += count * board.RecoverRollOperator(isWhite, index, homeRangeEnd);
                }
            }

            if (board is IHomeBarModel homeBarModel)
            {
                var homeBarCount = isWhite
                    ? homeBarModel.HomeBarCountWhite
                    : homeBarModel.HomeBarCountBlack;
                if (homeBarCount > 0)
                {
                    totalCheckers += homeBarCount;
                    var startIndex = isWhite
                        ? board.StartRangeWhite.Start.Value
                        : board.StartRangeBlack.Start.Value;
                    totalDistance += homeBarCount * board.RecoverRollOperator(isWhite, startIndex, homeRangeEnd);
                }
            }

            return totalCheckers > 0 ? totalDistance / totalCheckers : 0.0;
        }
    }
}
