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
		private static readonly IGameSessionFactory _gameSessionFactory = new GameSessionFactory();
		private static readonly IMatchSessionFactory _matchSessionFactory = new MatchSessionFactory(_gameSessionFactory);

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionCreated(WellKnownMatchVariant variant, GameModus gameModusToExpect)
		{
			var matchId = Guid.NewGuid();
			var session = _matchSessionFactory.Create(matchId, variant);
			Assert.NotNull(session);
			Assert.Equal(matchId, session.Id);
			Assert.Equal(variant, session.Variant);
			Assert.Equal(1, session.GameRound);
			Assert.Equal(Guid.Empty, session.Player1.Id);
			Assert.Equal(Guid.Empty, session.Player2.Id);
			Assert.True(string.IsNullOrEmpty(session.Player1.ConnectionId));
			Assert.True(string.IsNullOrEmpty(session.Player2.ConnectionId));
			Assert.Equal(0, session.Player1.Score);
			Assert.Equal(0, session.Player2.Score);
			Assert.Equal(gameModusToExpect, session.GetGameModus());
			Assert.False(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.CanEndTurn(Guid.Empty));
			Assert.Throws<InvalidOperationException>(() => session.GetGameState(Guid.Empty));
			Assert.IsType<MatchSession>(session);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionPayloadCreated(WellKnownMatchVariant variant)
		{
			var result = CreateMatchSession(variant);
			var session = result.Session;
			var matchId = result.MatchId;
			var payload = session.ToPayload();
			Assert.NotNull(payload);
			Assert.Equal(matchId, payload.Id);
			Assert.Equal(variant, payload.Variant);
			Assert.Equal(1, payload.GameRound);
			Assert.Equal(Guid.Empty, payload.Player1.Id);
			Assert.Equal(Guid.Empty, payload.Player2.Id);
			Assert.Equal(0, payload.Player1.Score);
			Assert.Equal(0, payload.Player2.Score);

		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionPlayersCanJoin(WellKnownMatchVariant variant)
		{
			var result = CreateMatchSession(variant);
			var session = result.Session as IMatchSessionModel;
			Assert.NotNull(session);

			var player1 = CreateLobbyEntry();
			var player2 = CreateLobbyEntry();
			var player3 = CreateLobbyEntry();
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
			var result = CreateMatchSession(variant);
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
			var result = CreateMatchSession(variant);
			var session = result.Session as IMatchSessionModel;
			Assert.NotNull(session);
			var entry = CreateLobbyEntry();
			session.JoinSession(entry);
			Assert.Throws<InvalidOperationException>(() => session.JoinSession(entry));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, GameModus.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli, GameModus.Portes)]
		[InlineData(WellKnownMatchVariant.Tavla, GameModus.Tavla)]
		public void MatchSessionCanStartMatch(WellKnownMatchVariant variant, GameModus modusToExpect)
		{
			var session = CreateMatchSessionWithPlayers(variant);
			Assert.NotNull(session);
			Assert.False(session.CanStartNextGame());
			session.Player1.AcceptNextGame();
			Assert.False(session.CanStartNextGame());
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();
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
			var session = CreateMatchSessionWithPlayers(variant);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();
			var player1Id = session.Player1.Id;
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			session.RollDices(player1Id);
			Assert.Equal(GamePhase.Rolling, gameSession.Phase);
			Assert.NotEmpty(gameSession.DiceRollsModel.DiceRolls);
			Assert.NotEmpty(gameSession.LegalMovesModel.LegalMoves);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionNotActivePlayerCannotRollDices(WellKnownMatchVariant variant)
		{
			var session = CreateMatchSessionWithPlayers(variant);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();
			var player2Id = session.Player2.Id;
			Assert.Throws<InvalidOperationException>(() => session.RollDices(player2Id));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCannotRollDicesIfNotStarted(WellKnownMatchVariant variant)
		{
			var session = CreateMatchSessionWithPlayers(variant);
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
			var session = CreateMatchSessionWithPlayers(variant);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			Assert.Throws<InvalidOperationException>(() => session.MoveCheckers(Guid.NewGuid(), 0 , 1));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCannotMoveIfNotActivePlayer(WellKnownMatchVariant variant)
		{
			var session = CreateMatchSessionWithPlayers(variant);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			session.StartNextGame();
			Assert.Throws<InvalidOperationException>(() => session.MoveCheckers(session.Player2.Id, 0, 1));
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCanMoveCheckers(WellKnownMatchVariant variant)
		{
			var session = CreateMatchSessionWithPlayers(variant);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();
			var mock = new Mock<IDiceService>();
			mock.Setup(x => x.Roll(2, 6)).Returns([1, 2]);
			gameSession.InjectDiceServiceMock(mock.Object);
			var player1Id = session.Player1.Id;
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			session.RollDices(player1Id);
			Assert.Equal(GamePhase.Rolling, gameSession.Phase);
			Assert.NotEmpty(gameSession.DiceRollsModel.DiceRolls);
			Assert.NotEmpty(gameSession.LegalMovesModel.LegalMoves);
			var firstLegalMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(firstLegalMove);
			session.MoveCheckers(player1Id, firstLegalMove.From, firstLegalMove.To);
			Assert.Equal(GamePhase.Moving, gameSession.Phase);
			var secondLegalMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(secondLegalMove);
			session.MoveCheckers(player1Id, secondLegalMove.From, secondLegalMove.To);
			Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCanEndTurn(WellKnownMatchVariant variant)
		{
			var session = CreateMatchSessionWithPlayers(variant);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			Assert.True(session.CanStartNextGame());
			var gameSession = session.StartNextGame();
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
			Assert.NotEmpty(gameSession.DiceRollsModel.DiceRolls);
			Assert.NotEmpty(gameSession.LegalMovesModel.LegalMoves);
			var firstLegalMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(firstLegalMove);
			session.MoveCheckers(player1Id, firstLegalMove.From, firstLegalMove.To);
			Assert.False(session.CanEndTurn(player1Id));
			Assert.False(session.CanEndTurn(session.Player2.Id));
			Assert.Equal(GamePhase.Moving, gameSession.Phase);
			var secondLegalMove = gameSession.LegalMovesModel.LegalMoves.FirstOrDefault();
			Assert.NotNull(secondLegalMove);
			session.MoveCheckers(player1Id, secondLegalMove.From, secondLegalMove.To);
			Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
			Assert.True(session.CanEndTurn(player1Id));
			Assert.False(session.CanEndTurn(player2Id));
			Assert.False(session.CanEndTurn(Guid.NewGuid()));
			session.EndTurn(player1Id);
			Assert.Equal(GamePhase.WaitingForRoll, gameSession.Phase);
			Assert.Equal(player2Id, gameSession.ActivePlayer);
			Assert.NotEqual(player1Id, gameSession.ActivePlayer);
			Assert.Equal(2, gameSession.TurnNumber);
			Assert.Empty(gameSession.DiceRollsModel.DiceRolls);
			Assert.Empty(gameSession.LegalMovesModel.LegalMoves);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon)]
		[InlineData(WellKnownMatchVariant.Tavli)]
		[InlineData(WellKnownMatchVariant.Tavla)]
		public void MatchSessionCannotEndTurnNotStartedMatch(WellKnownMatchVariant variant)
		{
			var session = CreateMatchSessionWithPlayers(variant);
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
			var session = CreateMatchSessionWithPlayers(variant);
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
			var session = CreateMatchSessionWithPlayers(variant);
			Assert.NotNull(session);
			session.Player1.AcceptNextGame();
			session.Player2.AcceptNextGame();
			session.StartNextGame();
			var allowedCommands = new string[] {"testCommand"};			
			var gameState = session.GetGameState(session.Player1.Id, allowedCommands);
			Assert.NotNull(gameState);
			Assert.Equal(allowedCommands, gameState.AllowedCommands);
			Assert.Equal(GamePhase.WaitingForRoll, gameState.Phase);
			gameState = session.GetGameState(session.Player2.Id, allowedCommands);
			Assert.NotNull(gameState);
			Assert.Empty(gameState.AllowedCommands);
			Assert.Equal(GamePhase.WaitingForOpponent, gameState.Phase);
		}

		private static IMatchSessionModel CreateMatchSessionWithPlayers(WellKnownMatchVariant variant)
		{
			var result = CreateMatchSession(variant);
			var session = result.Session as IMatchSessionModel;
			Assert.NotNull(session);
			var player1 = CreateLobbyEntry();
			var player2 = CreateLobbyEntry();
			session.JoinSession(player1);
			session.JoinSession(player2);
			return session;
		}

		private static dynamic CreateMatchSession(WellKnownMatchVariant variant)
		{
			var matchId = Guid.NewGuid();
			var session = _matchSessionFactory.Create(matchId, variant);
			return new { MatchId = matchId, Session = session };
		}

		private static LobbyEntry CreateLobbyEntry()
		{
			var entry = new LobbyEntry(Guid.NewGuid());
			entry.SetConnectionId(Guid.NewGuid().ToString());
			return entry;
		}
	}
}
