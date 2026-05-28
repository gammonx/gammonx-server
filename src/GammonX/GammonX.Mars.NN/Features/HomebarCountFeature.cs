using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Calculates the number of checkers on the homebar for the given player.
    /// </summary>
    public class HomebarCountFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            if (board is IHomeBarModel homebar)
            {
                if (isWhite)
                {
                    return homebar.HomeBarCountWhite;
                }
                else
                {
                    return homebar.HomeBarCountBlack;
                }
            }
            return 0;
        }
    }
}
