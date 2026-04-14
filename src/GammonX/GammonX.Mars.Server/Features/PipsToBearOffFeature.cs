using GammonX.Engine.Models;

namespace GammonX.Mars.Server.Features
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
            IBoardModel playersBoard = GetBoard(board, isWhite);
            // iterate from just outside black home (home start + 1) up to black's farthest position
            // and accumulate pips each checker needs to travel to reach the home entry point
            var pipsToBearOff = 0;
            for (int i = playersBoard.HomeRangeBlack.Start.Value + 1; i <= playersBoard.StartRangeBlack.Start.Value; i++)
            {
                if (playersBoard.Fields[i] > 0)
                {
                    pipsToBearOff += playersBoard.Fields[i] * (i - playersBoard.HomeRangeBlack.Start.Value);
                }
            }
            return pipsToBearOff;
        }

        private static IBoardModel GetBoard(IBoardModel board, bool isWhite)
        {
            if (isWhite)
            {
                return board.InvertBoard();
            }

            return board;
        }
    }
}
