using GammonX.Engine.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;
using GammonX.Server.Extensions;
using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Utils;

using Moq;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests.Match
{
	public class MatchSessionTests
	{
		private static readonly IDiceServiceFactory _diceServiceFactory = new DiceServiceFactory();
		private static readonly IGameSessionFactory _gameSessionFactory = new GameSessionFactory(_diceServiceFactory);
		private static readonly MatchSessionFactory _matchSessionFactory = new(_gameSessionFactory);

		[Theory]
		[InlineData(MatchVariant.Backgammon, MatchType.CashGame)]
		[InlineData(MatchVariant.Backgammon, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Backgammon, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavli, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavli, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavli, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavla, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavla, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavla, MatchType.SevenPointGame)]
		public void MatchSessionClonePreservesUnstartedRounds(MatchVariant variant, MatchType matchType)
		{
			var queueKey = new QueueKey(variant, MatchModus.Bot, matchType, BotLevel.Expert);
			var session = _matchSessionFactory.Create(Guid.NewGuid(), queueKey);

			var clone = Assert.IsAssignableFrom<IMatchSessionModel>(Assert.IsAssignableFrom<ICloneable>(session).Clone());

			Assert.NotSame(session, clone);
			Assert.Equal(session.GetType(), clone.GetType());
			Assert.Equal(session.Id, clone.Id);
			Assert.Equal(session.Variant, clone.Variant);
			Assert.Equal(session.Modus, clone.Modus);
			Assert.Equal(session.Type, clone.Type);
			Assert.Equal(session.BotLevel, clone.BotLevel);
			Assert.Equal(session.GameRound, clone.GameRound);
			Assert.Equal(session.GetGameModus(), clone.GetGameModus());
			Assert.Null(clone.StartedAt);
			Assert.Null(clone.EndedAt);
			Assert.Equal(0, clone.Duration);
			Assert.NotSame(session.GetGameSessions(), clone.GetGameSessions());
			Assert.Equal(session.GetGameSessions().Length, clone.GetGameSessions().Length);
			Assert.All(clone.GetGameSessions(), game => Assert.Null(game));
			clone.JoinSession(SessionUtils.CreatePlayerConnection());
			Assert.NotEqual(Guid.Empty, clone.Player1.Id);
			Assert.Equal(Guid.Empty, session.Player1.Id);
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionClonePreservesCompletedState(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			session.StartMatch(session.Player1.Id);
			session.ResignMatch(session.Player2.Id);
			Assert.True(session.IsMatchOver());

			var clone = Assert.IsAssignableFrom<IMatchSessionModel>(Assert.IsAssignableFrom<ICloneable>(session).Clone());

			Assert.Equal(session.GetType(), clone.GetType());
			Assert.Equal(session.Id, clone.Id);
			Assert.Equal(session.GameRound, clone.GameRound);
			Assert.Equal(session.StartedAt, clone.StartedAt);
			Assert.Equal(session.EndedAt, clone.EndedAt);
			Assert.Equal(session.Duration, clone.Duration);
			Assert.True(clone.IsMatchOver());
			Assert.Equal(session.Player1.Points, clone.Player1.Points);
			Assert.Equal(session.Player2.Points, clone.Player2.Points);
			Assert.NotSame(session.GetGameSessions(), clone.GetGameSessions());
			Assert.Equal(session.GetGameSessions().Length, clone.GetGameSessions().Length);
			for (var round = 1; round <= session.GetGameSessions().Length; round++)
			{
				var originalGame = session.GetGameSession(round);
				var clonedGame = clone.GetGameSession(round);
				Assert.NotNull(originalGame);
				Assert.NotNull(clonedGame);
				Assert.NotSame(originalGame, clonedGame);
				Assert.Equal(originalGame.Id, clonedGame.Id);
				Assert.Equal(originalGame.Modus, clonedGame.Modus);
				Assert.Equal(originalGame.Phase, clonedGame.Phase);
				Assert.Equal(originalGame.Result, clonedGame.Result);
				Assert.Equal(originalGame.EndedAt, clonedGame.EndedAt);
			}
			var originalPayload = session.ToPayload(session.Player1.Id);
			var clonedPayload = clone.ToPayload(clone.Player1.Id);
			Assert.Equal(originalPayload.Winner, clonedPayload.Winner);
			Assert.Equal(originalPayload.Loser, clonedPayload.Loser);
			Assert.Equal(originalPayload.WinnerPoints, clonedPayload.WinnerPoints);
			Assert.Equal(originalPayload.LoserPoints, clonedPayload.LoserPoints);
			Assert.Empty(clonedPayload.AllowedCommands);
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionCloneCopiesPlayersIndependently(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			session.Player1.SetStartDiceRoll(6);
			session.Player2.SetStartDiceRoll(1);
			session.StartMatch(session.Player1.Id);
			session.ResignMatch(session.Player2.Id);

			var clone = Assert.IsAssignableFrom<IMatchSessionModel>(Assert.IsAssignableFrom<ICloneable>(session).Clone());

			Assert.NotSame(session.Player1, clone.Player1);
			Assert.NotSame(session.Player2, clone.Player2);
			Assert.Equal(session.Player1.Id, clone.Player1.Id);
			Assert.Equal(session.Player2.Id, clone.Player2.Id);
			Assert.Equal(session.Player1.ConnectionId, clone.Player1.ConnectionId);
			Assert.Equal(session.Player2.ConnectionId, clone.Player2.ConnectionId);
			Assert.Equal(session.Player1.LastSeenUtc, clone.Player1.LastSeenUtc);
			Assert.Equal(session.Player2.LastSeenUtc, clone.Player2.LastSeenUtc);
			Assert.Equal(session.Player1.Points, clone.Player1.Points);
			Assert.Equal(session.Player2.Points, clone.Player2.Points);
			Assert.Equal(session.Player1.StartDiceRoll, clone.Player1.StartDiceRoll);
			Assert.Equal(session.Player2.StartDiceRoll, clone.Player2.StartDiceRoll);
			Assert.Equal(session.Player1.NextGameAccepted, clone.Player1.NextGameAccepted);
			Assert.Equal(session.Player2.NextGameAccepted, clone.Player2.NextGameAccepted);
			clone.Player1.AcceptNextGame();
			clone.Player2.AcceptNextGame();
			clone.Player1.SetStartDiceRoll(2);
			clone.Player2.SetStartDiceRoll(5);
			Assert.False(session.Player1.NextGameAccepted);
			Assert.False(session.Player2.NextGameAccepted);
			Assert.Equal(6, session.Player1.StartDiceRoll);
			Assert.Equal(1, session.Player2.StartDiceRoll);
		}

		[Theory]
		[InlineData(false, null)]
		[InlineData(true, 6)]
		public void MatchPlayerCloneCopiesStateAndSharesLiveConnection(bool nextGameAccepted, int? startDiceRoll)
		{
			var connection = SessionUtils.CreatePlayerConnection();
			var player = new MatchPlayerModel(connection) { Points = 4 };
			player.SetStartDiceRoll(startDiceRoll);
			if (nextGameAccepted)
			{
				player.AcceptNextGame();
			}

			var clone = Assert.IsType<MatchPlayerModel>(((ICloneable)player).Clone());

			Assert.NotSame(player, clone);
			Assert.Equal(player.Id, clone.Id);
			Assert.Equal(player.ConnectionId, clone.ConnectionId);
			Assert.Equal(player.LastSeenUtc, clone.LastSeenUtc);
			Assert.Equal(player.Claimed, clone.Claimed);
			Assert.Equal(player.IsBot, clone.IsBot);
			Assert.Equal(player.Points, clone.Points);
			Assert.Equal(player.StartDiceRoll, clone.StartDiceRoll);
			Assert.Equal(player.NextGameAccepted, clone.NextGameAccepted);
			clone.Points = 5;
			clone.SetStartDiceRoll(2);
			clone.AcceptNextGame();
			Assert.Equal(4, player.Points);
			Assert.Equal(startDiceRoll, player.StartDiceRoll);
			Assert.Equal(nextGameAccepted, player.NextGameAccepted);
			clone.ActiveGameOver();
			player.AcceptNextGame();
			Assert.False(clone.NextGameAccepted);

			var lastSeen = connection.LastSeenUtc.AddSeconds(1);
			connection.SetConnectionId(Guid.Empty.ToString());
			connection.LastSeenUtc = lastSeen;
			Assert.Equal(Guid.Empty.ToString(), player.ConnectionId);
			Assert.Equal(player.ConnectionId, clone.ConnectionId);
			Assert.Equal(lastSeen, player.LastSeenUtc);
			Assert.Equal(lastSeen, clone.LastSeenUtc);
			Assert.True(player.IsBot);
			Assert.True(clone.IsBot);
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionCreated(MatchVariant variant, GameModus gameModusToExpect)
		{
			var matchId = Guid.NewGuid();
			var queueKey = new QueueKey(variant, MatchModus.Normal, MatchType.CashGame, BotLevel.Hard);
			var session = _matchSessionFactory.Create(matchId, queueKey);
			Assert.NotNull(session);
			Assert.Equal(matchId, session.Id);
			Assert.Equal(variant, session.Variant);
			Assert.Equal(1, session.GameRound);
			Assert.Equal(Guid.Empty, session.Player1.Id);
			Assert.Equal(Guid.Empty, session.Player2.Id);
			Assert.True(string.IsNullOrEmpty(session.Player1.ConnectionId));
			Assert.True(string.IsNullOrEmpty(session.Player2.ConnectionId));
			Assert.Equal(0, session.Player1.Points);
			Assert.Equal(0, session.Player2.Points);
			Assert.Equal(gameModusToExpect, session.GetGameModus());
			Assert.Equal(MatchModus.Normal, session.Modus);
			Assert.Equal(MatchType.CashGame, session.Type);
			Assert.False(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.CanEndTurn(Guid.Empty));
			Assert.Throws<InvalidOperationException>(() => session.GetGameState(Guid.Empty));
			Assert.IsAssignableFrom<MatchSession>(session);
			Assert.Null(session.StartedAt);
			Assert.Null(session.EndedAt);
			Assert.Equal(0, session.Duration);
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionPayloadCreated(MatchVariant variant)
		{
			var result = SessionUtils.CreateHeadToHeadMatchSession(variant, MatchType.CashGame, _matchSessionFactory);
			var session = result.Item2;
			var matchId = result.Item1;
			// player 1
			var payloadPlayer1 = session.ToPayload(session.Player1.Id);
			Assert.Equal(3, payloadPlayer1.AllowedCommands.Length);
			Assert.Contains(ServerCommands.ResignMatchCommand, payloadPlayer1.AllowedCommands);
			Assert.Contains(ServerCommands.ResignGameCommand, payloadPlayer1.AllowedCommands);
			Assert.Contains(ServerCommands.JoinMatchCommand, payloadPlayer1.AllowedCommands);
			Assert.NotNull(payloadPlayer1);
			Assert.Equal(matchId, payloadPlayer1.Id);
			Assert.Equal(variant, payloadPlayer1.Variant);
			Assert.Equal(MatchType.CashGame, payloadPlayer1.Type);
			Assert.Equal(MatchModus.Normal, payloadPlayer1.Modus);
			Assert.Equal(1, payloadPlayer1.GameRound);
			Assert.Equal(Guid.Empty, payloadPlayer1.Player1?.Id);
			Assert.Equal(Guid.Empty, payloadPlayer1.Player2?.Id);
			Assert.Equal(0, payloadPlayer1.Player1?.Points);
			Assert.Equal(0, payloadPlayer1.Player2?.Points);
			// player 2
			var payloadPlayer2 = session.ToPayload(session.Player2.Id);
			Assert.Equal(3, payloadPlayer2.AllowedCommands.Length);
			Assert.Contains(ServerCommands.ResignMatchCommand, payloadPlayer2.AllowedCommands);
			Assert.Contains(ServerCommands.ResignGameCommand, payloadPlayer2.AllowedCommands);
			Assert.Contains(ServerCommands.JoinMatchCommand, payloadPlayer2.AllowedCommands);
			Assert.NotNull(payloadPlayer2);
			Assert.Equal(matchId, payloadPlayer2.Id);
			Assert.Equal(variant, payloadPlayer2.Variant);
			Assert.Equal(MatchType.CashGame, payloadPlayer2.Type);
			Assert.Equal(MatchModus.Normal, payloadPlayer2.Modus);
			Assert.Equal(1, payloadPlayer2.GameRound);
			Assert.Equal(Guid.Empty, payloadPlayer2.Player1?.Id);
			Assert.Equal(Guid.Empty, payloadPlayer2.Player2?.Id);
			Assert.Equal(0, payloadPlayer2.Player1?.Points);
			Assert.Equal(0, payloadPlayer2.Player2?.Points);

		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionPlayersCanJoin(MatchVariant variant)
		{
			var result = SessionUtils.CreateHeadToHeadMatchSession(variant, MatchType.CashGame, _matchSessionFactory);
			var session = result.Item2;
			Assert.NotNull(session);

			var player1 = SessionUtils.CreatePlayerConnection();
			var player2 = SessionUtils.CreatePlayerConnection();
			var player3 = SessionUtils.CreatePlayerConnection();
			session.JoinSession(player1);
			Assert.Equal(player1.Id, session.Player1.Id);
			Assert.NotEqual(Guid.Empty, session.Player1.Id);
			session.JoinSession(player2);
			Assert.Equal(player2.Id, session.Player2.Id);
			Assert.NotEqual(Guid.Empty, session.Player2.Id);
			Assert.Throws<InvalidOperationException>(() => session.JoinSession(player3));
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionPlayersCannotJoin(MatchVariant variant)
		{
			var result = SessionUtils.CreateHeadToHeadMatchSession(variant, MatchType.CashGame, _matchSessionFactory);
			var session = result.Item2;
			Assert.NotNull(session);
			var connection = new PlayerConnection(Guid.NewGuid());
			Assert.Throws<ArgumentNullException>(() => session.JoinSession(connection));
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionPlayerCannotJoinTwice(MatchVariant variant)
		{
			var result = SessionUtils.CreateHeadToHeadMatchSession(variant, MatchType.CashGame, _matchSessionFactory);
			var session = result.Item2;
			Assert.NotNull(session);
			var entry = SessionUtils.CreatePlayerConnection();
			session.JoinSession(entry);
			Assert.Throws<InvalidOperationException>(() => session.JoinSession(entry));
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionCanStartMatch(MatchVariant variant, GameModus modusToExpect)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			Assert.False(session.CanStartNextGame());
			session.Player1.AcceptNextGame();
			Assert.False(session.CanStartNextGame());
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartMatch(session.Player1.Id);
			Assert.NotNull(gameSession);
			Assert.Equal(session.Id, gameSession.MatchId);
			Assert.Equal(1, session.GameRound);
			Assert.Equal(variant, session.Variant);
			Assert.Equal(modusToExpect, session.GetGameModus());
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionActivePlayerCanRollDices(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartMatch(session.Player1.Id);
			var player1Id = session.Player1.Id;
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			session.RollDices(player1Id);
			Assert.Equal(GamePhase.Rolling, gameSession.Phase);
			Assert.NotEmpty(gameSession.DiceRolls);
			Assert.NotEmpty(gameSession.MoveSequences);
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionNotActivePlayerCannotRollDices(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartMatch(session.Player1.Id);
            Assert.NotNull(gameSession);
			var player2Id = session.Player2.Id;
			Assert.Throws<InvalidOperationException>(() => session.RollDices(player2Id));
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionCannotRollDicesIfNotStarted(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.RollDices(Guid.NewGuid()));
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionCannotMoveIfNotStarted(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.MoveCheckers(Guid.NewGuid(), 0, 1));
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionCannotMoveIfNotActivePlayer(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			session.StartMatch(session.Player1.Id);
			Assert.Throws<InvalidOperationException>(() => session.MoveCheckers(session.Player2.Id, 0, 1));
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionCanMoveCheckers(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartMatch(session.Player1.Id);
			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([1, 2]);
			gameSession.InjectDiceServiceMock(mock.Object);
			var player1Id = session.Player1.Id;
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			session.RollDices(player1Id);
			Assert.Equal(GamePhase.Rolling, gameSession.Phase);
			Assert.NotEmpty(gameSession.DiceRolls);
			Assert.NotEmpty(gameSession.MoveSequences);
			var firstLegalMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).FirstOrDefault();
			Assert.NotNull(firstLegalMove);
			session.MoveCheckers(player1Id, firstLegalMove.From, firstLegalMove.To);
			Assert.Equal(GamePhase.Moving, gameSession.Phase);
			var secondLegalMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).FirstOrDefault();
			Assert.NotNull(secondLegalMove);
			session.MoveCheckers(player1Id, secondLegalMove.From, secondLegalMove.To);
			Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionCanUndoLastMoves(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			var gameSession = session.StartMatch(session.Player1.Id);
			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([1, 2]);
			gameSession.InjectDiceServiceMock(mock.Object);
			var player1Id = session.Player1.Id;
			var player2Id = session.Player2.Id;
			session.RollDices(player1Id);

			var firstLegalMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).First();
			session.MoveCheckers(player1Id, firstLegalMove.From, firstLegalMove.To);
			var secondLegalMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).First();
			session.MoveCheckers(player1Id, secondLegalMove.From, secondLegalMove.To);
			Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);

			Assert.True(session.CanUndoLastMove(player1Id));
			Assert.False(session.CanUndoLastMove(player2Id));
			session.UndoLastMove(player1Id);
			Assert.Throws<InvalidOperationException>(() => session.UndoLastMove(player2Id));
			Assert.Equal(GamePhase.Moving, gameSession.Phase);
			Assert.True(session.CanUndoLastMove(player1Id));
			Assert.False(session.CanUndoLastMove(player2Id));
			session.UndoLastMove(player1Id);
			Assert.Throws<InvalidOperationException>(() => session.UndoLastMove(player2Id));
			Assert.Equal(GamePhase.Moving, gameSession.Phase);
			Assert.False(session.CanUndoLastMove(player1Id));
			Assert.False(session.CanUndoLastMove(player2Id));
			Assert.Throws<InvalidOperationException>(() => session.UndoLastMove(player1Id));
			Assert.Throws<InvalidOperationException>(() => session.UndoLastMove(player2Id));
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionCanEndTurn(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartMatch(session.Player1.Id);
			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([1, 2]);
			gameSession.InjectDiceServiceMock(mock.Object);
			var player1Id = session.Player1.Id;
			var player2Id = session.Player2.Id;
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			session.RollDices(player1Id);
			Assert.False(session.CanEndTurn(player1Id));
			Assert.False(session.CanEndTurn(session.Player2.Id));
			Assert.Equal(GamePhase.Rolling, gameSession.Phase);
			Assert.NotEmpty(gameSession.DiceRolls);
			Assert.NotEmpty(gameSession.MoveSequences);
			var firstLegalMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).FirstOrDefault();
			Assert.NotNull(firstLegalMove);
			session.MoveCheckers(player1Id, firstLegalMove.From, firstLegalMove.To);
			Assert.False(session.CanEndTurn(player1Id));
			Assert.False(session.CanEndTurn(session.Player2.Id));
			Assert.Equal(GamePhase.Moving, gameSession.Phase);
			var secondLegalMove = gameSession.MoveSequences.SelectMany(ms => ms.Moves).FirstOrDefault();
			Assert.NotNull(secondLegalMove);
			session.MoveCheckers(player1Id, secondLegalMove.From, secondLegalMove.To);
			Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
			Assert.True(session.CanEndTurn(player1Id));
			Assert.False(session.CanEndTurn(player2Id));
			Assert.False(session.CanEndTurn(Guid.NewGuid()));
			session.EndTurn(player1Id);
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			Assert.Equal(player2Id, gameSession.ActivePlayer);
			Assert.Equal(player1Id, gameSession.OtherPlayer);
			Assert.NotEqual(player1Id, gameSession.ActivePlayer);
			Assert.NotEqual(player2Id, gameSession.OtherPlayer);
			Assert.Equal(2, gameSession.TurnNumber);
			Assert.Empty(gameSession.DiceRolls);
			Assert.Empty(gameSession.MoveSequences);
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionCannotEndTurnNotStartedMatch(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.EndTurn(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => session.EndTurn(session.Player2.Id));
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionCannotReturnProperGameState(MatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.GetGameState(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => session.GetGameState(session.Player2.Id));
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon)]
		public void BackgammonMatchSessionReturnsProperGameState(MatchVariant variant)
		{
			var allowedCommandsAfterRollActivePlayer = new[]
			{
				ServerCommands.RollCommand,
				ServerCommands.ResignGameCommand,
				ServerCommands.ResignMatchCommand,
				ServerCommands.OfferDoubleCommand
			};
			var allowedCommandsAfterRollOtherPlayer = new[] { ServerCommands.ResignGameCommand, ServerCommands.ResignMatchCommand };
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			session.StartMatch(session.Player1.Id);
			var gameState = session.GetGameState(session.Player1.Id);
			Assert.NotNull(gameState);
			Assert.Equal(allowedCommandsAfterRollActivePlayer, gameState.AllowedCommands);
			Assert.Equal(GamePhase.WaitingForRoll, gameState.Phase);
			gameState = session.GetGameState(session.Player2.Id);
			Assert.NotNull(gameState);
			Assert.Equal(allowedCommandsAfterRollOtherPlayer, gameState.AllowedCommands);
			Assert.Equal(GamePhase.WaitingForOpponent, gameState.Phase);
		}

		[Theory]
		[InlineData(MatchVariant.Tavli)]
		[InlineData(MatchVariant.Tavla)]
		public void MatchSessionReturnsProperGameState(MatchVariant variant)
		{
			var allowedCommandsAfterRollActivePlayer = new[] 
			{ 
				ServerCommands.RollCommand,
				ServerCommands.ResignGameCommand, 
				ServerCommands.ResignMatchCommand,
			};
			var allowedCommandsAfterRollOtherPlayer = new[] { ServerCommands.ResignGameCommand, ServerCommands.ResignMatchCommand };
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			session.StartMatch(session.Player1.Id);
			var gameState = session.GetGameState(session.Player1.Id);
			Assert.NotNull(gameState);
			Assert.Equal(allowedCommandsAfterRollActivePlayer, gameState.AllowedCommands);
			Assert.Equal(GamePhase.WaitingForRoll, gameState.Phase);
			gameState = session.GetGameState(session.Player2.Id);
			Assert.NotNull(gameState);
			Assert.Equal(allowedCommandsAfterRollOtherPlayer, gameState.AllowedCommands);
			Assert.Equal(GamePhase.WaitingForOpponent, gameState.Phase);
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionPlayer1CanResignGame(MatchVariant variant, GameModus modusToExpect)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			var gameSession = session.StartMatch(session.Player1.Id);
			Assert.Equal(modusToExpect, session.GetGameModus());
			session.ResignGame(session.Player1.Id);
			Assert.False(session.Player1.NextGameAccepted);
			Assert.False(session.Player2.NextGameAccepted);

			if (variant == MatchVariant.Backgammon)
			{
				Assert.True(session.Player1.Points == 0);
				Assert.True(session.Player2.Points > 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<InvalidOperationException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == MatchVariant.Tavla)
			{
				Assert.True(session.Player1.Points == 0);
				Assert.True(session.Player2.Points > 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<InvalidOperationException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == MatchVariant.Tavli)
			{
				Assert.True(session.Player1.Points == 0);
				Assert.True(session.Player2.Points > 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.True(session.CanStartNextGame());
				var newGameSession = session.StartNextGame(session.Player1.Id);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(GamePhase.WaitingForRoll, newGameSession.Phase);
				Assert.Equal(2, session.GameRound);
			}
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(MatchVariant.Tavli, GameModus.Portes)]
		[InlineData(MatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionPlayer2CanResignGame(MatchVariant variant, GameModus modusToExpect)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, MatchType.CashGame, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			var gameSession = session.StartMatch(session.Player1.Id);
			Assert.Equal(modusToExpect, session.GetGameModus());
			session.ResignGame(session.Player2.Id);
			Assert.False(session.Player1.NextGameAccepted);
			Assert.False(session.Player2.NextGameAccepted);

			if (variant == MatchVariant.Backgammon)
			{
				Assert.True(session.Player1.Points > 0);
				Assert.True(session.Player2.Points == 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<InvalidOperationException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == MatchVariant.Tavla)
			{
				Assert.True(session.Player1.Points > 0);
				Assert.True(session.Player2.Points == 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<InvalidOperationException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == MatchVariant.Tavli)
			{
				Assert.True(session.Player1.Points > 0);
				Assert.True(session.Player2.Points == 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.True(session.CanStartNextGame());
				var newGameSession = session.StartNextGame(session.Player1.Id);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(GamePhase.WaitingForRoll, newGameSession.Phase);
				Assert.Equal(2, session.GameRound);
			}
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon, MatchType.CashGame, GameModus.Backgammon)]
        [InlineData(MatchVariant.Backgammon, MatchType.FivePointGame, GameModus.Backgammon)]
        [InlineData(MatchVariant.Backgammon, MatchType.SevenPointGame, GameModus.Backgammon)]
        [InlineData(MatchVariant.Tavli, MatchType.CashGame, GameModus.Portes)]
        [InlineData(MatchVariant.Tavli, MatchType.FivePointGame, GameModus.Portes)]
        [InlineData(MatchVariant.Tavli, MatchType.SevenPointGame, GameModus.Portes)]
        [InlineData(MatchVariant.Tavla, MatchType.CashGame, GameModus.Tavla)]
        [InlineData(MatchVariant.Tavla, MatchType.FivePointGame, GameModus.Tavla)]
        [InlineData(MatchVariant.Tavla, MatchType.SevenPointGame, GameModus.Tavla)]
        public void MatchSessionPlayer1CanResignMatch(MatchVariant variant, MatchType matchType, GameModus modusToExpect)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, matchType, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			var gameSession = session.StartMatch(session.Player1.Id);
			Assert.Equal(modusToExpect, session.GetGameModus());
			session.ResignMatch(session.Player1.Id);
			Assert.False(session.Player1.NextGameAccepted);
			Assert.False(session.Player2.NextGameAccepted);

            if (variant == MatchVariant.Backgammon)
			{
				Assert.True(session.Player1.Points == 0);
				Assert.True(session.Player2.Points > session.Player1.Points);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<InvalidOperationException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == MatchVariant.Tavla)
			{
				Assert.True(session.Player1.Points == 0);
				Assert.True(session.Player2.Points > session.Player1.Points);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
                Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<InvalidOperationException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == MatchVariant.Tavli)
			{
				Assert.True(session.Player1.Points == 0);
				Assert.True(session.Player2.Points > session.Player1.Points);
				Assert.False(session.CanStartNextGame());
				Assert.Throws<InvalidOperationException>(() => session.StartNextGame(session.Player1.Id));
				var sessionInstance = session as MatchSession;
				Assert.NotNull(sessionInstance);
				Assert.Equal(GamePhase.GameOver, sessionInstance.GetGameSession(1)?.Phase);
                if (matchType == MatchType.CashGame)
				{
                    Assert.Equal(3, session.GameRound);
                    Assert.Equal(GamePhase.GameOver, sessionInstance.GetGameSession(2)?.Phase);
                    Assert.Equal(GamePhase.GameOver, sessionInstance.GetGameSession(3)?.Phase);
                }
				else
				{
                    Assert.Equal(1, session.GameRound);
                    Assert.Null(sessionInstance.GetGameSession(2)?.Phase);
                    Assert.Null(sessionInstance.GetGameSession(3)?.Phase);
                }
            }

            var payload = session.ToPayload(session.Player2.Id);
            Assert.NotNull(payload);
            Assert.Equal(session.Player2.Id, payload.Winner);
            Assert.Equal(session.Player1.Id, payload.Loser);
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon, MatchType.CashGame, GameModus.Backgammon)]
        [InlineData(MatchVariant.Backgammon, MatchType.FivePointGame, GameModus.Backgammon)]
        [InlineData(MatchVariant.Backgammon, MatchType.SevenPointGame, GameModus.Backgammon)]
        [InlineData(MatchVariant.Tavli, MatchType.CashGame, GameModus.Portes)]
        [InlineData(MatchVariant.Tavli, MatchType.FivePointGame, GameModus.Portes)]
        [InlineData(MatchVariant.Tavli, MatchType.SevenPointGame, GameModus.Portes)]
        [InlineData(MatchVariant.Tavla, MatchType.CashGame, GameModus.Tavla)]
        [InlineData(MatchVariant.Tavla, MatchType.FivePointGame, GameModus.Tavla)]
        [InlineData(MatchVariant.Tavla, MatchType.SevenPointGame, GameModus.Tavla)]
        public void MatchSessionPlayer2CanResignMatch(MatchVariant variant, MatchType matchType, GameModus modusToExpect)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, matchType, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			var gameSession = session.StartMatch(session.Player1.Id);
			Assert.Equal(modusToExpect, session.GetGameModus());
			session.ResignMatch(session.Player2.Id);
			Assert.False(session.Player1.NextGameAccepted);
			Assert.False(session.Player2.NextGameAccepted);

			if (variant == MatchVariant.Backgammon)
			{
				Assert.True(session.Player2.Points == 0);
				Assert.True(session.Player1.Points > session.Player2.Points);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<InvalidOperationException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == MatchVariant.Tavla)
			{
				Assert.True(session.Player2.Points == 0);
				Assert.True(session.Player1.Points > session.Player2.Points);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<InvalidOperationException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == MatchVariant.Tavli)
			{
				Assert.True(session.Player2.Points == 0);
				Assert.True(session.Player1.Points > session.Player2.Points);
				Assert.False(session.CanStartNextGame());
				Assert.Throws<InvalidOperationException>(() => session.StartNextGame(session.Player1.Id));
				var sessionInstance = session as MatchSession;
				Assert.NotNull(sessionInstance);
				Assert.Equal(GamePhase.GameOver, sessionInstance.GetGameSession(1)?.Phase);
                if (matchType == MatchType.CashGame)
                {
                    Assert.Equal(3, session.GameRound);
                    Assert.Equal(GamePhase.GameOver, sessionInstance.GetGameSession(2)?.Phase);
                    Assert.Equal(GamePhase.GameOver, sessionInstance.GetGameSession(3)?.Phase);
                }
                else
                {
                    Assert.Equal(1, session.GameRound);
                    Assert.Null(sessionInstance.GetGameSession(2)?.Phase);
                    Assert.Null(sessionInstance.GetGameSession(3)?.Phase);
                }
            }

			var payload = session.ToPayload(session.Player1.Id);
			Assert.NotNull(payload);
			Assert.Equal(session.Player1.Id, payload.Winner);
            Assert.Equal(session.Player2.Id, payload.Loser);
        }

		[Fact]
		public void ResignMatchAfterConcludedGamePreservesGameHistory()
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(
				MatchVariant.Tavli,
				MatchType.SevenPointGame,
				_matchSessionFactory);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			var gameSession = session.StartMatch(session.Player1.Id);

			session.ResignGame(session.Player2.Id);
			var gameResult = gameSession.Result;
			var gameEndedAt = gameSession.EndedAt;

			session.ResignMatch(session.Player1.Id);

			Assert.NotNull(session.EndedAt);
			Assert.Equal(gameResult, gameSession.Result);
			Assert.Equal(gameEndedAt, gameSession.EndedAt);

			var player1Record = session.ToRecord(session.Player1.Id);
			var player2Record = session.ToRecord(session.Player2.Id);
			var work = new MatchCompletedWorkContract { Records = [player1Record, player2Record] };

			var (winner, loser) = work.GetValidatedRecords();

			Assert.Equal(session.Player2.Id, winner.PlayerId);
			Assert.Equal(session.Player1.Id, loser.PlayerId);
			Assert.Equal(player1Record.MatchHistory, player2Record.MatchHistory);
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.CashGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.SevenPointGame)]
		public void MatchSessionHandleDifferentConfigurations(MatchVariant variant, MatchModus modus, MatchType type)
		{
			var matchSession = _matchSessionFactory.Create(Guid.NewGuid(), new QueueKey(variant, modus, type, BotLevel.Hard));
			Assert.NotNull(matchSession);
			Assert.Equal(variant, matchSession.Variant);
			Assert.Equal(modus, matchSession.Modus);
			Assert.Equal(type, matchSession.Type);

			var pointsToWin = matchSession.PointsAway(matchSession.Player1.Id);
			Assert.Equal(type.GetMaxPoints(), pointsToWin);

			pointsToWin = matchSession.PointsAway(matchSession.Player2.Id);
			Assert.Equal(type.GetMaxPoints(), pointsToWin);

			var gameOverFunc = type.GetMatchOverFunc();

			if (type == MatchType.CashGame)
			{
				var gameSessions = matchSession.GetGameSessions();
                Assert.NotNull(gameSessions);
				var connection1 = new PlayerConnection(Guid.NewGuid());
				connection1.SetConnectionId(Guid.NewGuid().ToString());
				matchSession.JoinSession(connection1);
				var connection2 = new PlayerConnection(Guid.NewGuid());
				connection2.SetConnectionId(Guid.NewGuid().ToString());
				matchSession.JoinSession(connection2);
				matchSession.Player1.AcceptNextGame();
				matchSession.Player2.AcceptNextGame();
				var startPlayerId = matchSession.Player1.Id;

                matchSession.StartMatch(startPlayerId);
                matchSession.ResignGame(startPlayerId);
                startPlayerId = startPlayerId == matchSession.Player1.Id ? matchSession.Player2.Id : matchSession.Player1.Id;
                matchSession.Player1.AcceptNextGame();
                matchSession.Player2.AcceptNextGame();

                while (matchSession.CanStartNextGame())
				{
					matchSession.StartNextGame(startPlayerId);
					matchSession.ResignGame(startPlayerId);
					startPlayerId = startPlayerId == matchSession.Player1.Id ? matchSession.Player2.Id : matchSession.Player1.Id;
					matchSession.Player1.AcceptNextGame();
					matchSession.Player2.AcceptNextGame();
				}
				Assert.True(gameOverFunc.Invoke(matchSession));
			}
			else if (type == MatchType.FivePointGame)
			{
				matchSession.Player1.Points = 4;
				Assert.False(gameOverFunc.Invoke(matchSession));
				matchSession.Player2.Points = 5;
				Assert.True(gameOverFunc.Invoke(matchSession));
			}
			else if (type == MatchType.SevenPointGame)
			{
				matchSession.Player2.Points = 4;
				Assert.False(gameOverFunc.Invoke(matchSession));
				matchSession.Player1.Points = 8;
				Assert.True(gameOverFunc.Invoke(matchSession));
			}
		}

		[Theory]
		[InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.CashGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.SevenPointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.FivePointGame)]
		[InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.SevenPointGame)]
		public void MatchSessionReturnsProperHistory(MatchVariant variant, MatchModus modus, MatchType type)
		{
			var matchSession = _matchSessionFactory.Create(Guid.NewGuid(), new QueueKey(variant, modus, type, BotLevel.Hard));
			Assert.NotNull(matchSession);

			var player1Id = Guid.NewGuid();
			var connection1 = new PlayerConnection(player1Id);
			connection1.SetConnectionId("none1");
			var player2Id = Guid.NewGuid();
			var connection2 = new PlayerConnection(player2Id);
			connection2.SetConnectionId("none2");

			matchSession.JoinSession(connection1);
			matchSession.JoinSession(connection2);

			matchSession.Player1.AcceptNextGame();
			matchSession.Player2.AcceptNextGame();
			matchSession.StartMatch(player1Id);

			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			Assert.NotNull(gameSession);

			do
			{
				if (gameSession.Phase == GamePhase.GameOver)
				{
					Assert.NotNull(matchSession);
					gameSession = matchSession.GetGameSession(matchSession.GameRound);
					Assert.NotNull(gameSession);
				}

				if (gameSession.ActivePlayer == player1Id)
				{
					ExecutePlayerTurn(matchSession, gameSession);
				}
				else if (gameSession.ActivePlayer == player2Id)
				{
					ExecutePlayerTurn(matchSession, gameSession);
				}
			}
			while (!matchSession.IsMatchOver());

			Assert.True(matchSession.IsMatchOver());
			var matchHistory = matchSession.GetHistory();
			Assert.NotNull(matchHistory);
			foreach (var gameHistory in matchHistory.Games)
			{
				var gameHistoryStr = gameHistory.ToString();
				Assert.NotNull(gameHistory);
				Assert.NotNull(gameHistoryStr);
			}
			var historyStr = matchHistory.ToString();
			Assert.NotNull(historyStr);
		}

		private static void ExecutePlayerTurn(IMatchSessionModel matchSession, IGameSessionModel gameSession)
		{
			matchSession.RollDices(gameSession.ActivePlayer);
			var ms = gameSession.MoveSequences.FirstOrDefault();
			if (ms != null)
			{
				var moves = ms.Moves.ToList();
				foreach (var move in moves)
				{
					var gameOver = matchSession.MoveCheckers(gameSession.ActivePlayer, move.From, move.To);
					if (gameOver)
						break;
				}
			}

			if (gameSession.Phase != GamePhase.GameOver)
			{
				matchSession.EndTurn(gameSession.ActivePlayer);
				if (gameSession.Phase == GamePhase.GameOver && matchSession.GameRound != matchSession.GetGameSessions().Length)
				{
					matchSession.Player1.AcceptNextGame();
					matchSession.Player2.AcceptNextGame();
					matchSession.StartNextGame(gameSession.ActivePlayer);
				}
			}
			else if (!matchSession.IsMatchOver())
			{
				matchSession.Player1.AcceptNextGame();
				matchSession.Player2.AcceptNextGame();
				if (matchSession.CanStartNextGame())
				{
                    matchSession.StartNextGame(gameSession.ActivePlayer);
                }
				else
				{
					throw new InvalidOperationException("Cannot start next game although match is not over.");
                }
			}
		}
	}
}
