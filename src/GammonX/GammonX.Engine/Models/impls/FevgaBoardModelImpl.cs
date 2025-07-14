namespace GammonX.Engine.Models
{
    /// <summary>
    /// Fevga implementation.
    /// <seealso cref="https://www.bkgm.com/variants/Fevga.html"/>
    /// <seealso cref="https://www.bkgm.com/variants/Tavli.html"/>
    /// </summary>
    internal class FevgaBoardModelImpl : IBoardModel, IBearOffBoardModel
    {
        // <inheritdoc />
        public GameModus Modus => GameModus.Fevga;

        // <inheritdoc />
        public int[] Points => new int[24]
        {
            -15, // Point 1  :: 15 White Pieces
            0,  // Point 2
            0,  // Point 3
            0,  // Point 4
            0,  // Point 5
            0,  // Point 6
            0,  // Point 7  :: Black Home
            0,  // Point 8  :: Black Home
            0,  // Point 9  :: Black Home
            0,  // Point 10 :: Black Home
            0,  // Point 11 :: Black Home
            0,  // Point 12 :: Black Home
            15, // Point 13 :: 15 Black Pieces
            0,  // Point 14
            0,  // Point 15
            0,  // Point 16
            0,  // Point 17
            0,  // Point 18
            0,  // Point 19 :: White Home
            0,  // Point 20 :: White Home
            0,  // Point 21 :: White Home
            0,  // Point 22 :: White Home
            0,  // Point 23 :: White Home 
            0,  // Point 24 :: White Home

        };

        // <inheritdoc />
        public Range WhiteHome => new(18, 23);

        // <inheritdoc />
        public Range BlackHome => new(6, 11);

        // <inheritdoc />
        public int BearOffWhite => 0;

        // <inheritdoc />
        public int BearOffBlack => 0;

        // <inheritdoc />
        public int BarWhite => 0;

        // <inheritdoc />
        public int BarBlack => 0;

        // <inheritdoc />
        public Func<int, int, int> WhiteMoveOperator => new Func<int, int, int>((currentPosition, moveDistance) =>
        {
            // White moves from 0 to 23
            int newPosition = currentPosition + moveDistance;
            return newPosition;
        });

        // <inheritdoc />
        public Func<int, int, int> BlackMoveOperator => new Func<int, int, int>((currentPosition, moveDistance) =>
        {
            // Black moves forward (wraps from 23 -> 0)
            int newPosition = (currentPosition + moveDistance) % 24;
            return newPosition;
        });
    }
}
