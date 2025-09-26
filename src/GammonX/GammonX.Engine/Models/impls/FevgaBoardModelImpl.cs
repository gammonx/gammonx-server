namespace GammonX.Engine.Models
{
	/// <summary>
	/// Fevga implementation.
	/// <seealso cref="https://www.bkgm.com/variants/Fevga.html"/>
	/// <seealso cref="https://www.bkgm.com/variants/Tavli.html"/>
	/// </summary>
	internal sealed class FevgaBoardModelImpl : BoardBaseImpl, IHomeBarModel, IFevgaBoardModel
    {
        public FevgaBoardModelImpl()
        {
            Fields = new int[24]
            {
                -1, // Field 1  :: 1 White Checkers
                0,  // Field 2
                0,  // Field 3
                0,  // Field 4
                0,  // Field 5
                0,  // Field 6
                0,  // Field 7  :: Black Home
                0,  // Field 8  :: Black Home
                0,  // Field 9  :: Black Home
                0,  // Field 10 :: Black Home
                0,  // Field 11 :: Black Home
                0,  // Field 12 :: Black Home
                1,  // Field 13 :: 1 Black Checkers
                0,  // Field 14
                0,  // Field 15
                0,  // Field 16
                0,  // Field 17
                0,  // Field 18
                0,  // Field 19 :: White Home
                0,  // Field 20 :: White Home
                0,  // Field 21 :: White Home
                0,  // Field 22 :: White Home
                0,  // Field 23 :: White Home 
                0,  // Field 24 :: White Home
            };
        }

        // <inheritdoc />
        public override GameModus Modus => GameModus.Fevga;

		// <inheritdoc />
		public override int[] Fields { get; protected set; }

		// <inheritdoc />
		public override Range HomeRangeWhite => new(18, 23);

		// <inheritdoc />
		public override Range HomeRangeBlack => new(6, 11);

		// <inheritdoc />
		public override Range StartRangeBlack => new(12, 17);

		// <inheritdoc />
		public override int BlockAmount => 1;

        // <inheritdoc />
        public override Func<bool, int, int, int> MoveOperator => new((isWhite, currentPosition, moveDistance) =>
        {
            if (isWhite)
            {
                // White moves from 0 to 23
                int newPosition = currentPosition + moveDistance;
                return newPosition;
            }
            else
            {
				// we redirect checkers movements from the homebar (index 24) to the actual start position
				if (currentPosition == StartIndexBlack)
				{
					currentPosition = 11;
				}

                // Black moves forward (wraps from 23 -> 0)
                int newPosition = (currentPosition + moveDistance) % 24;
                return newPosition;
            }
        });

		// <inheritdoc />
		public override Func<bool, int, int, bool> CanBearOffOperator => new((isWhite, currentPosition, moveDistance) =>
		{
			if (isWhite)
			{
				int to = MoveOperator(isWhite, currentPosition, moveDistance);
				// checkers with the perfect bear off roll can always be taken out
				if (to == HomeRangeWhite.End.Value + 1)
				{
					return true;
				}
				// checkers with a higher roll than their bear off value can only be taken off
				// if there does not exist a checker with a higher index/distance.
				else if (to > HomeRangeWhite.End.Value)
				{
					// check if there are any checkers in the home range with above the current position
					bool highestCheckerIndex = !Fields
						.Skip(HomeRangeWhite.Start.Value)
						.Take(currentPosition - HomeRangeWhite.Start.Value)
						.Any(v => v < 0);
					return highestCheckerIndex;
				}
				return false;
			}
			else
			{
				int to = MoveOperator(isWhite, currentPosition, moveDistance);
				// checkers with the perfect bear off roll can always be taken out
				if (to == HomeRangeBlack.End.Value + 1)
				{
					return true;
				}
				// checkers with a higher roll than their bear off value can only be taken off
				// if there does not exist a checker with a lower index/distance.
				else if (to > HomeRangeBlack.End.Value)
				{
					// check if there are any checkers in the home range with above the current position
					bool highestCheckerIndex = !Fields
						.Skip(HomeRangeBlack.Start.Value)
						.Take(currentPosition - HomeRangeBlack.Start.Value)
						.Any(v => v > 0);
					return highestCheckerIndex;
				}
				return false;
			}
		});

		// <inheritdoc />
		public override Func<bool, int, bool> IsInHomeOperator => new((isWhite, position) =>
		{
			if (isWhite && (position < HomeRangeWhite.Start.Value || position > HomeRangeWhite.End.Value)) return false;
			if (!isWhite && (position < HomeRangeBlack.Start.Value || position > HomeRangeBlack.End.Value)) return false;
			return true;
		});

		// <inheritdoc />
		public int HomeBarCountWhite { get; private set; } = 14;

		// <inheritdoc />
		public int HomeBarCountBlack { get; private set; } = 14;

		// <inheritdoc />
		public int StartIndexWhite => -1;

		// <inheritdoc />
		public int StartIndexBlack => 24;

		// <inheritdoc />
		public bool MustEnterFromHomebar => false;

		// <inheritdoc />
		public bool CanSendToHomeBar => false;

		// <inheritdoc />
		public override IBoardModel InvertBoard()
		{
			var invertedFields = InvertFevgaBoardHorizontally(Fields);
			return new FevgaBoardModelImpl()
			{
				// assign white values to black
				BearOffCountBlack = BearOffCountWhite,
				// assign black values to white
				BearOffCountWhite = BearOffCountBlack,
				// inverted board fieds
				Fields = invertedFields,
			};
		}

		// <inheritdoc />
		public IBoardModel InvertBoardVertically()
		{
			var invertedFields = InvertFevgaBoardVertically(Fields);
			return new FevgaBoardModelImpl()
			{
				// inverted board fieds
				Fields = invertedFields,
			};
		}

		// <inheritdoc />
		public bool HasPassedOpponentStart(bool isWhite)
		{
			throw new NotImplementedException();
		}

		// <inheritdoc />
		public override object Clone()
		{
			return new FevgaBoardModelImpl()
			{
				BearOffCountWhite = BearOffCountWhite,
				BearOffCountBlack = BearOffCountBlack,
				HomeBarCountBlack = HomeBarCountBlack,
				HomeBarCountWhite = HomeBarCountWhite,
				// clone is okay for primitive types
				Fields = (int[])Fields.Clone(),
			};
		}

		// <inheritdoc />
		public void AddToHomeBar(bool isWhite, int amount)
		{
			throw new InvalidOperationException("Fevga does not support adding checkers to the home bar.");
		}

		// <inheritdoc />
		public void RemoveFromHomeBar(bool isWhite, int amount)
		{
			if (isWhite)
			{
				HomeBarCountWhite -= amount;
			}
			else
			{
				HomeBarCountBlack -= amount;
			}
		}

		private static int[] InvertFevgaBoardHorizontally(int[] originalFields)
		{
			int[] invertedFields = new int[24];
			for (int i = 0; i < 24; i++)
			{
				// first half (0–11) mirrors to (12–23)
				// second half (12–23) mirrors to (0–11)
				int mirroredIndex = (i < 12) ? i + 12 : i - 12;
				invertedFields[mirroredIndex] = -originalFields[i];
			}

			return invertedFields;
		}

		private static int[] InvertFevgaBoardVertically(int[] originalFields)
		{
			int[] invertedFields = new int[24];
			for (int i = 0; i < 12; i++)
			{
				// inverts upper half index 0 to 11
				int mirroredIndex = 11 - i;
				invertedFields[mirroredIndex] = originalFields[i];
			}

			for (int i = 12; i < 24; i++)
			{
				// inverts lower half index 12 to 23
				int mirroredIndex = 35 - i;
				invertedFields[mirroredIndex] = originalFields[i];
			}

			return invertedFields;
		}
	}
}
