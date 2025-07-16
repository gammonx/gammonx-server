using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Engine.Tests.Data;

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
	}
}
