namespace GammonX.Engine.Models
{
	/// <summary>
	/// Fevga implementation.
	/// <seealso cref="https://www.bkgm.com/variants/Fevga.html"/>
	/// <seealso cref="https://www.bkgm.com/variants/Tavli.html"/>
	/// </summary>
	internal sealed class FevgaBoardModelImpl : BoardBaseImpl
    {
        public FevgaBoardModelImpl()
        {
            Fields = new int[24]
            {
                -15, // Field 1  :: 15 White Checkers
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
                15, // Field 13 :: 15 Black Checkers
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
		public override int BlockAmount => 1;

        // <inheritdoc />
        public override Func<bool, int, int, int> MoveOperator => new Func<bool, int, int, int>((isWhite, currentPosition, moveDistance) =>
        {
            if (isWhite)
            {
                // White moves from 0 to 23
                int newPosition = currentPosition + moveDistance;
                return newPosition;
            }
            else
            {
                // Black moves forward (wraps from 23 -> 0)
                int newPosition = (currentPosition + moveDistance) % 24;
                return newPosition;
            }
        });

		// <inheritdoc />
		public override Func<bool, int, int, bool> CanBearOffOperator => new Func<bool, int, int, bool>((isWhite, currentPosition, moveDistance) =>
		{
			if (isWhite)
			{
				int to = MoveOperator(isWhite, currentPosition, moveDistance);
				return to > HomeRangeWhite.End.Value;
			}
			else
			{
				int to = MoveOperator(isWhite, currentPosition, moveDistance);
                return to > HomeRangeBlack.End.Value;
			}
		});

		// <inheritdoc />
		public override Func<bool, int, bool> IsInHomeOperator => new Func<bool, int, bool>((isWhite, position) =>
		{
			if (isWhite && (position < HomeRangeWhite.Start.Value || position > HomeRangeWhite.End.Value)) return false;
			if (!isWhite && (position < HomeRangeBlack.Start.Value || position > HomeRangeBlack.End.Value)) return false;
			return true;
		});
	}
}
