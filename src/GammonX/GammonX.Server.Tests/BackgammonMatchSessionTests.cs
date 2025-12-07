using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Utils;

using Moq;

namespace GammonX.Server.Tests
{
	public class BackgammonMatchSessionTests
	{
		private static readonly IDiceServiceFactory _diceServiceFactory = new DiceServiceFactory();
		private static readonly IGameSessionFactory _gameSessionFactory = new GameSessionFactory(_diceServiceFactory);
		private static readonly IMatchSessionFactory _matchSessionFactory = new MatchSessionFactory(_gameSessionFactory);

		[Fact]
		public void BackgammonSingleGameScoreIsCalculatedWhite()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);

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
			var anyMove = gameSession.MoveSequences.FirstOrDefault()?.Moves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(session.Player1.Id, gameSession.Result.WinnerId);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, gameSession.Result.Points);
			Assert.Equal(GameResult.Single, gameSession.Result.WinnerResult);
			Assert.Equal(GameResult.LostSingle, gameSession.Result.LoserResult);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, session.Player1.Points);
			Assert.Equal(0, session.Player2.Points);

			var matchState = session.ToPayload(session.Player1.Id);
			Assert.Empty(matchState.AllowedCommands);
			Assert.Equal(1, matchState.GameRound);
			Assert.NotNull(matchState.Player1);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, matchState.Player1.Points);
			Assert.NotNull(matchState.Player2);
			Assert.Equal(0, matchState.Player2.Points);
			Assert.Equal(session.Id, matchState.Id);
			Assert.Equal(MatchVariant.Backgammon, matchState.Variant);
			Assert.Equal(session.Player1.Id, matchState.Player1.Id);
			Assert.Equal(session.Player2.Id, matchState.Player2.Id);
			Assert.NotNull(matchState.GameRounds);
			Assert.Single(matchState.GameRounds);
			Assert.Equal(GameModus.Backgammon, matchState.GameRounds[0].Modus);
			Assert.Equal(GamePhase.GameOver, matchState.GameRounds[0].Phase);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, matchState.GameRounds[0].Points);
			Assert.Equal(0, matchState.GameRounds[0].GameRoundIndex);
			Assert.Equal(session.Player1.Id, matchState.GameRounds[0].Winner);
			Assert.Equal(session.Player1.Id, matchState.Winner);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, matchState.WinnerPoints);
			Assert.Equal(session.Player2.Id, matchState.Loser);
			Assert.Equal(0, matchState.LoserPoints);
		}

		[Fact]
		public void BackgammonGammonGameScoreIsCalculatedWhite()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);

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
			var anyMove = gameSession.MoveSequences.FirstOrDefault()?.Moves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.Equal(session.Player1.Id, gameSession.Result.WinnerId);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, gameSession.Result.Points);
            Assert.Equal(GameResult.Gammon, gameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostGammon, gameSession.Result.LoserResult);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, session.Player1.Points);
			Assert.Equal(0, session.Player2.Points);
			var matchState = session.ToPayload(session.Player1.Id);
			Assert.NotNull(matchState.Player1);
			Assert.NotNull(matchState.GameRounds);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, matchState.Player1.Points);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, matchState.GameRounds[0].Points);
			Assert.Equal(session.Player1.Id, matchState.Winner);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, matchState.WinnerPoints);
			Assert.Equal(session.Player2.Id, matchState.Loser);
			Assert.Equal(0, matchState.LoserPoints);
		}

		[Fact]
		public void BackGammonGameScoreLoserHasOnHomebarWhite()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);

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
			var anyMove = gameSession.MoveSequences.FirstOrDefault()?.Moves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.Equal(session.Player1.Id, gameSession.Result.WinnerId);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, gameSession.Result.Points);
            Assert.Equal(GameResult.Backgammon, gameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostBackgammon, gameSession.Result.LoserResult);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, session.Player1.Points);
			Assert.Equal(0, session.Player2.Points);
			var matchState = session.ToPayload(session.Player1.Id);
			Assert.NotNull(matchState.Player1);
			Assert.NotNull(matchState.GameRounds);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.Player1.Points);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.GameRounds[0].Points);
		}

		[Fact]
		public void BackGammonGameScoreLoserIsInWinnerHomeRangeWhite()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);

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
			var anyMove = gameSession.MoveSequences.FirstOrDefault()?.Moves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.Equal(session.Player1.Id, gameSession.Result.WinnerId);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, gameSession.Result.Points);
            Assert.Equal(GameResult.Backgammon, gameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostBackgammon, gameSession.Result.LoserResult);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, session.Player1.Points);
			Assert.Equal(0, session.Player2.Points);
			var matchState = session.ToPayload(session.Player1.Id);
			Assert.NotNull(matchState.Player1);
			Assert.NotNull(matchState.GameRounds);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.Player1.Points);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.GameRounds[0].Points);
		}

		[Fact]
		public void BackgammonSingleGameScoreIsCalculatedBlack()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);

			var board = gameSession.BoardModel;
			var doubleCubeBoard = board as IDoublingCubeModel;
			Assert.NotNull(doubleCubeBoard);
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[1] = -14;
			singleGameWinBoard[0] = 1;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(false, 14);
			board.BearOffChecker(true, 1);

			// execute a full turn
			session.RollDices(session.Player1.Id);
			var anyMoveSeq = gameSession.MoveSequences.FirstOrDefault();
			Assert.NotNull(anyMoveSeq);
			foreach (var move in anyMoveSeq.Moves)
			{
				session.MoveCheckers(session.Player1.Id, move.From, move.To);
			}

			session.EndTurn(session.Player1.Id);

			session.RollDices(session.Player2.Id);
			var anyMove = gameSession.MoveSequences.FirstOrDefault()?.Moves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
            Assert.Equal(session.Player2.Id, gameSession.Result.WinnerId);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, gameSession.Result.Points);
            Assert.Equal(GameResult.Single, gameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostSingle, gameSession.Result.LoserResult);
            Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, session.Player2.Points);
			Assert.Equal(0, session.Player1.Points);
			var matchState = session.ToPayload(session.Player2.Id);
			Assert.NotNull(matchState.Player2);
			Assert.NotNull(matchState.GameRounds);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, matchState.Player2.Points);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 1, matchState.GameRounds[0].Points);
		}

		[Fact]
		public void BackgammonGammonGameScoreIsCalculatedBlack()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);

			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
			gameSession.InjectDiceServiceMock(mock.Object);

			var board = gameSession.BoardModel;
			var doubleCubeBoard = board as IDoublingCubeModel;
			Assert.NotNull(doubleCubeBoard);
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[0] = 1;
			singleGameWinBoard[18] = -14;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(false, 14);
			// no borne off for white

			// execute a turn for white checkers
			session.RollDices(session.Player1.Id);
			var anyMoveSeq = gameSession.MoveSequences.FirstOrDefault();
			Assert.NotNull(anyMoveSeq);
			foreach (var move in anyMoveSeq.Moves)
			{
				session.MoveCheckers(session.Player1.Id, move.From, move.To);
			}
			session.EndTurn(session.Player1.Id);

			session.RollDices(session.Player2.Id);
			var anyMove = gameSession.MoveSequences.FirstOrDefault()?.Moves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.Equal(session.Player2.Id, gameSession.Result.WinnerId);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, gameSession.Result.Points);
            Assert.Equal(GameResult.Gammon, gameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostGammon, gameSession.Result.LoserResult);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, session.Player2.Points);
			Assert.Equal(0, session.Player1.Points);
			var matchState = session.ToPayload(session.Player2.Id);
			Assert.NotNull(matchState.Player2);
			Assert.NotNull(matchState.GameRounds);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, matchState.Player2.Points);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 2, matchState.GameRounds[0].Points);
		}

		[Fact]
		public void BackGammonGameScoreLoserHasOnHomebarBlack()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);

			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
			gameSession.InjectDiceServiceMock(mock.Object);

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

			// execute a turn for white checkers
			session.RollDices(session.Player1.Id);
			var anyMoveSeq = gameSession.MoveSequences.FirstOrDefault();
			Assert.NotNull(anyMoveSeq);
			foreach (var move in anyMoveSeq.Moves)
			{
				session.MoveCheckers(session.Player1.Id, move.From, move.To);
			}
			session.EndTurn(session.Player1.Id);

			session.RollDices(session.Player2.Id);
			var anyMove = gameSession.MoveSequences.FirstOrDefault()?.Moves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.Equal(session.Player2.Id, gameSession.Result.WinnerId);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, gameSession.Result.Points);
            Assert.Equal(GameResult.Backgammon, gameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostBackgammon, gameSession.Result.LoserResult);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, session.Player2.Points);
			Assert.Equal(0, session.Player1.Points);
			var matchState = session.ToPayload(session.Player2.Id);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.Player2?.Points);
			Assert.NotNull(matchState.GameRounds);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.GameRounds[0].Points);
		}

		[Fact]
		public void BackGammonGameScoreLoserIsInWinnerHomeRangeBlack()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);

			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([1, 2]);
			gameSession.InjectDiceServiceMock(mock.Object);

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

			// execute a turn for white checkers
			session.RollDices(session.Player1.Id);
			var anyMoveSeq = gameSession.MoveSequences.FirstOrDefault();
			Assert.NotNull(anyMoveSeq);
			foreach (var move in anyMoveSeq.Moves)
			{
				session.MoveCheckers(session.Player1.Id, move.From, move.To);
			}
			session.EndTurn(session.Player1.Id);

			session.RollDices(session.Player2.Id);
			var anyMove = gameSession.MoveSequences.FirstOrDefault()?.Moves.FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.Equal(session.Player2.Id, gameSession.Result.WinnerId);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, gameSession.Result.Points);
            Assert.Equal(GameResult.Backgammon, gameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostBackgammon, gameSession.Result.LoserResult);
            Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, session.Player2.Points);
			Assert.Equal(0, session.Player1.Points);
			var matchState = session.ToPayload(session.Player2.Id);
			Assert.NotNull(matchState.Player2);
			Assert.NotNull(matchState.GameRounds);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.Player2.Points);
			Assert.Equal(doubleCubeBoard.DoublingCubeValue * 3, matchState.GameRounds[0].Points);
		}

		[Fact]
		public void BackgammonPlayersCanOfferDoubleBeforeRolling()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			var player1GameState = session.GetGameState(session.Player1.Id);
			Assert.Contains(ServerCommands.OfferDoubleCommand, player1GameState.AllowedCommands);
			var player2GameState = session.GetGameState(session.Player2.Id);
			Assert.DoesNotContain(ServerCommands.OfferDoubleCommand, player2GameState.AllowedCommands);

			session.RollDices(session.Player1.Id);
			Assert.Equal(GamePhase.Rolling, gameSession.Phase);
			player1GameState = session.GetGameState(session.Player1.Id);
			Assert.DoesNotContain(ServerCommands.OfferDoubleCommand, player1GameState.AllowedCommands);
			player2GameState = session.GetGameState(session.Player2.Id);
			Assert.DoesNotContain(ServerCommands.OfferDoubleCommand, player2GameState.AllowedCommands);

			// execute a full turn
			var anyMoveSeq = gameSession.MoveSequences.FirstOrDefault();
			Assert.NotNull(anyMoveSeq);
			foreach (var anyMove in anyMoveSeq.Moves)
			{
				session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			}

			session.EndTurn(session.Player1.Id);
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			player1GameState = session.GetGameState(session.Player1.Id);
			Assert.DoesNotContain(ServerCommands.OfferDoubleCommand, player1GameState.AllowedCommands);
			player2GameState = session.GetGameState(session.Player2.Id);
			Assert.Contains(ServerCommands.OfferDoubleCommand, player2GameState.AllowedCommands);
		}

		[Fact]
		public void BackgammonPlayerCanOfferAndAcceptDoubles()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			var doublingCubeSession = session as IDoubleCubeMatchSession;
			Assert.NotNull(doublingCubeSession);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);
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
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			var doublingCubeSession = session as IDoubleCubeMatchSession;
			Assert.NotNull(doublingCubeSession);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);
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
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			var doublingCubeSession = session as IDoubleCubeMatchSession;
			Assert.NotNull(doublingCubeSession);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);
			// avoid rolling pasch
			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
			gameSession.InjectDiceServiceMock(mock.Object);
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			Assert.True(doublingCubeSession.CanOfferDouble(session.Player1.Id));
			session.RollDices(session.Player1.Id);
			Assert.False(doublingCubeSession.CanOfferDouble(session.Player1.Id));
			Assert.Equal(GamePhase.Rolling, gameSession.Phase);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player2.Id));
			var move = gameSession.MoveSequences.FirstOrDefault()?.Moves.FirstOrDefault();
			Assert.NotNull(move);
			session.MoveCheckers(session.Player1.Id, move.From, move.To);
			Assert.Equal(GamePhase.Moving, gameSession.Phase);
			Assert.False(doublingCubeSession.CanOfferDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player2.Id));
			move = gameSession.MoveSequences.FirstOrDefault()?.Moves.FirstOrDefault();
			Assert.NotNull(move);
			session.MoveCheckers(session.Player1.Id, move.From, move.To);
			Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
			Assert.False(doublingCubeSession.CanOfferDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player2.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player2.Id));
			session.EndTurn(session.Player1.Id);
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			Assert.True(doublingCubeSession.CanOfferDouble(session.Player2.Id));
			// player 2 offers first double
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.OfferDouble(session.Player1.Id));
			doublingCubeSession.OfferDouble(session.Player2.Id);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.AcceptDouble(session.Player2.Id));
			doublingCubeSession.AcceptDouble(session.Player1.Id);
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => doublingCubeSession.DeclineDouble(session.Player2.Id));
			// execute a full turn
			session.RollDices(session.Player2.Id);
			var anyMoveSeq = gameSession.MoveSequences.FirstOrDefault();
			Assert.NotNull(anyMoveSeq);
			foreach (var anyMove in anyMoveSeq.Moves)
			{
				session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
			}
			session.EndTurn(session.Player2.Id);
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			Assert.True(doublingCubeSession.CanOfferDouble(session.Player1.Id));
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
			Assert.Equal(2, session.Player1.Points);
			Assert.Equal(0, session.Player2.Points);
		}

		[Fact]
		public void BackgammonPlayerCanDoubleMultipleTimes()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Backgammon, _matchSessionFactory);
			Assert.NotNull(session);
			var doublingCubeSession = session as IDoubleCubeMatchSession;
			Assert.NotNull(doublingCubeSession);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);
			// avoid rolling pasch
			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
			gameSession.InjectDiceServiceMock(mock.Object);
			// player 1 turn
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			Assert.True(doublingCubeSession.CanOfferDouble(session.Player1.Id));
			// player 1 offers double
			doublingCubeSession.OfferDouble(session.Player1.Id);
			// player 2 accepts double
			doublingCubeSession.AcceptDouble(session.Player2.Id);
			// roll
			session.RollDices(session.Player1.Id);
			Assert.False(doublingCubeSession.CanOfferDouble(session.Player1.Id));
			Assert.Equal(GamePhase.Rolling, gameSession.Phase);
			// move
			var anyMoveSeq = gameSession.MoveSequences.FirstOrDefault();
			Assert.NotNull(anyMoveSeq);
			foreach (var anyMove in anyMoveSeq.Moves)
			{
				session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			}
			// end turn
			session.EndTurn(session.Player1.Id);
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			// player 2 turn
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			Assert.True(doublingCubeSession.CanOfferDouble(session.Player2.Id));
			// player 1 offers double
			doublingCubeSession.OfferDouble(session.Player2.Id);
			// player 2 accepts double
			doublingCubeSession.AcceptDouble(session.Player1.Id);
			// roll
			session.RollDices(session.Player2.Id);
			// move
			anyMoveSeq = gameSession.MoveSequences.FirstOrDefault();
			Assert.NotNull(anyMoveSeq);
			foreach (var anyMove in anyMoveSeq.Moves)
			{
				session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
			}
			// end turn
			session.EndTurn(session.Player2.Id);
			// player 1 turn
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			Assert.True(doublingCubeSession.CanOfferDouble(session.Player1.Id));
			// player 1 offers double
			doublingCubeSession.OfferDouble(session.Player1.Id);
			// player 2 accepts double
			doublingCubeSession.AcceptDouble(session.Player2.Id);

			Assert.Equal(8, doublingCubeSession.GetDoublingCubeValue());
		}
	}
}
