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
                var whitePositions = board.Fields.Index().Where(i => i.Item < 0).ToList();
                if (board is IHomeBarModel homeBarModel && homeBarModel.HomeBarCountWhite > 0)
                {
                    whitePositions.Add((board.StartRangeWhite.Start.Value, -homeBarModel.HomeBarCountWhite));
                }

                if (whitePositions.Count != 0)
                {
                    var avgIndex = whitePositions.Average(wp => wp.Index);
                    return board.RecoverRollOperator(isWhite, (int)avgIndex, board.HomeRangeWhite.End.Value);
                }
                return 0.0;
            }
            else
            {
                var blackPositions = board.Fields.Index().Where(i => i.Item > 0).ToList();
                if (board is IHomeBarModel homeBarModel && homeBarModel.HomeBarCountBlack > 0)
                {
                    blackPositions.Add((board.StartRangeBlack.Start.Value, homeBarModel.HomeBarCountBlack));
                }

                if (blackPositions.Count != 0)
                {
                    var avgIndex = blackPositions.Average(wp => wp.Index);
                    return board.RecoverRollOperator(isWhite, (int)avgIndex, board.HomeRangeBlack.End.Value);
                }
                return 0.0;
            }
        }
    }
}
