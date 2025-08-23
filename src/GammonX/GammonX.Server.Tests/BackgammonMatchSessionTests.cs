using GammonX.Engine.Models;

using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Utils;

namespace GammonX.Server.Tests
{
	public class BackgammonMatchSessionTests
	{
		private static readonly IGameSessionFactory _gameSessionFactory = new GameSessionFactory();
		private static readonly IMatchSessionFactory _matchSessionFactory = new MatchSessionFactory(_gameSessionFactory);

		[Fact]
		public void BackgammonSingleGameScoreIsCalculatedWhite()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();

			var board = gameSession.BoardModel;
			var doubleCubeBoard = board as IDoublingCubeModel;
			Assert.NotNull(doubleCubeBoard);
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[23] = -1;
			singleGameWinBoard[22] = 14;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(true, 14);
			board.BearOffChecker(false, 1);

			session.RollDices(session.Player1.Id);
			var anyMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, session.Player1.Score);
			Assert.Equal(0, session.Player2.Score);

			var matchState = session.ToPayload();
			Assert.Empty(matchState.AllowedCommands);
			Assert.Equal(1, matchState.GameRound);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, matchState.Player1.Score);
			Assert.Equal(0, matchState.Player2.Score);
			Assert.Equal(session.Id, matchState.Id);
			Assert.Equal(WellKnownMatchVariant.Backgammon, matchState.Variant);
			Assert.Equal(session.Player1.Id, matchState.Player1.Id);
			Assert.Equal(session.Player2.Id, matchState.Player2.Id);
			Assert.Single(matchState.GameRounds);
			Assert.Equal(GameModus.Backgammon, matchState.GameRounds[0].Modus);
			Assert.Equal(GamePhase.GameOver, matchState.GameRounds[0].Phase);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, matchState.GameRounds[0].Score);
			Assert.Equal(0, matchState.GameRounds[0].GameRoundIndex);
			Assert.Equal(session.Player1.Id, matchState.GameRounds[0].Winner);
		}

		[Fact]
		public void BackgammonGammonGameScoreIsCalculatedWhite()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();

			var board = gameSession.BoardModel;
			var doubleCubeBoard = board as IDoublingCubeModel;
			Assert.NotNull(doubleCubeBoard);
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[23] = -1;
			singleGameWinBoard[0] = 14;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(true, 14);
			// no borne off for black

			session.RollDices(session.Player1.Id);
			var anyMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, session.Player1.Score);
			Assert.Equal(0, session.Player2.Score);
			var matchState = session.ToPayload();
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, matchState.Player1.Score);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, matchState.GameRounds[0].Score);
		}

		[Fact]
		public void BackGammonGameScoreLoserHasOnHomebarWhite()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();

			var board = gameSession.BoardModel;
			var doubleCubeBoard = board as IDoublingCubeModel;
			Assert.NotNull(doubleCubeBoard);
			var homeBarBoard = board as IHomeBarModel;
			Assert.NotNull(homeBarBoard);
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[23] = -1;
			singleGameWinBoard[0] = 14;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(true, 14);
			// no borne off for black but has one on homebar
			homeBarBoard.AddToHomeBar(false, 1);

			session.RollDices(session.Player1.Id);
			var anyMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, session.Player1.Score);
			Assert.Equal(0, session.Player2.Score);
			var matchState = session.ToPayload();
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.Player1.Score);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.GameRounds[0].Score);
		}

		[Fact]
		public void BackGammonGameScoreLoserIsInWinnerHomeRangeWhite()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();

			var board = gameSession.BoardModel;
			var doubleCubeBoard = board as IDoublingCubeModel;
			Assert.NotNull(doubleCubeBoard);
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[23] = -1;
			singleGameWinBoard[10] = 14;
			singleGameWinBoard[22] = 1;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(true, 14);
			// no borne off for black but has one in winners home range

			session.RollDices(session.Player1.Id);
			var anyMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, session.Player1.Score);
			Assert.Equal(0, session.Player2.Score);
			var matchState = session.ToPayload();
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.Player1.Score);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.GameRounds[0].Score);
		}

		[Fact]
		public void BackgammonSingleGameScoreIsCalculatedBlack()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();

			var board = gameSession.BoardModel;
			var doubleCubeBoard = board as IDoublingCubeModel;
			Assert.NotNull(doubleCubeBoard);
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[1] = -14;
			singleGameWinBoard[0] = 1;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(false, 14);
			board.BearOffChecker(true, 1);

			session.EndTurn(session.Player1.Id);

			session.RollDices(session.Player2.Id);
			var anyMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, session.Player2.Score);
			Assert.Equal(0, session.Player1.Score);
			var matchState = session.ToPayload();
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, matchState.Player2.Score);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, matchState.GameRounds[0].Score);
		}

		[Fact]
		public void BackgammonGammonGameScoreIsCalculatedBlack()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();

			var board = gameSession.BoardModel;
			var doubleCubeBoard = board as IDoublingCubeModel;
			Assert.NotNull(doubleCubeBoard);
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[0] = 1;
			singleGameWinBoard[23] = -14;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(false, 14);
			// no borne off for white

			session.EndTurn(session.Player1.Id);

			session.RollDices(session.Player2.Id);
			var anyMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, session.Player2.Score);
			Assert.Equal(0, session.Player1.Score);
			var matchState = session.ToPayload();
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, matchState.Player2.Score);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, matchState.GameRounds[0].Score);
		}

		[Fact]
		public void BackGammonGameScoreLoserHasOnHomebarBlack()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();

			var board = gameSession.BoardModel;
			var doubleCubeBoard = board as IDoublingCubeModel;
			Assert.NotNull(doubleCubeBoard);
			var homeBarBoard = board as IHomeBarModel;
			Assert.NotNull(homeBarBoard);
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[23] = -1;
			singleGameWinBoard[0] = 14;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(false, 14);
			// no borne off for white but has one on homebar
			homeBarBoard.AddToHomeBar(true, 1);

			session.EndTurn(session.Player1.Id);

			session.RollDices(session.Player2.Id);
			var anyMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, session.Player2.Score);
			Assert.Equal(0, session.Player1.Score);
			var matchState = session.ToPayload();
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.Player2.Score);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.GameRounds[0].Score);
		}

		[Fact]
		public void BackGammonGameScoreLoserIsInWinnerHomeRangeBlack()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();

			var board = gameSession.BoardModel;
			var doubleCubeBoard = board as IDoublingCubeModel;
			Assert.NotNull(doubleCubeBoard);
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[0] = 1;
			singleGameWinBoard[13] = -14;
			singleGameWinBoard[1] = -1;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(false, 14);
			// no borne off for white but has one in winners home range

			session.EndTurn(session.Player1.Id);

			session.RollDices(session.Player2.Id);
			var anyMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, session.Player2.Score);
			Assert.Equal(0, session.Player1.Score);
			var matchState = session.ToPayload();
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.Player2.Score);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.GameRounds[0].Score);
		}

		[Fact]
		public void BackgammonPlayersCanOfferDoubleBeforeRolling()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			var player1GameState = session.GetGameState(session.Player1.Id, ServerCommands.RollCommand);
			Assert.Contains(ServerCommands.OfferDouble, player1GameState.AllowedCommands);
			var player2GameState = session.GetGameState(session.Player2.Id, ServerCommands.RollCommand);
			Assert.DoesNotContain(ServerCommands.OfferDouble, player2GameState.AllowedCommands);

			session.RollDices(session.Player1.Id);
			Assert.Equal(GamePhase.Rolling, gameSession.Phase);
			player1GameState = session.GetGameState(session.Player1.Id, ServerCommands.RollCommand);
			Assert.DoesNotContain(ServerCommands.OfferDouble, player1GameState.AllowedCommands);
			player2GameState = session.GetGameState(session.Player2.Id, ServerCommands.RollCommand);
			Assert.DoesNotContain(ServerCommands.OfferDouble, player2GameState.AllowedCommands);

			session.EndTurn(session.Player1.Id);
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			player1GameState = session.GetGameState(session.Player1.Id, ServerCommands.RollCommand);
			Assert.DoesNotContain(ServerCommands.OfferDouble, player1GameState.AllowedCommands);
			player2GameState = session.GetGameState(session.Player2.Id, ServerCommands.RollCommand);
			Assert.Contains(ServerCommands.OfferDouble, player2GameState.AllowedCommands);
		}

		[Fact]
		public void BackgammonPlayerCanOfferAndAcceptDoubles()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			var doublingCubeSession = session as IDoubleCubeMatchSession;
			Assert.NotNull(doublingCubeSession);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player2.Id));
			doublingCubeSession.OfferDouble(session.Player1.Id);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player1.Id));
			doublingCubeSession.AcceptDouble(session.Player2.Id);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
		}

		[Fact]
		public void BackgammonPlayerCanOfferAndDeclineDoubles()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			var doublingCubeSession = session as IDoubleCubeMatchSession;
			Assert.NotNull(doublingCubeSession);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player2.Id));
			doublingCubeSession.OfferDouble(session.Player1.Id);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player1.Id));
			doublingCubeSession.DeclineDouble(session.Player2.Id);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
		}

		[Fact]
		public void BackgammonPlayerCannotDoubles()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(WellKnownMatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			var doublingCubeSession = session as IDoubleCubeMatchSession;
			Assert.NotNull(doublingCubeSession);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			session.RollDices(session.Player1.Id);
			Assert.Equal(GamePhase.Rolling, gameSession.Phase);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player2.Id));
			var move = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault(lm => !lm.Used);
			Assert.NotNull(move);
			session.MoveCheckers(session.Player1.Id, move.From, move.To);
			Assert.Equal(GamePhase.Moving, gameSession.Phase);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player2.Id));
			move = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault(lm => !lm.Used);
			Assert.NotNull(move);
			session.MoveCheckers(session.Player1.Id, move.From, move.To);
			Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player2.Id));
			session.EndTurn(session.Player1.Id);
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			// player 2 offers first double
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
			doublingCubeSession.OfferDouble(session.Player2.Id);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player2.Id));
			doublingCubeSession.AcceptDouble(session.Player1.Id);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player2.Id));
			session.EndTurn(session.Player2.Id);
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			// player 1 offers another double
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player2.Id));
			doublingCubeSession.OfferDouble(session.Player1.Id);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player1.Id));
			doublingCubeSession.DeclineDouble(session.Player2.Id);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player2.Id));
			// game is lost after double offer decline
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			// score got doubled 1 * 2
			Assert.Equal(2, session.Player1.Score);
			Assert.Equal(0, session.Player2.Score);
		}
	}
}
