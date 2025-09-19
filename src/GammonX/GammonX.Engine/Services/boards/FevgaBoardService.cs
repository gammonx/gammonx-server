using GammonX.Engine.Models;

namespace GammonX.Engine.Services
{
    // <inheritdoc />
    internal class FevgaBoardService : BoardBaseServiceImpl
    {
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
					// we have a special case for black fevga checkers.
					// their last home field is index 11
					// their start field index is 11 aswell
					// black checkers must not move past the start field
					if (!isWhite)
					{
						var homeEndField = model.HomeRangeBlack.End.Value;
						// checkers must not pass their original start field
						if (from <= homeEndField && from + roll > homeEndField)
						{
							return false;
						}
					}

					if (CreatesAnInvalidStartFieldPrime(model, from, roll, isWhite))
						return false;

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
				var furthestIndex = fieldList.FindIndex(_blackStartCheckerIndex, (i) => i == -1);
				if (furthestIndex != -1)
				{
					return true;
				}                
            }
            else if (!isWhite)
            {
				var furthestIndex = fieldList.FindIndex(_whiteStartCheckerIndex, (i) => i == 1);
				if (furthestIndex != -1 && furthestIndex < _blackStartCheckerIndex)
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

		private static bool CreatesAnInvalidStartFieldPrime(IBoardModel model, int from, int roll, bool isWhite)
		{
			var to = model.MoveOperator(isWhite, from, roll);
			// check if this move would create a 6er prime on the opponents starting field
			if (isWhite)
			{
				if (IsIndexInRange(to, model.StartRangeBlack, model.Fields.Length))
				{
					var (offset, length) = model.StartRangeBlack.GetOffsetAndLength(model.Fields.Length);
					var primeCounter = 0;
					for (int i = offset; i <= offset + length; i++)
					{
						if (model.Fields[i] < 0)
							primeCounter++;
						else if (i == to)
							primeCounter++;
						else
							break;
					}
					if (primeCounter == 6)
						return true;
				}
			}
			else
			{
				if (IsIndexInRange(to, model.StartRangeWhite, model.Fields.Length))
				{
					var (offset, length) = model.StartRangeWhite.GetOffsetAndLength(model.Fields.Length);
					var primeCounter = 0;
					for (int i = offset; i <= offset + length; i++)
					{
						if (model.Fields[i] > 0)
							primeCounter++;
						else if (i == to)
							primeCounter++;
						else
							break;
					}
					if (primeCounter == 6)
						return true;
				}
			}
			return false;
		}

		private static bool IsIndexInRange(int index, Range range, int arrayLength)
		{
			var (start, length) = range.GetOffsetAndLength(arrayLength);
			int end = start + length + 1; // end inklusiv
			return index >= start && index < end;
		}
	}
}