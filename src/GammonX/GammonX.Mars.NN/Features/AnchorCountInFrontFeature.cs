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
                var nearStartRangeIndex = board.StartRangeBlack.Start.Value;
                var nearStartPosition = board.RecoverRollOperator(false, nearStartRangeIndex, board.HomeRangeBlack.End.Value);
                if (board is IHomeBarModel homeBarModel && homeBarModel.HomeBarCountBlack == 0)
                {
                    var hasBlackChecker = false;
                    for (var index = 0; index < board.Fields.Length; index++)
                    {
                        if (board.Fields[index] > 0)
                        {
                            var movementPosition = board.RecoverRollOperator(false, index, board.HomeRangeBlack.End.Value);
                            if (!hasBlackChecker || movementPosition > nearStartPosition)
                            {
                                nearStartPosition = movementPosition;
                            }

                            hasBlackChecker = true;
                        }
                    }
                }

                var anchorCount = 0;
                var pinModel = board as IPinModel;
                for (var index = 0; index < board.Fields.Length; index++)
                {
                    var field = board.Fields[index];
                    var isAnchor = field <= -board.BlockAmount;
                    if (!isAnchor && pinModel is not null)
                    {
                        isAnchor = field == -(board.BlockAmount - 1)
                            && pinModel.PinnedFields[index] == board.BlockAmount - 1;
                    }

                    if (isAnchor && board.RecoverRollOperator(false, index, board.HomeRangeBlack.End.Value) < nearStartPosition)
                    {
                        anchorCount++;
                    }
                }

                return anchorCount;
            }
            else
            {
                var nearStartRangeIndex = board.StartRangeWhite.Start.Value;
                var nearStartPosition = board.RecoverRollOperator(true, nearStartRangeIndex, board.HomeRangeWhite.End.Value);
                if (board is IHomeBarModel homeBarModel && homeBarModel.HomeBarCountWhite == 0)
                {
                    var hasWhiteChecker = false;
                    for (var index = 0; index < board.Fields.Length; index++)
                    {
                        if (board.Fields[index] < 0)
                        {
                            var movementPosition = board.RecoverRollOperator(true, index, board.HomeRangeWhite.End.Value);
                            if (!hasWhiteChecker || movementPosition > nearStartPosition)
                            {
                                nearStartPosition = movementPosition;
                            }

                            hasWhiteChecker = true;
                        }
                    }
                }

                var anchorCount = 0;
                var pinModel = board as IPinModel;
                for (var index = 0; index < board.Fields.Length; index++)
                {
                    var field = board.Fields[index];
                    var isAnchor = field >= board.BlockAmount;
                    if (!isAnchor && pinModel is not null)
                    {
                        isAnchor = field == board.BlockAmount - 1
                            && pinModel.PinnedFields[index] == -(board.BlockAmount - 1);
                    }

                    if (isAnchor && board.RecoverRollOperator(true, index, board.HomeRangeWhite.End.Value) < nearStartPosition)
                    {
                        anchorCount++;
                    }
                }

                return anchorCount;
            }
        }
    }
}
