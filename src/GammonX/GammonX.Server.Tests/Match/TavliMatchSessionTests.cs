using GammonX.Engine.Services;

using GammonX.Models.Enums;
using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Utils;

using Moq;

namespace GammonX.Server.Tests.Match
{
	public class TavliMatchSessionTests
	{
		private static readonly IDiceServiceFactory _diceServiceFactory = new DiceServiceFactory();
		private static readonly IGameSessionFactory _gameSessionFactory = new GameSessionFactory(_diceServiceFactory);
		private static readonly IMatchSessionFactory _matchSessionFactory = new MatchSessionFactory(_gameSessionFactory);

		[Fact]
		public void TavliSingleGameScoreIsCalculatedWhite()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Tavli, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartMatch(session.Player1.Id);

			var board = gameSession.BoardModel;
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[23] = -1;
			singleGameWinBoard[22] = 14;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(true, 14);
			board.BearOffChecker(false, 1);

			session.RollDices(session.Player1.Id);
			var anyMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(1, session.Player1.Points);
			Assert.Equal(0, session.Player2.Points);

			var matchState = session.ToPayload(session.Player1.Id);
			Assert.Equal(3, matchState.AllowedCommands.Length);
			Assert.Contains(ServerCommands.StartGameCommand, matchState.AllowedCommands);
			Assert.Contains(ServerCommands.ResignGameCommand, matchState.AllowedCommands);
			Assert.Contains(ServerCommands.ResignMatchCommand, matchState.AllowedCommands);
			Assert.Equal(1, matchState.GameRound);
			Assert.NotNull(matchState.Player1);
			Assert.NotNull(matchState.Player2);
			Assert.Equal(1, matchState.Player1.Points);
			Assert.Equal(0, matchState.Player2.Points);
			Assert.Equal(session.Id, matchState.Id);
			Assert.Equal(MatchVariant.Tavli, matchState.Variant);
			Assert.Equal(session.Player1.Id, matchState.Player1.Id);
			Assert.Equal(session.Player2.Id, matchState.Player2.Id);
			Assert.NotNull(matchState.GameRounds);
			Assert.Single(matchState.GameRounds);
			Assert.Equal(GameModus.Portes, matchState.GameRounds[0].Modus);
			Assert.Equal(GamePhase.GameOver, matchState.GameRounds[0].Phase);
            Assert.Equal(session.Player1.Id, gameSession.Result.WinnerId);
            Assert.Equal(1, gameSession.Result.Points);
            Assert.Equal(GameResult.Single, gameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostSingle, gameSession.Result.LoserResult);
            Assert.Equal(1, matchState.GameRounds[0].Points);
			Assert.Equal(0, matchState.GameRounds[0].GameRoundIndex);
			Assert.Equal(session.Player1.Id, matchState.GameRounds[0].Winner);
			// match not over yet
			Assert.Null(matchState.Winner);
			Assert.Null(matchState.WinnerPoints);
			Assert.Null(matchState.Loser);
			Assert.Null(matchState.LoserPoints);
		}

		[Fact]
		public void TavliSingleGameScoreIsCalculatedBlack()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Tavli, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartMatch(session.Player1.Id);

			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
			gameSession.InjectDiceServiceMock(mock.Object);

			var board = gameSession.BoardModel;
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[5] = -14;
			singleGameWinBoard[0] = 1;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(false, 14);
			board.BearOffChecker(true, 1);

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
			var anyMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
			Assert.Equal(1, session.Player2.Points);
			Assert.Equal(0, session.Player1.Points);
			var matchState = session.ToPayload(session.Player2.Id);
			Assert.NotNull(matchState.Player2);
			Assert.Equal(1, matchState.Player2.Points);
			Assert.NotNull(matchState.GameRounds);
			Assert.Equal(1, matchState.GameRounds[0].Points);
            Assert.Equal(session.Player2.Id, gameSession.Result.WinnerId);
            Assert.Equal(1, gameSession.Result.Points);
            Assert.Equal(GameResult.Single, gameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostSingle, gameSession.Result.LoserResult);
            // match not over yet
            Assert.Null(matchState.Winner);
			Assert.Null(matchState.WinnerPoints);
			Assert.Null(matchState.Loser);
			Assert.Null(matchState.LoserPoints);
		}

		[Fact]
		public void TavliGammonGameScoreIsCalculatedWhite()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Tavli, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartMatch(session.Player1.Id);

			var board = gameSession.BoardModel;
			var singleGameWinBoard = new int[24];
			singleGameWinBoard[23] = -1;
			singleGameWinBoard[0] = 14;
			board.SetFields(singleGameWinBoard);
			board.BearOffChecker(true, 14);
			// no borne off for black
			session.RollDices(session.Player1.Id);
			var anyMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player1.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.Equal(session.Player1.Id, gameSession.Result.WinnerId);
            Assert.Equal(2, gameSession.Result.Points);
            Assert.Equal(GameResult.Gammon, gameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostGammon, gameSession.Result.LoserResult);
            Assert.Equal(2, session.Player1.Points);
			Assert.Equal(0, session.Player2.Points);
			var matchState = session.ToPayload(session.Player1.Id);
			Assert.NotNull(matchState.Player1);
			Assert.Equal(2, matchState.Player1.Points);
			Assert.NotNull(matchState.GameRounds);
			Assert.Equal(2, matchState.GameRounds[0].Points);
		}

		[Fact]
		public void TavliGammonGameScoreIsCalculatedBlack()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(MatchVariant.Tavli, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartMatch(session.Player1.Id);

			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([2, 3]);
			gameSession.InjectDiceServiceMock(mock.Object);

			var board = gameSession.BoardModel;
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
			var anyMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).FirstOrDefault();
			Assert.NotNull(anyMove);
			session.MoveCheckers(session.Player2.Id, anyMove.From, anyMove.To);
			Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.Equal(session.Player2.Id, gameSession.Result.WinnerId);
            Assert.Equal(2, gameSession.Result.Points);
            Assert.Equal(GameResult.Gammon, gameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostGammon, gameSession.Result.LoserResult);
            Assert.Equal(2, session.Player2.Points);
			Assert.Equal(0, session.Player1.Points);
			var matchState = session.ToPayload(session.Player2.Id);
			Assert.NotNull(matchState.Player2);
			Assert.Equal(2, matchState.Player2.Points);
			Assert.NotNull(matchState.GameRounds);
			Assert.Equal(2, matchState.GameRounds[0].Points);
		}
	}
}
