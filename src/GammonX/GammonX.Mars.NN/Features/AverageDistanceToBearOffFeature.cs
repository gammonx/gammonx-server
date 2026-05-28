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
            if (isWhite)
            {
                var totalCheckers = 0;
                var totalDistance = 0.0;

                foreach (var (index, value) in board.Fields.Index().Where(i => i.Item < 0))
                {
                    var count = Math.Abs(value);
                    totalCheckers += count;
                    totalDistance += count * board.RecoverRollOperator(isWhite, index, board.HomeRangeWhite.End.Value);
                }

                if (board is IHomeBarModel homeBarModelWhite && homeBarModelWhite.HomeBarCountWhite > 0)
                {
                    var count = homeBarModelWhite.HomeBarCountWhite;
                    totalCheckers += count;
                    totalDistance += count * board.RecoverRollOperator(isWhite, board.StartRangeWhite.Start.Value, board.HomeRangeWhite.End.Value);
                }

                return totalCheckers > 0 ? totalDistance / totalCheckers : 0.0;
            }
            else
            {
                var totalCheckers = 0;
                var totalDistance = 0.0;

                foreach (var (index, value) in board.Fields.Index().Where(i => i.Item > 0))
                {
                    totalCheckers += value;
                    totalDistance += value * board.RecoverRollOperator(isWhite, index, board.HomeRangeBlack.End.Value);
                }

                if (board is IHomeBarModel homeBarModelBlack && homeBarModelBlack.HomeBarCountBlack > 0)
                {
                    var count = homeBarModelBlack.HomeBarCountBlack;
                    totalCheckers += count;
                    totalDistance += count * board.RecoverRollOperator(isWhite, board.StartRangeBlack.Start.Value, board.HomeRangeBlack.End.Value);
                }

                return totalCheckers > 0 ? totalDistance / totalCheckers : 0.0;
            }
        }
    }
}
