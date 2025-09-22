using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Bot;
using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;

using Moq;

using System.Security.Claims;

namespace GammonX.Server.Tests
{
	public class MatchHubTests
	{
		private readonly HttpClient _wildBgClient = new() { BaseAddress = new Uri("http://localhost:8082") };
		private readonly MatchSessionRepository _matchRepo;
		private readonly MatchLobbyHub _hub;
		private readonly SimpleMatchmakingService _matchmakingService;

		public MatchHubTests()
		{
			var diceFactory = new DiceServiceFactory();
			var gameSessionFactory = new GameSessionFactory(diceFactory);
			var matchSessionFactory = new MatchSessionFactory(gameSessionFactory);
			_matchmakingService = new SimpleMatchmakingService();
			_matchRepo = new MatchSessionRepository(matchSessionFactory);
			var botService = new WildbgBotService(_wildBgClient);
			_hub = new MatchLobbyHub(_matchmakingService, _matchRepo, diceFactory, botService);
		}

		// TODO :: resign game tests
		// TODO :: resign match tests
		// TODO :: doubling cube tests
		// TODO :: two player match tests

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchType.CashGame, Skip = "bot does not support tavli yet")]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchType.FivePointGame, Skip = "bot does not support tavli yet")]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchType.SevenPointGame, Skip = "bot does not support tavli yet")]
		public async Task MatchHubCanPlayPlayerVsBotMatch(WellKnownMatchVariant variant, WellKnownMatchType type)
		{
			var queueKey = new QueueKey(variant, WellKnownMatchModus.Bot, type);
			var playerId = Guid.NewGuid();
			var matchId = CreateTwoBotMatchLobby(queueKey, playerId);
			var matchIdStr = matchId.ToString();

			Assert.True(_matchmakingService.TryFindMatchLobby(matchId, out var lobby));
			Assert.NotNull(lobby);
			var groupName = lobby.GroupName;

			var mockClients = new Mock<IHubCallerClients>();
			var mockGroup = new Mock<IClientProxy>();
			_hub.Clients = mockClients.Object;
			_hub.Context = new HubCallerContextStub(Guid.NewGuid().ToString());
			_hub.Groups = new Mock<IGroupManager>().Object;
			mockClients.Setup(c => c.Group(groupName)).Returns(mockGroup.Object);
			mockClients.Setup(mc => mc.Client(It.IsAny<string>())).Returns(new Mock<ISingleClientProxy>().Object);
			mockClients.Setup(mc => mc.Caller).Returns(new Mock<ISingleClientProxy>().Object);

			await _hub.JoinMatchAsync(matchId.ToString(), playerId.ToString());

			var matchSession = _matchRepo.Get(matchId);
			Assert.NotNull(matchSession);

			var playerConnectionId = matchSession.Player1.ConnectionId;
			var botConnectionId = matchSession.Player2.ConnectionId;

			// bot lobby was found
			mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.MatchLobbyFoundEvent, It.IsAny<object[]>(), default), Times.Once);
			// player receives event with roll command
			mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.MatchStartedEvent, It.IsAny<object[]>(), default), Times.Once);
			// bot player does not receive start game event
			mockClients.Verify(c => c.Client(botConnectionId).SendCoreAsync(ServerEventTypes.MatchStartedEvent, It.IsAny<object[]>(), default), Times.Never);
			// bot lobby is always instantly ready
			mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.GameWaitingEvent, It.IsAny<object[]>(), default), Times.Never);

			await _hub.StartGameAsync(matchIdStr);

			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			Assert.NotNull(gameSession);

			do
			{
				if (gameSession.Phase == GamePhase.GameOver)
				{
					gameSession = matchSession.GetGameSession(matchSession.GameRound);
					Assert.NotNull(gameSession);
				}

				// bot offered a double
				if (matchSession is IDoubleCubeMatchSession cubeSession && cubeSession.IsDoubleOfferPending)
				{
					await _hub.AcceptDoubleAsync(matchIdStr);
				}

				if (gameSession.ActivePlayer == playerId)
				{
					await _hub.RollAsync(matchIdStr);

					var ms = gameSession.MoveSequences.FirstOrDefault();
					if (ms != null)
					{
						var moves = ms.Moves.ToList();
						foreach (var move in moves)
						{
							await _hub.MoveAsync(matchIdStr, move.From, move.To);
						}
					}

					if (gameSession.Phase != GamePhase.GameOver)
					{
						// end turn and bot executes its turn
						await _hub.EndTurnAsync(matchIdStr);
						if (gameSession.Phase == GamePhase.GameOver && matchSession.GameRound != matchSession.GetGameSessions().Length)
						{
							await _hub.StartGameAsync(matchIdStr);
						}
					}
					else
					{
						await _hub.StartGameAsync(matchIdStr);
					}
				}
			}
			while (!matchSession.IsMatchOver());

			Assert.True(matchSession.IsMatchOver());
			Assert.False(matchSession.CanStartNextGame());
			var playedSessions = matchSession.GetGameSessions().Where(gs => gs != null).ToList();
			if (type == WellKnownMatchType.CashGame)
			{
				Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) == 1);
			}
			else
			{
				Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) >= 2);
			}
			// bot will win every time
			Assert.True(matchSession.Player2.Points > 0);
			Assert.True(matchSession.Player2.Points >= type.GetMaxPoints());
			mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), default), Times.Exactly(playedSessions.Count - 1));
			mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), default), Times.Once);
			mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.ForceDisconnect, It.IsAny<object[]>(), default), Times.Once);
		}

		private Guid CreateTwoBotMatchLobby(QueueKey queueKey, Guid playerId)
		{
			var lobbyEntry = new LobbyEntry(playerId);
			var matchId = _matchmakingService.JoinQueue(lobbyEntry, queueKey);
			return matchId;
		}

		public class HubCallerContextStub : HubCallerContext
		{
			private readonly string _connectionId;

			public HubCallerContextStub(string connectionId)
			{
				_connectionId = connectionId;
			}

			public override string ConnectionId => _connectionId;

			public override string? UserIdentifier => default;

			public override ClaimsPrincipal? User => default;

			public override IDictionary<object, object?> Items => new Dictionary<object, object?>();

			public override IFeatureCollection Features => new FeatureCollection();

			public override CancellationToken ConnectionAborted => CancellationToken.None;

			public override void Abort()
			{
				// pass
			}
		}
	}
}
