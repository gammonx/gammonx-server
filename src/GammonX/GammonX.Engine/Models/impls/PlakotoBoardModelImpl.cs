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

            BlockedPoints = new int[24]
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
        public GameModus Modus => GameModus.Plakoto;

        // <inheritdoc />
        public int[] Fields { get; private set; }

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
