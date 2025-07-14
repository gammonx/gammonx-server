using GammonX.Engine.Services;

namespace GammonX.Engine.Models
{
    /// <summary>
    /// Portes implementation
    /// <seealso cref="https://www.bkgm.com/variants/Portes.html"/>
    /// <seealso cref="https://www.bkgm.com/variants/Tavli.html"/>
    /// </summary>
    internal class PortesBoardModelImpl : IBoardModel, IHomeBarBoardModel
    {
        public PortesBoardModelImpl()
        {
            Points = new int[24]
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
        }

        // <inheritdoc />
        public GameModus Modus => GameModus.Portes;

        // <inheritdoc />
        public int[] Points { get; private set; }

        // <inheritdoc />
        public Range HomeRangeWhite => new(18, 23);

        // <inheritdoc />
        public Range HomeRangeBlack => new(5, 0);

        // <inheritdoc />
        public int BearOffCountWhite => 0;

        // <inheritdoc />
        public int BearOffCountBlack => 0;

        // <inheritdoc />
        public int BlockAmount => 2;

        // <inheritdoc />
        public int HomeBarCountWhite => 0;

        // <inheritdoc />
        public int HomeBarCountBlack => 0;

        // <inheritdoc />
        public int StartIndexWhite => 0;

        // <inheritdoc />
        public int StartIndexBlack => 23;

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
