using GammonX.Engine.Models;
using System.Reflection;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class FevgaBoardService : BoardBaseServiceImpl
    {
        private bool _whiteHasPassedBlackStart = false;
        private bool _blackHasPassedWhiteStart = false;
        
        private readonly int _whiteStartIndex = 0;
        private readonly int _blackStartIndex = 12;

        // <inheritdoc />
        public override GameModus Modus => GameModus.Fevga;

        // <inheritdoc />
        public override IBoardModel CreateBoard()
        {
            return new FevgaBoardModelImpl();
        }

        // <inheritdoc />
        public override bool CanMoveChecker(IBoardModel model, int from, int roll, bool isWhite)
        {
            // in Fevga, the first checker needs to pass the start index of the opponent
            // until then no other checker may be moved.
            if (HasPassedOpponentsStart(model, isWhite))
            {
                return base.CanMoveChecker(model, from, roll, isWhite);
            }
            else
            {
                int furthest = GetFurthestCheckerIndex(model, isWhite);
                if (from == furthest)
                    return base.CanMoveChecker(model, from, roll, isWhite);
            }            

            return false;
        }

        private bool HasPassedOpponentsStart(IBoardModel model, bool isWhite)
        {
            var fieldList = model.Fields.ToList();
            if (isWhite)
            {
                if (!_whiteHasPassedBlackStart)
                {
                    var furthestIndex = fieldList.FindIndex(_blackStartIndex, (i) => i == -1);
                    if (furthestIndex != -1)
                    {
                        _whiteHasPassedBlackStart = true;
                        return true;
                    }
                }
                else
                {
                    return true;
                }
                
            }
            else if (!isWhite)
            {
                if (!_blackHasPassedWhiteStart)
                {
                    var furthestIndex = fieldList.FindIndex(_whiteStartIndex, (i) => i == 1);
                    if (furthestIndex != -1 && furthestIndex < _blackStartIndex)
                    {
                        _blackHasPassedWhiteStart = true;
                        return true;
                    }
                }
                else
                {
                    return true;
                }
               
            }
            return false;
        }

        private int GetFurthestCheckerIndex(IBoardModel model, bool isWhite)
        {
            var fieldList = model.Fields.ToList();
            if (isWhite)
            {
                var furthestWhiteIndex = fieldList.FindIndex(_whiteStartIndex, (i) => i == -1 || i == -15);
                return furthestWhiteIndex;
            }
            else
            {
                var furthestBlackIndex = fieldList.FindIndex(_blackStartIndex, (i) => i == 1 || i == 15);
                return furthestBlackIndex;
            }
        }
    }
}