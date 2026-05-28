using GammonX.Engine.Models;

namespace GammonX.Mars.NN.Features
{
    /// <summary>
    /// Finds the pinned opponent checker in the players home board that is nearest to the home range end (bear-off side)
    /// and counts all of the player's own checkers that still need to pass through that pin (i.e. could stack on it).
    /// This is an important feature for the plakoto variant, as stacking more checkers on a pin strengthens the
    /// player's position and increases the chance to bear off checkers and win with a gammon (2pts).
    /// </summary>
    public sealed class NumbersOfCheckersInFronOfLastPinFeature : IFeature<int>
    {
        // <inheritdoc />
        public int Eval(IBoardModel board, bool isWhite)
        {
            if (board is not IPinModel pinModel)
            {
                return 0;
            }


            if (isWhite)
            {
                // find pinned black checkers nearest to the white home range end
                var pinIndexTuple = pinModel.PinnedFields.Index().Where(i => i.Item != 0);
                var pinBlackIndicesInWhiteHome = pinIndexTuple.Where(pit => pit.Item > 0 && board.IsInHomeOperator(isWhite, pit.Index));
                if (pinBlackIndicesInWhiteHome.Any())
                {
                    var nearestBlackPin = pinBlackIndicesInWhiteHome.OrderByDescending(
                        pit => board.RecoverRollOperator(isWhite, pit.Index, board.HomeRangeBlack.End.Value)).First();

                    // we count all white checkers which could stack on the pin
                    var fieldIndexTuple = board.Fields.Index().Where(i => i.Item < 0);
                    var whiteCheckersInFront = fieldIndexTuple.Where(pit => pit.Index < nearestBlackPin.Index).Select(pit => pit.Item);
                    return Math.Abs(whiteCheckersInFront.Sum());
                }
            }
            else
            {
                // find pinned white checkers nearest to the black home range end
                var pinIndexTuple = pinModel.PinnedFields.Index().Where(i => i.Item != 0);
                var pinWhiteIndicesInWhiteHome = pinIndexTuple.Where(pit => pit.Item < 0 && board.IsInHomeOperator(isWhite, pit.Index));
                if (pinWhiteIndicesInWhiteHome.Any())
                {
                    var nearestWhitePin = pinWhiteIndicesInWhiteHome.OrderByDescending(
                        pit => board.RecoverRollOperator(isWhite, pit.Index, board.HomeRangeWhite.End.Value)).First();

                    // we count all black checkers which could stack on the pin
                    var fieldIndexTuple = board.Fields.Index().Where(i => i.Item > 0);
                    var blackCheckersInFront = fieldIndexTuple.Where(pit => pit.Index > nearestWhitePin.Index).Select(pit => pit.Item);
                    return Math.Abs(blackCheckersInFront.Sum());
                }
            }
            return 0;
        }
    }
}
