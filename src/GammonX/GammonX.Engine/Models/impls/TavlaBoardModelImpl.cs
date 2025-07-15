using GammonX.Engine.Services;

namespace GammonX.Engine.Models
{
    /// <summary>
    /// Tavla implementation.
    /// <seealso cref="https://www.bkgm.com/variants/Tavla.html"/>
    /// </summary>
    internal class TavlaBoardModelImpl : IBoardModel, IHomeBarBoardModel
    {
        public TavlaBoardModelImpl()
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
        public GameModus Modus => GameModus.Tavla;

        // <inheritdoc />
        public int[] Fields { get; private set; }

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
        public Func<bool, int, int, int> MoveOperator => new Func<bool, int, int, int>((isWhite, currentPosition, moveDistance) =>
        {
            if (isWhite)
            {
                // White moves from 0 to 23
                int newPosition = currentPosition + moveDistance;
                return newPosition;
            }
            else
            {
                // Black moves from 23 to 0
                int newPosition = currentPosition - moveDistance;
                return newPosition;
            }
        });

        // <inheritdoc />
        public Func<bool, int, int, bool> CanBearOffOperator => new Func<bool, int, int, bool>((isWhite, currentPosition, moveDistance) =>
        {
            if (isWhite)
            {
                int to = MoveOperator(isWhite, currentPosition, moveDistance);
                return to > HomeRangeWhite.End.Value;
            }
            else
            {
                int to = MoveOperator(isWhite, currentPosition, moveDistance);
                return to < HomeRangeBlack.End.Value;
            }
        });

        // <inheritdoc />
        public Func<bool, int, bool> IsInHomeOperator => new Func<bool, int, bool>((isWhite, position) =>
        {
            if (isWhite && (position < HomeRangeWhite.Start.Value || position > HomeRangeWhite.End.Value)) return false;
            if (!isWhite && (position > HomeRangeBlack.Start.Value || position < HomeRangeBlack.End.Value)) return false;
            return true;
        });
    }
}
