using GammonX.Engine.Services;

namespace GammonX.Engine.Models
{
    /// <summary>
    /// Portes implementation
    /// <seealso cref="https://www.bkgm.com/variants/Plakoto.html"/>
    /// <seealso cref="https://www.bkgm.com/variants/Tavli.html"/>
    /// </summary>
    internal class PlakotoBoardModelImpl : IBoardModel, IHomeBarBoardModel, IPinModel
    {
        public PlakotoBoardModelImpl()
        {
            Points = new int[24]
            {
                -15,// Point 1  :: Black Home  :: 15 White Pieces
                0,  // Point 2  :: Black Home
                0,  // Point 3  :: Black Home
                0,  // Point 4  :: Black Home
                0,  // Point 5  :: Black Home
                0,  // Point 6  :: Black Home
                0,  // Point 7
                0,  // Point 8
                0,  // Point 9
                0,  // Point 10
                0,  // Point 11
                0,  // Point 12
                0,  // Point 13
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
                15, // Point 24 :: White Home :: 15 Black Pieces
            };

            BlockedPoints = new int[24]
            {
                0,  // Point 1  :: Black Home
                0,  // Point 2  :: Black Home
                0,  // Point 3  :: Black Home
                0,  // Point 4  :: Black Home
                0,  // Point 5  :: Black Home
                0,  // Point 6  :: Black Home
                0,  // Point 7
                0,  // Point 8
                0,  // Point 9
                0,  // Point 10
                0,  // Point 11
                0,  // Point 12
                0,  // Point 13
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
        }

        // <inheritdoc />
        public GameModus Modus => GameModus.Plakoto;

        // <inheritdoc />
        public int[] Points { get; private set; }

        // <inheritdoc />
        public int[] BlockedPoints { get; private set; }

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
