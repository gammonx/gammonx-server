using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Calculates the geometric pips until all remaining checkers have entered home,
    /// including pinned checkers and checkers on bar/reserve.
    /// </summary>
    public sealed class PipsToBearOffFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            var home = isWhite ? board.HomeRangeWhite : board.HomeRangeBlack;
            var homePointCount = Math.Abs(home.End.Value - home.Start.Value) + 1;
            var pips = FeatureBoardGeometry.BarCount(board, isWhite) * (board.Fields.Length + 1 - homePointCount);

            for (var index = 0; index < board.Fields.Length; index++)
            {
                var count = FeatureBoardGeometry.RemainingCountAt(board, isWhite, index);
                if (count > 0)
                {
                    var pipsToBearOff = FeatureBoardGeometry.BearOffDistance(board, isWhite, index);
                    pips += count * Math.Max(pipsToBearOff - homePointCount, 0);
                }
            }

            return pips;
        }
    }
}
