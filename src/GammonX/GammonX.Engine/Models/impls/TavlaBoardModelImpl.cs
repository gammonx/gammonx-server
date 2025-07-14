namespace GammonX.Engine.Models
{
    /// <summary>
    /// Tavla implementation.
    /// <seealso cref="https://www.bkgm.com/variants/Tavla.html"/>
    /// </summary>
    internal class TavlaBoardModelImpl : IBoardModel, IBearOffBoardModel
    {
        // <inheritdoc />
        public GameModus Modus => GameModus.Tavla;

        // <inheritdoc />
        public int[] Points => new int[24]
        {
            -2, // Point 1 :: Black Home :: 2 White pieces
            0,  // Point 2
            0,  // Point 3
            0,  // Point 4
            0,  // Point 5
            5,  // Point 6 :: 5 Black pieces
            0,  // Point 7
            3,  // Point 8 :: 3 Black pieces
            0,  // Point 9
            0,  // Point 10
            0,  // Point 11
            -5, // Point 12 :: 5 White pieces
            5,  // Point 13 :: 5 Black pieces
            0,  // Point 14
            0,  // Point 15
            0,  // Point 16
            -3, // Point 17 :: 3 White pieces
            0,  // Point 18
            -5, // Point 19 :: 5 White pieces
            0,  // Point 20
            0,  // Point 21
            0,  // Point 22
            0,  // Point 23 
            2,  // Point 24 :: White Home :: 2 Black pieces

        };

        // <inheritdoc />
        public Range WhiteHome => new(18, 23);

        // <inheritdoc />
        public Range BlackHome => new(0, 5);

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
            // White moves from 23 to 0
            int newPosition = currentPosition - moveDistance;
            return newPosition;
        });
    }
}
