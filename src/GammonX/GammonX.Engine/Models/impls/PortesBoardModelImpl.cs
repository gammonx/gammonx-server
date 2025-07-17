using GammonX.Engine.Models;
using GammonX.Engine.Services;

namespace GammonX.Engine.Models
{
	/// <summary>
	/// Portes implementation
	/// <seealso cref="https://www.bkgm.com/variants/Portes.html"/>
	/// <seealso cref="https://www.bkgm.com/variants/Tavli.html"/>
	/// </summary>
	internal sealed class PortesBoardModelImpl : BoardBaseImpl, IHomeBarModel
    {
        public PortesBoardModelImpl()
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
        public override GameModus Modus => GameModus.Portes;

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
	}
}
