using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2, roll3, roll4);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2, roll3, roll4);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2, roll3, roll4);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2, roll3, roll4);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2, roll3, roll4);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2, roll3, roll4);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2, roll3, roll4);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2, roll3, roll4);
			Assert.Equal(expectedMoves.Length, legalMoves.Length);
			for (int i = 0; i < expectedMoves.Length; i++)
			{
				Assert.Contains(expectedMoves[i], legalMoves);
			}
		}

		[Fact]
		public void CreatePrimeOnStartRangeFevgaBlack()
		{
			var service = BoardServiceFactory.Create(GameModus.Fevga);
			var board = service.CreateBoard();

			// open starting field
			service.MoveChecker(board, -1, 14, true);
			service.MoveChecker(board, 0, 14, true);

			// cannot create a 6er prime in opponents start field
			service.MoveChecker(board, 24, 18, false);
			service.MoveChecker(board, 24, 17, false);
			service.MoveChecker(board, 24, 16, false);
			service.MoveChecker(board, 24, 15, false);
			service.MoveChecker(board, 24, 14, false);
			Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, 24, 13, false));
			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 13);
			Assert.DoesNotContain((24, 0), legalMoves);

			// can create a 6er prime in opponents start field
			service.MoveChecker(board, 24, 24, false);
			service.MoveChecker(board, 24, 23, false);
			service.MoveChecker(board, 24, 22, false);
			service.MoveChecker(board, 24, 21, false);
			service.MoveChecker(board, 24, 20, false);
			legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 19);
			Assert.Contains((24, 6), legalMoves);
			service.MoveChecker(board, 24, 19, false);

		}

		[Fact]
		public void CreatePrimeOnStartRangeFevgaWhite()
		{
			var service = BoardServiceFactory.Create(GameModus.Fevga);
			var board = service.CreateBoard();

			// open starting field
			service.MoveChecker(board, 24, 14, false);
			service.MoveChecker(board, 12, 14, false);

			// cannot create a 6er prime in opponents start field
			service.MoveChecker(board, -1, 18, true);
			service.MoveChecker(board, -1, 17, true);
			service.MoveChecker(board, -1, 16, true);
			service.MoveChecker(board, -1, 15, true);
			service.MoveChecker(board, -1, 14, true);
			Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, -1, 13, true));
			var legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 13);
			Assert.DoesNotContain((-1, 23), legalMoves);

			// can create a 6er prime in opponents start field
			service.MoveChecker(board, -1, 24, true);
			service.MoveChecker(board, -1, 23, true);
			service.MoveChecker(board, -1, 22, true);
			service.MoveChecker(board, -1, 21, true);
			service.MoveChecker(board, -1, 20, true);
			legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 19);
			Assert.Contains((-1, 18), legalMoves);
			service.MoveChecker(board, -1, 19, true);
		}

		[Fact]
		public void InvalidPrimeCreationFevga()
		{
			var service = BoardServiceFactory.Create(GameModus.Fevga);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.FevgaInvalidPrimeCreationBlack);
			if (board is IHomeBarModel homeBarModel)
			{
				homeBarModel.RemoveFromHomeBar(true, 14);
				homeBarModel.RemoveFromHomeBar(false, 14);
			}

			var legalMoveSequences = service.GetLegalMoveSequences(board, false, 3, 3, 3, 3);
			foreach (var movSeq in legalMoveSequences)
			{
				var clone = board.DeepClone();
				foreach (var move in movSeq.Moves)
				{
					service.MoveCheckerTo(clone, move.From, move.To, false);
				}
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2, roll3, roll4);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, isWhite, roll1, roll2, roll3, roll4);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 1);
			Assert.Single(legalMoves);
			Assert.Contains((WellKnownBoardPositions.HomeBarWhite, 0), legalMoves);

			legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 1, 2);
			Assert.Equal(2, legalMoves.Count());
			Assert.Contains((WellKnownBoardPositions.HomeBarWhite, 0), legalMoves);
			Assert.Contains((WellKnownBoardPositions.HomeBarWhite, 1), legalMoves);
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
			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 1);
			Assert.Single(legalMoves);
			Assert.Contains((WellKnownBoardPositions.HomeBarBlack, 23), legalMoves);

			legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 1, 2);
			Assert.Equal(2, legalMoves.Count());
			Assert.Contains((WellKnownBoardPositions.HomeBarBlack, 23), legalMoves);
			Assert.Contains((WellKnownBoardPositions.HomeBarBlack, 22), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void WhiteHasToPlayWhiteCheckerFromHomebar(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.BackgammonHomebarWhite);
			if (board is IHomeBarModel homeBar)
			{
				homeBar.AddToHomeBar(true, 1);
			}

			var moveSequences = service.GetLegalMoveSequences(board, true, 2, 4);
			Assert.Equal(8, moveSequences.Length);
			// all seuqences has to play in the homebar first
			Assert.True(moveSequences.All(ms => ms.Moves[0].From == -1));
			Assert.True(moveSequences.All(ms => ms.Moves.Count == 2));
			// we now try to move something other than the homebar checker
			var ilegalMove = moveSequences.First().Moves.Last();
			Assert.Throws<InvalidOperationException>(() => service.MoveCheckerTo(board, ilegalMove.From, ilegalMove.To, true));
			var legalMove = moveSequences.First().Moves.First();
			service.MoveCheckerTo(board, legalMove.From, legalMove.To, true);
			service.MoveCheckerTo(board, ilegalMove.From, ilegalMove.To, true);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void WhiteHasToPlayTwoWhiteCheckerFromHomebarFirstOnPasch(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.BackgammonHomebarWhite);
			if (board is IHomeBarModel homeBar)
			{
				homeBar.AddToHomeBar(true, 2);
			}

			var moveSequences = service.GetLegalMoveSequences(board, true, 1, 1, 1, 1);
			// all seuqences has to play in the two checkers from the homebar first
			Assert.True(moveSequences.All(ms => ms.Moves[0].From == -1));
			Assert.True(moveSequences.All(ms => ms.Moves[1].From == -1));
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 1);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((23, WellKnownBoardPositions.BearOffWhite), legalMoves);
			legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 6, 2);
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

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 1);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((0, WellKnownBoardPositions.BearOffBlack), legalMoves);
			legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 6, 2);
			Assert.Equal(7, legalMoves.Count());
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
			var homebarModel = board as IHomeBarModel;
			Assert.NotNull(homebarModel);
			homebarModel.RemoveFromHomeBar(true, 14);

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 1);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((23, WellKnownBoardPositions.BearOffWhite), legalMoves);
			legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 6, 2);
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
			var homebarModel = board as IHomeBarModel;
			Assert.NotNull(homebarModel);
			homebarModel.RemoveFromHomeBar(false, 14);

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 1);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((11, WellKnownBoardPositions.BearOffBlack), legalMoves);
			legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 6, 2);
			Assert.Equal(6, legalMoves.Count());
			Assert.Contains((10, WellKnownBoardPositions.BearOffBlack), legalMoves);
			Assert.Contains((6, WellKnownBoardPositions.BearOffBlack), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void CannotBearOffOnHittedCheckerLegalMovesWhite(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.StandardCanBearOffBoard);
			var homeBarModel = board as IHomeBarModel;
			Assert.NotNull(homeBarModel);

			homeBarModel.AddToHomeBar(true, 1);
			var legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 1);
			// white cannot enter board, black checkers block entry
			Assert.Empty(legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void CannotBearOffOnHittedCheckerLegalMovesBlack(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.StandardCanBearOffBoard);
			var homeBarModel = board as IHomeBarModel;
			Assert.NotNull(homeBarModel);

			homeBarModel.AddToHomeBar(false, 1);
			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 2);
			// black cannot enter board, white checkers block entry
			Assert.Empty(legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Plakoto)]
		public void CannotBearOffOnPinnedCheckerLegalMovesWhite(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.StandardCanBearOffBoard);
			var pinModel = board as IPinModel;
			Assert.NotNull(pinModel);

			// pin a white checker in opponents home field
			service.MoveCheckerTo(board, 5, 23, false);
			// white cannot enter board, black checkers block entry
			var legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 2);
			Assert.Equal(3, legalMoves.Count());
			Assert.DoesNotContain((23, WellKnownBoardPositions.BearOffWhite), legalMoves);
			Assert.DoesNotContain((21, 23), legalMoves);
			legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 6, 2);
			Assert.Equal(3, legalMoves.Count());
			Assert.DoesNotContain((22, WellKnownBoardPositions.BearOffWhite), legalMoves);
			Assert.DoesNotContain((18, WellKnownBoardPositions.BearOffWhite), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Plakoto)]
		public void CannotBearOffOnPinnedCheckerLegalMovesBlack(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.StandardCanBearOffBoard);
			var pinModel = board as IPinModel;
			Assert.NotNull(pinModel);

			// pin a black checker in opponents home field
			service.MoveCheckerTo(board, 18, 3, true);
			// black cannot enter board, white checkers block entry
			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 3);
			Assert.Equal(2, legalMoves.Count());
			Assert.DoesNotContain((3, WellKnownBoardPositions.BearOffBlack), legalMoves);
			legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 6, 3);
			Assert.Equal(2, legalMoves.Count());
			Assert.DoesNotContain((3, WellKnownBoardPositions.BearOffBlack), legalMoves);
			Assert.DoesNotContain((5, WellKnownBoardPositions.BearOffBlack), legalMoves);
		}

		#endregion Bearing Off Tests

		#region Blockamount Tests

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void BlockedLegalMovesStandardWhite(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();

			// blocked
			service.MoveCheckerTo(board, 5, 4, false);
			service.MoveCheckerTo(board, 5, 4, false);
			// not blocked
			service.MoveCheckerTo(board, 5, 3, false);
			Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, 5, 5, false));
						
			var legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 3, 4, 5);
			// single checker does not block
			Assert.Contains((0, 3), legalMoves);
			// double checker do block
			Assert.DoesNotContain((0, 4), legalMoves);
			Assert.DoesNotContain((0, 5), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void BlockedLegalMovesStandardBlack(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();

			// blocked
			service.MoveCheckerTo(board, 18, 19, true);
			service.MoveCheckerTo(board, 18, 19, true);
			// not blocked
			service.MoveCheckerTo(board, 18, 20, true);
			Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, 18, 5, true));

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 3, 4, 5);
			// single checker does not block
			Assert.Contains((23, 20), legalMoves);
			// double checker do block
			Assert.DoesNotContain((23, 19), legalMoves);
			Assert.DoesNotContain((23, 18), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Fevga)]
		public void BlockedLegalMovesFevgaBlack(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();

			// blocked
			service.MoveCheckerTo(board, -1, 13, true);
			service.MoveCheckerTo(board, -1, 14, true);
			Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, -1, 13, true));

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 1, 2);
			// single checker do block
			Assert.DoesNotContain((24, 13), legalMoves);
			Assert.DoesNotContain((24, 14), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Fevga)]
		public void BlockedLegalMovesFevgaWhite(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();

			// blocked
			service.MoveCheckerTo(board, 24, 1, false);
			service.MoveCheckerTo(board, 24, 2, false);
			Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, 24, 13, false));

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 1, 2);
			// single checker do block
			Assert.DoesNotContain((0, 1), legalMoves);
			Assert.DoesNotContain((0, 2), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Plakoto)]
		public void BlockedLegalMovesPlakotoBlack(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();

			// blocked
			service.MoveCheckerTo(board, 0, 22, true);
			service.MoveCheckerTo(board, 0, 22, true);
			// not blocked
			service.MoveCheckerTo(board, 0, 21, true);
			Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, 0, 23, true));

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 1, 2);
			// double checkers do block
			Assert.DoesNotContain((24, 22), legalMoves);
			// we need to use both dices
			Assert.DoesNotContain((23, 21), legalMoves);
			// single or none checkers do not block
			Assert.Contains((23, 20), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Plakoto)]
		public void BlockedLegalMovesPlakotoWhite(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();

			// blocked
			service.MoveCheckerTo(board, 23, 1, false);
			service.MoveCheckerTo(board, 23, 1, false);
			// not blocked
			service.MoveCheckerTo(board, 23, 2, false);
			Assert.Throws<InvalidOperationException>(() => service.MoveChecker(board, 23, 23, false));

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 1, 2);
			// double checkers do block
			Assert.DoesNotContain((0, 1), legalMoves);
			// we need to use both dices
			Assert.DoesNotContain((0, 2), legalMoves);
			// single or none checkers do not block
			Assert.Contains((0, 3), legalMoves);
		}

		#endregion Blockamount Tests

		#region Pinned Tests

		[Theory]
		[InlineData(GameModus.Plakoto)]
		public void PinnedLegalMovesPlakotoBlack(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			var pinModel = board as IPinModel;
			Assert.NotNull(pinModel);

			service.MoveCheckerTo(board, 0, 18, true);

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 5);
			// single checkers do not block
			Assert.Contains((23, 18), legalMoves);
			service.MoveChecker(board, 23, 5, false);
			Assert.Equal(-1, pinModel.PinnedFields[18]);
			// pinned checker cannot be moved
			legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 1);
			Assert.Contains((0, 1), legalMoves);
			Assert.DoesNotContain((18, 19), legalMoves);
			// unpin checker
			service.MoveChecker(board, 18, 1, false);
			Assert.Equal(0, pinModel.PinnedFields[18]);
			// unpinned checker cannot be moved
			legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 1);
			Assert.Contains((0, 1), legalMoves);
			Assert.Contains((18, 19), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Plakoto)]
		public void PinnedLegalMovesPlakotoWhite(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			var pinModel = board as IPinModel;
			Assert.NotNull(pinModel);

			service.MoveCheckerTo(board, 23, 5, false);

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 5);
			// single checkers do not block
			Assert.Contains((0, 5), legalMoves);
			service.MoveChecker(board, 0, 5, true);
			Assert.Equal(1, pinModel.PinnedFields[5]);
			// pinned checker cannot be moved
			legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 1);
			Assert.Contains((23, 22), legalMoves);
			Assert.DoesNotContain((5, 4), legalMoves);
			// unpin checker
			service.MoveChecker(board, 5, 1, true);
			Assert.Equal(0, pinModel.PinnedFields[5]);
			// unpinned checker cannot be moved
			legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 1);
			Assert.Contains((23, 22), legalMoves);
			Assert.Contains((5, 4), legalMoves);
		}

		#endregion Pinned Tests

		#region Hitting Tests

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void HitLegalMovesStandardWhite(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			var homebarModel = board as IHomeBarModel;
			Assert.NotNull(homebarModel);

			service.MoveCheckerTo(board, 5, 3, false);

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 3);
			// single checker does not block
			Assert.Contains((0, 3), legalMoves);
			// hit single checker
			service.MoveChecker(board, 0, 3, true);
			Assert.Equal(1, homebarModel.HomeBarCountBlack);
			legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 2);
			// must enter from homebar first
			Assert.Contains((24, 22), legalMoves);
			service.MoveChecker(board, 24, 2, false);
			// hit new single checker
			legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 2);
			Assert.Contains((5, 3), legalMoves);
			service.MoveChecker(board, 5, 2, false);
			Assert.Equal(1, homebarModel.HomeBarCountWhite);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		public void HitLegalMovesStandardBlack(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			var homebarModel = board as IHomeBarModel;
			Assert.NotNull(homebarModel);

			service.MoveCheckerTo(board, 18, 20, true);

			var legalMoves = service.GetLegalMovesAsFlattenedList(board, false, 3);
			// single checker does not block
			Assert.Contains((23, 20), legalMoves);
			// hit single checker
			service.MoveChecker(board, 23, 3, false);
			Assert.Equal(1, homebarModel.HomeBarCountWhite);
			legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 2);
			// must enter from homebar first
			Assert.Contains((-1, 1), legalMoves);
			service.MoveChecker(board, -1, 2, true);
			// hit new single checker
			legalMoves = service.GetLegalMovesAsFlattenedList(board, true, 2);
			Assert.Contains((18, 20), legalMoves);
			service.MoveChecker(board, 18, 2, true);
			Assert.Equal(1, homebarModel.HomeBarCountBlack);
		}

		#endregion Hitting Tests

		#region Dice Rule Tests

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		[InlineData(GameModus.Fevga)]
		[InlineData(GameModus.Plakoto)]
		public void BothDicesHasToBeUsedWhite(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.BothDicesMustBeUsedBoardStandardWhite);
			if (board is IHomeBarModel homeBar)
			{
				homeBar.RemoveFromHomeBar(true, 14);
				homeBar.RemoveFromHomeBar(false, 14);
			}

			var moveSequences = service.GetLegalMoveSequences(board, true, 4, 5);
			var legalMoves = moveSequences.SelectMany(ms => ms.Moves).ToArray();
			Assert.Equal(4, legalMoves.Length); // 2 unique moves
			Assert.DoesNotContain(new MoveModel(4, 8), legalMoves);
			Assert.Contains(new MoveModel(0, 4), legalMoves);
			Assert.Contains(new MoveModel(4, 9), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		[InlineData(GameModus.Fevga, Skip = "need to create proper mock board")]
		[InlineData(GameModus.Plakoto)]
		public void BothDicesHasToBeUsedBlack(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.BothDicesMustBeUsedBoardStandardBlack);
			if (board is IHomeBarModel homeBar)
			{
				homeBar.RemoveFromHomeBar(true, 14);
				homeBar.RemoveFromHomeBar(false, 14);
			}

			var moveSequences = service.GetLegalMoveSequences(board, false, 4, 5);
			var legalMoves = moveSequences.SelectMany(ms => ms.Moves).ToArray();
			Assert.Equal(4, legalMoves.Length); // 2 unique moves
			Assert.DoesNotContain(new MoveModel(19, 15), legalMoves);
			Assert.Contains(new MoveModel(23, 19), legalMoves);
			Assert.Contains(new MoveModel(19, 14), legalMoves);
		}

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		[InlineData(GameModus.Fevga)]
		[InlineData(GameModus.Plakoto)]
		public void HigherDiceHasToBeUsedWhite(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.HigherRollMustBeUsedBoardStandardWhite);
			if (board is IHomeBarModel homeBar)
			{
				homeBar.RemoveFromHomeBar(true, 14);
				homeBar.RemoveFromHomeBar(false, 14);
			}

			var moveSequences = service.GetLegalMoveSequences(board, true, 3, 5);
			var legalMoves = moveSequences.SelectMany(ms => ms.Moves).ToArray();
			Assert.Single(legalMoves);
			Assert.DoesNotContain(new MoveModel(0, 3), legalMoves);
			Assert.Contains(new MoveModel(0, 5), legalMoves);
		}



		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		[InlineData(GameModus.Fevga, Skip = "need to create proper mock board")]
		[InlineData(GameModus.Plakoto)]
		public void HigherDiceHasToBeUsedBlack(GameModus modus)
		{
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.HigherRollMustBeUsedBoardStandardBlack);
			if (board is IHomeBarModel homeBar)
			{
				homeBar.RemoveFromHomeBar(true, 14);
				homeBar.RemoveFromHomeBar(false, 14);
			}

			var moveSequences = service.GetLegalMoveSequences(board, false, 3, 5);
			var legalMoves = moveSequences.SelectMany(ms => ms.Moves).ToArray();
			Assert.Single(legalMoves);
			Assert.DoesNotContain(new MoveModel(23, 20), legalMoves);
			Assert.Contains(new MoveModel(23, 18), legalMoves);
		}
		
		#endregion Dice Rule Tests
	}
}
