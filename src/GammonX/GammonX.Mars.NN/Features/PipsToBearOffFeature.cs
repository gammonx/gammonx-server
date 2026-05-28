using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Calculates the amount of pipes needed for the given player until all of his checkers
    /// are in the home range and can be borne off.
    /// </summary>
    public sealed class PipsToBearOffFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            // iterate from just outside home (home start + 1) up to start range end
            // and count pips each checker needs to travel to reach the home entry point
            if (isWhite)
            {
                var pipsToBearOff = 0;
                for (int i = board.HomeRangeWhite.Start.Value - 1; i >= board.StartRangeWhite.Start.Value; i--)
                {
                    if (board.Fields[i] < 0)
                    {
                        pipsToBearOff += board.RecoverRollOperator(isWhite, i, board.HomeRangeWhite.Start.Value) * Math.Abs(board.Fields[i]);
                    }
                }
                return pipsToBearOff;
            }
            else
            {
                var pipsToBearOff = 0;
                for (int i = board.HomeRangeBlack.Start.Value + 1; i <= board.StartRangeBlack.Start.Value; i++)
                {
                    if (board.Fields[i] > 0)
                    {
                        pipsToBearOff += board.RecoverRollOperator(isWhite, i, board.HomeRangeBlack.Start.Value) * Math.Abs(board.Fields[i]);
                    }
                }
                return pipsToBearOff;
            }
        }
    }
}
