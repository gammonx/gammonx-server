using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Engine.Tests.Data;
using GammonX.Engine.Tests.Utils;

namespace GammonX.Engine.Tests
{
	public class LegalMovesTests
	{
		#region Backgammon

		[Theory]
		[ClassData(typeof(BackgammonStartBoardLegalMovesForWhiteTestData))]
		public void CalculateLegalMovesBackGammonBoardRegularDiceRollWhite(
			int roll1,
			int roll2,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Backgammon);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(BackgammonStartBoardLegalMovesForBlackTestData))]
		public void CalculateLegalMovesBackGammonBoardRegularDiceRollBlack(
			int roll1,
			int roll2,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Backgammon);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(BackgammonStartBoardDoubleRollsForWhiteTestData))]
		public void CalculateLegalMovesBackGammonBoardDoubleDiceRollWhite(
			int roll1,
			int roll2,
			int roll3,
			int roll4,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Backgammon);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2, roll3, roll4);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(BackgammonStartBoardDoubleRollsForBlackTestData))]
		public void CalculateLegalMovesBackGammonBoardDoubleDiceRollBlack(
			int roll1,
			int roll2,
			int roll3,
			int roll4,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Backgammon);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2, roll3, roll4);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		#endregion Backgammon

		#region Tavla

		[Theory]
		[ClassData(typeof(BackgammonStartBoardLegalMovesForWhiteTestData))]
		public void CalculateLegalMovesTavlaBoardRegularDiceRollWhite(
			int roll1,
			int roll2,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Tavla);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(BackgammonStartBoardLegalMovesForBlackTestData))]
		public void CalculateLegalMovesTavlaBoardRegularDiceRollBlack(
			int roll1,
			int roll2,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Tavla);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(BackgammonStartBoardDoubleRollsForWhiteTestData))]
		public void CalculateLegalMovesTavlaBoardDoubleDiceRollWhite(
			int roll1,
			int roll2,
			int roll3,
			int roll4,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Tavla);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2, roll3, roll4);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(BackgammonStartBoardDoubleRollsForBlackTestData))]
		public void CalculateLegalMovesTavlaBoardDoubleDiceRollBlack(
			int roll1,
			int roll2,
			int roll3,
			int roll4,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Tavla);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2, roll3, roll4);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		#endregion Tavla

		#region Portes

		[Theory]
		[ClassData(typeof(BackgammonStartBoardLegalMovesForWhiteTestData))]
		public void CalculateLegalMovesPortesBoardRegularDiceRollWhite(
			int roll1,
			int roll2,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Portes);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(BackgammonStartBoardLegalMovesForBlackTestData))]
		public void CalculateLegalMovesPortesBoardRegularDiceRollBlack(
			int roll1,
			int roll2,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Portes);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(BackgammonStartBoardDoubleRollsForWhiteTestData))]
		public void CalculateLegalMovesPortesBoardDoubleDiceRollWhite(
			int roll1,
			int roll2,
			int roll3,
			int roll4,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Portes);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2, roll3, roll4);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(BackgammonStartBoardDoubleRollsForBlackTestData))]
		public void CalculateLegalMovesPortesBoardDoubleDiceRollBlack(
			int roll1,
			int roll2,
			int roll3,
			int roll4,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Portes);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2, roll3, roll4);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		#endregion Portes

		#region Fevga

		[Theory]
		[ClassData(typeof(FevgaStartBoardLegalMovesForWhiteTestData))]
		public void CalculateLegalMovesFevgaBoardRegularDiceRollWhite(
			int roll1,
			int roll2,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Fevga);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(FevgaStartBoardLegalMovesDoubleRollsForWhiteTestData))]
		public void CalculateLegalMovesFevgaBoardDoubleDiceRollWhite(
			int roll1,
			int roll2,
			int roll3,
			int roll4,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Fevga);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2, roll3, roll4);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(FevgaStartBoardLegalMovesForBlackTestData))]
		public void CalculateLegalMovesFevgaBoardRegularDiceRollBlack(
			int roll1,
			int roll2,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Fevga);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(FevgaStartBoardLegalMovesForBlackDoublesTestData))]
		public void CalculateLegalMovesFevgaBoardDoubleDiceRollBlack(
			int roll1,
			int roll2,
			int roll3,
			int roll4,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Fevga);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2, roll3, roll4);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		#endregion Fevga

		#region Plakoto

		[Theory]
		[ClassData(typeof(PlakotoStartBoardLegalMovesForWhiteTestData))]
		public void CalculateLegalMovesPlakotoBoardRegularDiceRollWhite(
			int roll1,
			int roll2,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Plakoto);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(PlakotoStartBoardLegalMovesDoubleRollsForWhiteTestData))]
		public void CalculateLegalMovesPlakotoBoardDoubleDiceRollWhite(
			int roll1,
			int roll2,
			int roll3,
			int roll4,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Plakoto);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2, roll3, roll4);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(PlakotoStartBoardLegalMovesForBlackTestData))]
		public void CalculateLegalMovesPlakotoBoardRegularDiceRollBlack(
			int roll1,
			int roll2,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Plakoto);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Theory]
		[ClassData(typeof(PlakotoStartBoardLegalMovesForBlackDoublesTestData))]
		public void CalculateLegalMovesPlakotoBoardDoubleDiceRollBlack(
			int roll1,
			int roll2,
			int roll3,
			int roll4,
			bool isWhite,
			ValueTuple<int, int>[] expectedMoves)
		{
			var service = BoardServiceFactory.Create(GameModus.Plakoto);
			var board = service.CreateBoard();

			var legalMoves = service.GetLegalMoves(board, isWhite, roll1, roll2, roll3, roll4);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		#endregion

		#region Homebar Tests

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void EnterFromHomebarWhite(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			var homeBarModel = board as IHomeBarModel;
			Assert.NotNull(homeBarModel);
			homeBarModel.AddToHomeBar(true, 5); // Add 5 white checkers to home bar

			var legalMoves = service.GetLegalMoves(board, true, 1);
			Assert.Single(legalMoves);
			Assert.Contains((WellKnownBoardPositions.HomeBarWhite, 0), legalMoves);

			legalMoves = service.GetLegalMoves(board, true, 1, 2);
			Assert.Equal(2, legalMoves.Count());
			Assert.Contains((WellKnownBoardPositions.HomeBarWhite, 0), legalMoves);
			Assert.Contains((WellKnownBoardPositions.HomeBarWhite, 1), legalMoves);
			// TODO :: combined move on board entering not supported atm
			// Assert.Contains((WellKnownBoardPositions.HomeBarWhite, 2), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void EnterFromHomebarBlack(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			var homeBarModel = board as IHomeBarModel;
			Assert.NotNull(homeBarModel);
			homeBarModel.AddToHomeBar(false, 5); // Add 5 black checkers to home bar
			var legalMoves = service.GetLegalMoves(board, false, 1);
			Assert.Single(legalMoves);
			Assert.Contains((WellKnownBoardPositions.HomeBarBlack, 23), legalMoves);

			legalMoves = service.GetLegalMoves(board, false, 1, 2);
			Assert.Equal(2, legalMoves.Count());
			Assert.Contains((WellKnownBoardPositions.HomeBarBlack, 23), legalMoves);
			Assert.Contains((WellKnownBoardPositions.HomeBarBlack, 22), legalMoves);
			// TODO :: combined move on board entering not supported atm
			// Assert.Contains((WellKnownBoardPositions.HomeBarBlack, 21), legalMoves);
		}

		#endregion Homebar Tests

		#region Bearing Off Tests

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void CanBearOffLegalMovesWhite(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.StandardCanBearOffBoard);

			var legalMoves = service.GetLegalMoves(board, true, 1);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((23, WellKnownBoardPositions.BearOffWhite), legalMoves);
			legalMoves = service.GetLegalMoves(board, true, 6, 2);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((22, WellKnownBoardPositions.BearOffWhite), legalMoves);
			Assert.Contains((18, WellKnownBoardPositions.BearOffWhite), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void CanBearOffLegalMovesBlack(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.StandardCanBearOffBoard);

			var legalMoves = service.GetLegalMoves(board, false, 1);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((0, WellKnownBoardPositions.BearOffBlack), legalMoves);
			legalMoves = service.GetLegalMoves(board, false, 6, 2);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((1, WellKnownBoardPositions.BearOffBlack), legalMoves);
			Assert.Contains((5, WellKnownBoardPositions.BearOffBlack), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Fevga)]
		public void CanBearOffLegalMovesWhiteFevga(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.FevgaCanBearOffBoard);

			var legalMoves = service.GetLegalMoves(board, true, 1);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((23, WellKnownBoardPositions.BearOffWhite), legalMoves);
			legalMoves = service.GetLegalMoves(board, true, 6, 2);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((22, WellKnownBoardPositions.BearOffWhite), legalMoves);
			Assert.Contains((18, WellKnownBoardPositions.BearOffWhite), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Fevga)]
		public void CanBearOffLegalMovesBlackFevga(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.FevgaCanBearOffBoard);

			var legalMoves = service.GetLegalMoves(board, false, 1);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((11, WellKnownBoardPositions.BearOffBlack), legalMoves);
			legalMoves = service.GetLegalMoves(board, false, 6, 2);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((10, WellKnownBoardPositions.BearOffBlack), legalMoves);
			Assert.Contains((6, WellKnownBoardPositions.BearOffBlack), legalMoves);
		}

		// TODO: Hit a checker when already bearing off
		// TODO: Deny bearing off if one of the checkers is pinned in the homefield

		#endregion Bearing Off Tests

		#region Blockamount Tests

		// TODO: UNIT TESTS for blocked (already tested within CanMoveChecker)

		#endregion Blockamount Tests

		#region Pinned Tests

		// TODO: UNIT TESTS for pinned (already tested within CanMoveChecker)

		#endregion Pinned Tests

		#region Hitting Tests

		// TODO: UNIT TESTS for hitting (already tested within CanMoveChecker)

		#endregion Hitting Tests
	}
}
