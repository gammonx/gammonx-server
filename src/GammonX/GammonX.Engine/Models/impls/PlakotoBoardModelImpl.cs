using GammonX.Engine.Services;

namespace GammonX.Engine.Models
{
	/// <summary>
	/// Portes implementation
	/// <seealso cref="https://www.bkgm.com/variants/Plakoto.html"/>
	/// <seealso cref="https://www.bkgm.com/variants/Tavli.html"/>
	/// </summary>
	internal sealed class PlakotoBoardModelImpl : BoardBaseImpl, IPinModel
    {
        public PlakotoBoardModelImpl()
        {
            Fields = new int[24]
            {
                -15,// Field 1  :: Black Home  :: 15 White Checkers
                0,  // Field 2  :: Black Home
                0,  // Field 3  :: Black Home
                0,  // Field 4  :: Black Home
                0,  // Field 5  :: Black Home
                0,  // Field 6  :: Black Home
                0,  // Field 7
                0,  // Field 8
                0,  // Field 9
                0,  // Field 10
                0,  // Field 11
                0,  // Field 12
                0,  // Field 13
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
                15, // Field 24 :: White Home :: 15 Black Checkers
            };

            PinnedFields = new int[24]
            {
                0,  // Field 1  :: Black Home
                0,  // Field 2  :: Black Home
                0,  // Field 3  :: Black Home
                0,  // Field 4  :: Black Home
                0,  // Field 5  :: Black Home
                0,  // Field 6  :: Black Home
                0,  // Field 7
                0,  // Field 8
                0,  // Field 9
                0,  // Field 10
                0,  // Field 11
                0,  // Field 12
                0,  // Field 13
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
		public override GameModus Modus => GameModus.Plakoto;

		// <inheritdoc />
		public override int[] Fields { get; protected set; }

        // <inheritdoc />
        public int[] PinnedFields { get; private set; }

		// <inheritdoc />
		public override Range HomeRangeWhite => new(18, 23);

		// <inheritdoc />
		public override Range HomeRangeBlack => new(5, 0);

		// <inheritdoc />
		public override int BlockAmount => 2;

		// <inheritdoc />
		public override IBoardModel InvertBoard()
		{
			var invertedFields = BoardBroker.InvertBoardFields(Fields);
			var invertedPinnedFields = BoardBroker.InvertBoardFields(PinnedFields);
			return new PlakotoBoardModelImpl()
			{
				// assign white values to black
				BearOffCountBlack = BearOffCountWhite,
				// assign black values to white
				BearOffCountWhite = BearOffCountBlack,
				// inverted board fieds
				Fields = invertedFields,
                PinnedFields = invertedPinnedFields
			};
		}

		// <inheritdoc />
		public override object Clone()
		{
			return new PlakotoBoardModelImpl()
			{
				BearOffCountWhite = BearOffCountWhite,
				BearOffCountBlack = BearOffCountBlack,
				// clone is okay for primitive types
				Fields = (int[])Fields.Clone(),
                PinnedFields = (int[])PinnedFields.Clone()
			};
		}
	}
}
