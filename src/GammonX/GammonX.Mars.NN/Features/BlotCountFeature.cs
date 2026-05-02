using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Calculates how many blots the player has on the board.
    /// </summary>
    public class BlotCountFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            if (isWhite)
            {
                var whiteBlots = board.Fields.Index().Where(i => i.Item == -1);
                if (board is IPinModel pinModel)
                {
                    whiteBlots = whiteBlots.Where(wb => pinModel.PinnedFields[wb.Index] == 0);
                }
                return whiteBlots.Count();
            }
            else
            {
                var blackBlots = board.Fields.Index().Where(i => i.Item == 1);
                if (board is IPinModel pinModel)
                {
                    blackBlots = blackBlots.Where(wb => pinModel.PinnedFields[wb.Index] == 0);
                }
                return blackBlots.Count();
            }
        }
    }
}
