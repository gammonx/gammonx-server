using GammonX.Engine.Models;

namespace GammonX.Mars.Server.Features
{
    /// <summary>
    /// Calculates the pip difference between the player and his opponent.
    /// Returns a positive value if the player is ahead and a negative value if the player is behind.
    /// </summary>
    public sealed class PipDifferenceFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            if (isWhite)
            {
                return board.PipCountBlack - board.PipCountWhite;
            }
            else
            {
                return board.PipCountWhite - board.PipCountBlack;
            }
        }
    }
}
