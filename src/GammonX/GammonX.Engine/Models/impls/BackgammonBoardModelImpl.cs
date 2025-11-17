using GammonX.Engine.Services;

namespace GammonX.Engine.Models
{
    /// <summary>
    /// Classic BackGammon implementation.
    /// <seealso cref="https://www.bkgm.com/rules.html"/>
    /// </summary>
    internal sealed class BackgammonBoardModelImpl : BoardBaseImpl, IHomeBarModel, IDoublingCubeModel, IHitModel
    {
        public BackgammonBoardModelImpl()
        {
            Fields = new int[24]
            {
                -2, // Field 1 :: Black Home :: 2 White pieces
                0,  // Field 2
                0,  // Field 3
                0,  // Field 4
                0,  // Field 5
                5,  // Field 6 :: 5 Black pieces
                0,  // Field 7
                3,  // Field 8 :: 3 Black pieces
                0,  // Field 9
                0,  // Field 10
                0,  // Field 11
                -5, // Field 12 :: 5 White pieces
                5,  // Field 13 :: 5 Black pieces
                0,  // Field 14
                0,  // Field 15
                0,  // Field 16
                -3, // Field 17 :: 3 White pieces
                0,  // Field 18
                -5, // Field 19 :: 5 White pieces
                0,  // Field 20
                0,  // Field 21
                0,  // Field 22
                0,  // Field 23 
                2,  // Field 24 :: White Home :: 2 Black pieces
            };
        }

        // <inheritdoc />
        public override GameModus Modus => GameModus.Backgammon;

		// <inheritdoc />
		public override int[] Fields { get; protected set; }

		// <inheritdoc />
		public override Range HomeRangeWhite => new(18, 23);

		// <inheritdoc />
		public override Range HomeRangeBlack => new(5, 0);

		// <inheritdoc />
		public override int BlockAmount => 2;

        // <inheritdoc />
        public int HomeBarCountWhite { get; private set; } = 0;

        // <inheritdoc />
        public int HomeBarCountBlack { get; private set; } = 0;

        // <inheritdoc />
        public int StartIndexWhite => WellKnownBoardPositions.HomeBarWhite;

        // <inheritdoc />
        public int StartIndexBlack => WellKnownBoardPositions.HomeBarBlack;

		// <inheritdoc />
		public bool MustEnterFromHomebar => true;

		// <inheritdoc />
		public bool CanSendToHomeBar => true;

		// <inheritdoc />
		public int DoublingCubeValue { get; private set; } = 1;

        // <inheritdoc />
        public bool DoublingCubeOwner { get; set; } = false;

		// <inheritdoc />
		public void AcceptDoublingCubeOffer(bool isWhite)
		{
            var owner = isWhite ? DoublingCubeOwner : !DoublingCubeOwner;
			if (owner && DoublingCubeValue > 1)
            {
                throw new InvalidOperationException("Doubling offer can only be accepted by a non owner of the doubling cube");
            }
            else
            {
                if (DoublingCubeValue < 64)
                {
                    DoublingCubeValue *= 2;
                    DoublingCubeOwner = !DoublingCubeOwner;
                }
                else
                {
					throw new InvalidOperationException("The max doubling cube value of 64 is already reached");
				}
            }
		}

		// <inheritdoc />
		public bool CanOfferDoublingCube(bool isWhite)
        {
			var owner = isWhite ? DoublingCubeOwner : !DoublingCubeOwner;
			return (DoublingCubeValue < 64 && owner) || DoublingCubeValue == 1;
        }

		// <inheritdoc />
		public void AddToHomeBar(bool isWhite, int amount)
        {
            if (isWhite)
            {
                HomeBarCountWhite += amount;
            }
            else
            {
                HomeBarCountBlack += amount;
            }
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

		// <inheritdoc />
		public override IBoardModel InvertBoard()
		{
            var invertedFields = BoardBroker.InvertBoardFields(Fields);
			return new BackgammonBoardModelImpl()
            {
				// assign white values to black
				BearOffCountBlack = BearOffCountWhite,
                HomeBarCountBlack = HomeBarCountWhite,
				// assign black values to white
				BearOffCountWhite = BearOffCountBlack,
                HomeBarCountWhite = HomeBarCountBlack,
                // invert doubling cube
                DoublingCubeOwner = !DoublingCubeOwner,
                DoublingCubeValue = DoublingCubeValue,
                // inverted board fieds
                Fields = invertedFields,
			};
		}

		// <inheritdoc />
		public override object Clone()
		{
			return new BackgammonBoardModelImpl()
			{
				BearOffCountWhite = BearOffCountWhite,
				BearOffCountBlack = BearOffCountBlack,
				HomeBarCountWhite = HomeBarCountWhite,
				HomeBarCountBlack = HomeBarCountBlack,
                DoublingCubeOwner = DoublingCubeOwner,
                DoublingCubeValue = DoublingCubeValue,
				// clone is okay for primitive types
				Fields = (int[])Fields.Clone(),
			};
		}
	}
}
