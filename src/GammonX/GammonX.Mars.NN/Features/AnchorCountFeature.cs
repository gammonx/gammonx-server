using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    public class AnchorCountFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            if (isWhite)
            {
                var whiteAnchors = board.Fields.Index().Where(i => i.Item <= -board.BlockAmount);
                if (board is IPinModel pinModel)
                {
                    var potWhiteAnchors = board.Fields.Index().Where(i => i.Item == -(board.BlockAmount -1));
                    potWhiteAnchors = potWhiteAnchors.Where(i => pinModel.PinnedFields[i.Index] == board.BlockAmount - 1);
                    whiteAnchors = whiteAnchors.Concat(potWhiteAnchors);
                }
                return whiteAnchors.Count();
            }
            else
            {
                var blackAnchors = board.Fields.Index().Where(i => i.Item > board.BlockAmount);
                if (board is IPinModel pinModel)
                {
                    var potBlackAnchors = board.Fields.Index().Where(i => i.Item == board.BlockAmount - 1);
                    potBlackAnchors = potBlackAnchors.Where(i => pinModel.PinnedFields[i.Index] == -(board.BlockAmount - 1));
                    blackAnchors = blackAnchors.Concat(potBlackAnchors);
                }
                return blackAnchors.Count();
            }
        }
    }
}
