using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class FevgaBoardService : BoardBaseServiceImpl
    {
        private bool _whiteHasPassedBlackStart = false;
        private bool _blackHasPassedWhiteStart = false;
        
        // black must pass this in order to move others
        private readonly int _whiteStartCheckerIndex = 0;
        // white must pass this in order to move others
        private readonly int _blackStartCheckerIndex = 12;

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
			if (model is IHomeBarModel homeBarModel)
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
					else if (furthest == -1)
                    {
                        var startIndex = isWhite ? homeBarModel.StartIndexWhite : homeBarModel.StartIndexBlack;
                        if (startIndex == from)
							return base.CanMoveChecker(model, startIndex, roll, isWhite);
					}
				}

				return false;
			}

			throw new InvalidOperationException("FevgaBoardService requires a board model that implements IHomeBarModel.");
		}

		// <inheritdoc />
		protected override IEnumerable<int> GetMoveableCheckerFields(IBoardModel model, bool isWhite)
		{
			if (model is IHomeBarModel homeBarModel)
            {
				if (HasPassedOpponentsStart(model, isWhite))
				{
					return base.GetMoveableCheckerFields(model, isWhite);
				}
				else
				{
					var furthest = GetFurthestCheckerIndex(model, isWhite);
					var startIndex = isWhite ? homeBarModel.StartIndexWhite : homeBarModel.StartIndexBlack;
					if (furthest == -1)
					{
						return [startIndex];
					}
                    return [furthest];
				}
			}

            throw new InvalidOperationException("FevgaBoardService requires a board model that implements IHomeBarModel.");
		}

        private bool HasPassedOpponentsStart(IBoardModel model, bool isWhite)
        {
            var fieldList = model.Fields.ToList();
            if (isWhite)
            {
                if (!_whiteHasPassedBlackStart)
                {
                    var furthestIndex = fieldList.FindIndex(_blackStartCheckerIndex, (i) => i == -1);
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
                    var furthestIndex = fieldList.FindIndex(_whiteStartCheckerIndex, (i) => i == 1);
                    if (furthestIndex != -1 && furthestIndex < _blackStartCheckerIndex)
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
            // returns -1 if none checkers have been moved

            var fieldList = model.Fields.ToList();
            if (isWhite)
            {
                // if white rolled an one at start
                var furthestWhiteIndex = fieldList.FindIndex(_whiteStartCheckerIndex, (i) => i == -2);
				if (furthestWhiteIndex == -1)
                {
					furthestWhiteIndex = fieldList.FindIndex(_whiteStartCheckerIndex + 1, (i) => i == -1);
				}
                return furthestWhiteIndex;				
			}
            else
            {
                // if black rolled an one at start
                var furthestBlackIndex = fieldList.FindIndex(_blackStartCheckerIndex, (i) => i == 2);
                if (furthestBlackIndex == -1)
                {
					furthestBlackIndex = fieldList.FindIndex(_blackStartCheckerIndex + 1, (i) => i == 1);
				}
                return furthestBlackIndex;
            }
        }
    }
}