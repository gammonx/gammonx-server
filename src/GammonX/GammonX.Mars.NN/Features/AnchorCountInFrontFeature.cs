using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Counts the amount of anchors (blocked points) in front of the opponent.
    /// Fevga is a forward movement game. Blocking forward movement is a core strategy.
    /// </summary>
    public class AnchorCountInFrontFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            if (isWhite)
            {
                var whiteAnchors = board.Fields.Index().Where(i => i.Item <= -board.BlockAmount).ToList();
                if (board is IPinModel pinModel)
                {
                    var potWhiteAnchors = board.Fields.Index().Where(i => i.Item == -(board.BlockAmount - 1));
                    potWhiteAnchors = potWhiteAnchors.Where(i => pinModel.PinnedFields[i.Index] == board.BlockAmount - 1);
                    whiteAnchors = whiteAnchors.Concat(potWhiteAnchors).ToList();
                }

                var blackIndices = board.Fields.Index().Where(i => i.Item > 0);
                var nearStartRangeIndex = board.StartRangeBlack.Start.Value;
                if (board is IHomeBarModel homeBarModel && homeBarModel.HomeBarCountBlack == 0 && blackIndices.Any())
                {
                    nearStartRangeIndex = blackIndices.MaxBy(
                           bi => board.RecoverRollOperator(!isWhite, bi.Index, board.HomeRangeBlack.End.Value)).Index;
                }
                
                // we count all white anchors in front of the black checker which is the furthest away from home end
                return whiteAnchors.Count(wa => 
                    board.RecoverRollOperator(!isWhite, wa.Index, board.HomeRangeBlack.End.Value) <
                    board.RecoverRollOperator(!isWhite, nearStartRangeIndex, board.HomeRangeBlack.End.Value));
            }
            else
            {
                var blackAnchors = board.Fields.Index().Where(i => i.Item >= board.BlockAmount).ToList();
                if (board is IPinModel pinModel)
                {
                    var potBlackAnchors = board.Fields.Index().Where(i => i.Item == board.BlockAmount - 1);
                    potBlackAnchors = potBlackAnchors.Where(i => pinModel.PinnedFields[i.Index] == -(board.BlockAmount - 1));
                    blackAnchors = blackAnchors.Concat(potBlackAnchors).ToList();
                }

                var whiteIndices = board.Fields.Index().Where(i => i.Item < 0);
                var nearStartRangeIndex = board.StartRangeWhite.Start.Value;
                if (board is IHomeBarModel homeBarModel && homeBarModel.HomeBarCountWhite == 0 && whiteIndices.Any())
                {
                    nearStartRangeIndex = whiteIndices.MaxBy(
                           bi => board.RecoverRollOperator(!isWhite, bi.Index, board.HomeRangeWhite.End.Value)).Index;
                }

                // we count all black anchors in front of the white checker which is the furthest away from home end
                return blackAnchors.Count(wa =>
                    board.RecoverRollOperator(!isWhite, wa.Index, board.HomeRangeWhite.End.Value) <
                    board.RecoverRollOperator(!isWhite, nearStartRangeIndex, board.HomeRangeWhite.End.Value));
            }
        }
    }
}
