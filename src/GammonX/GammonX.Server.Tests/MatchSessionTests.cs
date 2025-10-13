using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Models;
using GammonX.Server.Services;

using GammonX.Server.Tests.Utils;

using Moq;

namespace GammonX.Server.Tests
{
	public class MatchSessionTests
	{
		private static readonly IDiceServiceFactory _diceServiceFactory = new DiceServiceFactory();
		private static readonly IGameSessionFactory _gameSessionFactory = new GameSessionFactory(_diceServiceFactory);
		private static readonly IMatchSessionFactory _matchSessionFactory = new MatchSessionFactory(_gameSessionFactory);

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionCreated(WellKnownMatchVariant variant, GameModus gameModusToExpect)
		{
			var matchId = Guid.NewGuid();
			var queueKey = new QueueKey(variant, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame);
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
			Assert.Equal(WellKnownMatchModus.Normal, session.Modus);
			Assert.Equal(WellKnownMatchType.CashGame, session.Type);
			Assert.False(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.CanEndTurn(Guid.Empty));
			Assert.Throws<InvalidOperationException>(() => session.GetGameState(Guid.Empty));
			Assert.IsAssignableFrom<MatchSession>(session);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionPayloadCreated(WellKnownMatchVariant variant)
		{
			var result = SessionUtils.CreateHeadToHeadMatchSession(variant, _matchSessionFactory);
			var session = result.Session;
			var matchId = result.MatchId;
			var payload = session.ToPayload();
			Assert.NotNull(payload);
			Assert.Equal(matchId, payload.Id);
			Assert.Equal(variant, payload.Variant);
			Assert.Equal(WellKnownMatchType.CashGame, payload.Type);
			Assert.Equal(WellKnownMatchModus.Normal, payload.Modus);
			Assert.Equal(1, payload.GameRound);
			Assert.Equal(Guid.Empty, payload.Player1.Id);
			Assert.Equal(Guid.Empty, payload.Player2.Id);
			Assert.Equal(0, payload.Player1.Points);
			Assert.Equal(0, payload.Player2.Points);

		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionPlayersCanJoin(WellKnownMatchVariant variant)
		{
			var result = SessionUtils.CreateHeadToHeadMatchSession(variant, _matchSessionFactory);
			var session = result.Session as IMatchSessionModel;
			Assert.NotNull(session);

			var player1 = SessionUtils.CreateLobbyEntry();
			var player2 = SessionUtils.CreateLobbyEntry();
			var player3 = SessionUtils.CreateLobbyEntry();
			session.JoinSession(player1);
			Assert.Equal(player1.PlayerId, session.Player1.Id);
			Assert.NotEqual(Guid.Empty, session.Player1.Id);
			session.JoinSession(player2);
			Assert.Equal(player2.PlayerId, session.Player2.Id);
			Assert.NotEqual(Guid.Empty, session.Player2.Id);
			Assert.Throws<InvalidOperationException>(() => session.JoinSession(player3));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionPlayersCannotJoin(WellKnownMatchVariant variant)
		{
			var result = SessionUtils.CreateHeadToHeadMatchSession(variant, _matchSessionFactory);
			var session = result.Session as IMatchSessionModel;
			Assert.NotNull(session);
			var entry = new LobbyEntry(Guid.NewGuid());
			Assert.Throws<ArgumentNullException>(() => session.JoinSession(entry));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionPlayerCannotJoinTwice(WellKnownMatchVariant variant)
		{
			var result = SessionUtils.CreateHeadToHeadMatchSession(variant, _matchSessionFactory);
			var session = result.Session as IMatchSessionModel;
			Assert.NotNull(session);
			var entry = SessionUtils.CreateLobbyEntry();
			session.JoinSession(entry);
			Assert.Throws<InvalidOperationException>(() => session.JoinSession(entry));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionCanStartMatch(WellKnownMatchVariant variant, GameModus modusToExpect)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			Assert.False(session.CanStartNextGame());
			session.Player1.AcceptNextGame();
			Assert.False(session.CanStartNextGame());
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);
			Assert.NotNull(gameSession);
			Assert.Equal(session.Id, gameSession.MatchId);
			Assert.Equal(1, session.GameRound);
			Assert.Equal(variant, session.Variant);
			Assert.Equal(modusToExpect, session.GetGameModus());
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionActivePlayerCanRollDices(WellKnownMatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);
			var player1Id = session.Player1.Id;
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			session.RollDices(player1Id);
			Assert.Equal(GamePhase.Rolling, gameSession.Phase);
			Assert.NotEmpty(gameSession.DiceRolls);
			Assert.NotEmpty(gameSession.MoveSequences);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionNotActivePlayerCannotRollDices(WellKnownMatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);
			var player2Id = session.Player2.Id;
			Assert.Throws<InvalidOperationException>(() => session.RollDices(player2Id));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCannotRollDicesIfNotStarted(WellKnownMatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.RollDices(Guid.NewGuid()));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCannotMoveIfNotStarted(WellKnownMatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.MoveCheckers(Guid.NewGuid(), 0, 1));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCannotMoveIfNotActivePlayer(WellKnownMatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			session.StartNextGame(session.Player1.Id);
			Assert.Throws<InvalidOperationException>(() => session.MoveCheckers(session.Player2.Id, 0, 1));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCanMoveCheckers(WellKnownMatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);
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
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCanUndoLastMoves(WellKnownMatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			var gameSession = session.StartNextGame(session.Player1.Id);
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
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCanEndTurn(WellKnownMatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame(session.Player1.Id);
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
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCannotEndTurnNotStartedMatch(WellKnownMatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.EndTurn(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => session.EndTurn(session.Player2.Id));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCannotReturnProperGameState(WellKnownMatchVariant variant)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.GetGameState(session.Player1.Id));
			Assert.Throws<InvalidOperationException>(() => session.GetGameState(session.Player2.Id));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionReturnsProperGameState(WellKnownMatchVariant variant)
		{
			var defaultAllowedCommands = new[] { ServerCommands.ResignGameCommand, ServerCommands.ResignMatchCommand };
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			session.StartNextGame(session.Player1.Id);
			var allowedCommands = new string[] { "testCommand" };
			allowedCommands = allowedCommands.Union(defaultAllowedCommands).ToArray();
			var gameState = session.GetGameState(session.Player1.Id, allowedCommands);
			Assert.NotNull(gameState);
			Assert.Equal(allowedCommands, gameState.AllowedCommands);
			Assert.Equal(GamePhase.WaitingForRoll, gameState.Phase);
			gameState = session.GetGameState(session.Player2.Id, allowedCommands);
			Assert.NotNull(gameState);
			Assert.Equal(defaultAllowedCommands, gameState.AllowedCommands);
			Assert.Equal(GamePhase.WaitingForOpponent, gameState.Phase);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionPlayer1CanResignGame(WellKnownMatchVariant variant, GameModus modusToExpect)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			var gameSession = session.StartNextGame(session.Player1.Id);
			Assert.Equal(modusToExpect, session.GetGameModus());
			session.ResignGame(session.Player1.Id);
			Assert.False(session.Player1.NextGameAccepted);
			Assert.False(session.Player2.NextGameAccepted);

			if (variant == WellKnownMatchVariant.Backgammon)
			{
				Assert.True(session.Player1.Points == 0);
				Assert.True(session.Player2.Points > 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<IndexOutOfRangeException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == WellKnownMatchVariant.Tavla)
			{
				Assert.True(session.Player1.Points == 0);
				Assert.True(session.Player2.Points > 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<IndexOutOfRangeException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == WellKnownMatchVariant.Tavli)
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
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionPlayer2CanResignGame(WellKnownMatchVariant variant, GameModus modusToExpect)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			var gameSession = session.StartNextGame(session.Player1.Id);
			Assert.Equal(modusToExpect, session.GetGameModus());
			session.ResignGame(session.Player2.Id);
			Assert.False(session.Player1.NextGameAccepted);
			Assert.False(session.Player2.NextGameAccepted);

			if (variant == WellKnownMatchVariant.Backgammon)
			{
				Assert.True(session.Player1.Points > 0);
				Assert.True(session.Player2.Points == 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<IndexOutOfRangeException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == WellKnownMatchVariant.Tavla)
			{
				Assert.True(session.Player1.Points > 0);
				Assert.True(session.Player2.Points == 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<IndexOutOfRangeException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == WellKnownMatchVariant.Tavli)
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
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionPlayer1CanResignMatch(WellKnownMatchVariant variant, GameModus modusToExpect)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			var gameSession = session.StartNextGame(session.Player1.Id);
			Assert.Equal(modusToExpect, session.GetGameModus());
			session.ResignMatch(session.Player1.Id);
			Assert.False(session.Player1.NextGameAccepted);
			Assert.False(session.Player2.NextGameAccepted);

			if (variant == WellKnownMatchVariant.Backgammon)
			{
				Assert.True(session.Player1.Points == 0);
				Assert.True(session.Player2.Points > 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<IndexOutOfRangeException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == WellKnownMatchVariant.Tavla)
			{
				Assert.True(session.Player1.Points == 0);
				Assert.True(session.Player2.Points > 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<IndexOutOfRangeException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == WellKnownMatchVariant.Tavli)
			{
				Assert.True(session.Player1.Points == 0);
				Assert.True(session.Player2.Points > 0);
				Assert.Equal(3, session.GameRound);
				Assert.False(session.CanStartNextGame());
				Assert.Throws<IndexOutOfRangeException>(() => session.StartNextGame(session.Player1.Id));
				var sessionInstace = session as MatchSession;
				Assert.NotNull(sessionInstace);
				Assert.Equal(GamePhase.GameOver, sessionInstace.GetGameSession(1)?.Phase);
				Assert.Equal(GamePhase.GameOver, sessionInstace.GetGameSession(2)?.Phase);
				Assert.Equal(GamePhase.GameOver, sessionInstace.GetGameSession(3)?.Phase);
			}
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionPlayer2CanResignMatch(WellKnownMatchVariant variant, GameModus modusToExpect)
		{
			var session = SessionUtils.CreateMatchSessionWithPlayers(variant, _matchSessionFactory);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			var gameSession = session.StartNextGame(session.Player1.Id);
			Assert.Equal(modusToExpect, session.GetGameModus());
			session.ResignMatch(session.Player2.Id);
			Assert.False(session.Player1.NextGameAccepted);
			Assert.False(session.Player2.NextGameAccepted);

			if (variant == WellKnownMatchVariant.Backgammon)
			{
				Assert.True(session.Player2.Points == 0);
				Assert.True(session.Player1.Points > 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<IndexOutOfRangeException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == WellKnownMatchVariant.Tavla)
			{
				Assert.True(session.Player2.Points == 0);
				Assert.True(session.Player1.Points > 0);
				Assert.Equal(GamePhase.GameOver, gameSession.Phase);
				Assert.Equal(1, session.GameRound);
				Assert.False(session.CanStartNextGame());
				session.Player1.AcceptNextGame();
				session.Player2.AcceptNextGame();
				Assert.Throws<IndexOutOfRangeException>(() => session.StartNextGame(session.Player1.Id));
			}
			else if (variant == WellKnownMatchVariant.Tavli)
			{
				Assert.True(session.Player2.Points == 0);
				Assert.True(session.Player1.Points > 0);
				Assert.Equal(3, session.GameRound);
				Assert.False(session.CanStartNextGame());
				Assert.Throws<IndexOutOfRangeException>(() => session.StartNextGame(session.Player1.Id));
				var sessionInstace = session as MatchSession;
				Assert.NotNull(sessionInstace);
				Assert.Equal(GamePhase.GameOver, sessionInstace.GetGameSession(1)?.Phase);
				Assert.Equal(GamePhase.GameOver, sessionInstace.GetGameSession(2)?.Phase);
				Assert.Equal(GamePhase.GameOver, sessionInstace.GetGameSession(3)?.Phase);
			}
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Bot, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Bot, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Bot, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Bot, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Bot, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Bot, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Bot, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Bot, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Bot, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		public void MatchSessionHandleDifferentConfigurations(WellKnownMatchVariant variant, WellKnownMatchModus modus, WellKnownMatchType type)
		{
			var matchSession = _matchSessionFactory.Create(Guid.NewGuid(), new QueueKey(variant, modus, type));
			Assert.NotNull(matchSession);
			Assert.Equal(variant, matchSession.Variant);
			Assert.Equal(modus, matchSession.Modus);
			Assert.Equal(type, matchSession.Type);

			var pointsToWin = matchSession.PointsAway(matchSession.Player1.Id);
			Assert.Equal(type.GetMaxPoints(), pointsToWin);

			pointsToWin = matchSession.PointsAway(matchSession.Player2.Id);
			Assert.Equal(type.GetMaxPoints(), pointsToWin);

			var gameOverFunc = type.GetMatchOverFunc();

			if (type == WellKnownMatchType.CashGame)
			{
				var gameSessions = matchSession.GetGameSessions();
				var startPlayerId = matchSession.Player1.Id;
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
			else if (type == WellKnownMatchType.FivePointGame)
			{
				matchSession.Player1.Points = 4;
				Assert.False(gameOverFunc.Invoke(matchSession));
				matchSession.Player2.Points = 5;
				Assert.True(gameOverFunc.Invoke(matchSession));
			}
			else if (type == WellKnownMatchType.SevenPointGame)
			{
				matchSession.Player2.Points = 4;
				Assert.False(gameOverFunc.Invoke(matchSession));
				matchSession.Player1.Points = 8;
				Assert.True(gameOverFunc.Invoke(matchSession));
			}
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Bot, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Bot, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Bot, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Bot, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Bot, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Bot, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Bot, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Bot, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Bot, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		public void MatchSessionReturnsProperHistory(WellKnownMatchVariant variant, WellKnownMatchModus modus, WellKnownMatchType type)
		{
			var matchSession = _matchSessionFactory.Create(Guid.NewGuid(), new QueueKey(variant, modus, type));
			Assert.NotNull(matchSession);

			var player1Id = Guid.NewGuid();
			var lobbyEntry1 = new LobbyEntry(player1Id);
			lobbyEntry1.SetConnectionId("none1");
			var player2Id = Guid.NewGuid();
			var lobbyEntry2 = new LobbyEntry(player2Id);
			lobbyEntry2.SetConnectionId("none2");

			matchSession.JoinSession(lobbyEntry1);
			matchSession.JoinSession(lobbyEntry2);

			matchSession.Player1.AcceptNextGame();
			matchSession.Player2.AcceptNextGame();
			matchSession.StartNextGame(player1Id);

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
			var historyStr = matchHistory.ToString();
			Assert.NotNull(historyStr);
		}

		private static void ExecutePlayerTurn(IMatchSessionModel matchSession, IGameSessionModel gameSession)
		{
			var matchIdStr = matchSession.Id.ToString();

			matchSession.RollDices(gameSession.ActivePlayer);

			var ms = gameSession.MoveSequences.FirstOrDefault();
			if (ms != null)
			{
				var moves = ms.Moves.ToList();
				foreach (var move in moves)
				{
					matchSession.MoveCheckers(gameSession.ActivePlayer, move.From, move.To);
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
				matchSession.StartNextGame(gameSession.ActivePlayer);
			}
		}
	}
}
