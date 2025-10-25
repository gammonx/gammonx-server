using GammonX.Engine.Services;
using GammonX.Server.Analysis;
using GammonX.Server.Bot;
using GammonX.Server.Contracts;
using GammonX.Server.EntityFramework.Entities;
using GammonX.Server.EntityFramework.Services;
using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

using Moq;
using NuGet.Frameworks;
using System.Security.Claims;

namespace GammonX.Server.Tests
{
	public class MatchHubTests
	{
		private readonly HttpClient _wildBgClient = new() { BaseAddress = new Uri("http://localhost:8082/bot/wildbg/") };
		private readonly MatchSessionRepository _matchRepo;
		private readonly IDiceServiceFactory _diceFactory;
		private readonly IBotService _botService;
		private readonly MatchLobbyHub _hub;
		private readonly NormalMatchmakingService _normalService;
		private readonly RankedMatchmakingService _rankedService;
		private readonly BotMatchmakingService _botMatchService;
		private readonly Guid _player1Id = Guid.NewGuid();
		private readonly Guid _player2Id = Guid.NewGuid();

		public MatchHubTests()
		{
			Mock<IPlayerService> service = new();
			service.Setup(x => x.GetWithRatingAsync(_player1Id, default)).Returns(() => Task.FromResult(new Player { Id = _player1Id }));
			service.Setup(x => x.GetWithRatingAsync(_player2Id, default)).Returns(() => Task.FromResult(new Player { Id = _player2Id }));
			_diceFactory = new DiceServiceFactory();
			var gameSessionFactory = new GameSessionFactory(_diceFactory);
			var matchSessionFactory = new MatchSessionFactory(gameSessionFactory);
			_normalService = new NormalMatchmakingService();

			var mockServiceProvider = new Mock<IServiceProvider>();
			mockServiceProvider
				.Setup(x => x.GetService(typeof(IPlayerService)))
				.Returns(service.Object);
			var mockScope = new Mock<IServiceScope>();
			mockScope
				.Setup(x => x.ServiceProvider)
				.Returns(mockServiceProvider.Object);
			var mockScopeFactory = new Mock<IServiceScopeFactory>();
			mockScopeFactory
				.Setup(x => x.CreateScope())
				.Returns(mockScope.Object);

			Mock<IMatchAnalysisQueue> analysisQueue = new();
			analysisQueue.Setup(x => x.EnqueueAsync(It.IsAny<MatchAnalysisJob>())).Returns(new ValueTask());
			analysisQueue.Setup(x => x.DequeueAsync(It.IsAny<CancellationToken>())).Returns(new ValueTask<MatchAnalysisJob>());

			_rankedService = new RankedMatchmakingService(mockScopeFactory.Object);
			_botMatchService = new BotMatchmakingService();
			var compositeService = new CompositeMatchmakingService();
			compositeService.SetServices(_normalService, _rankedService, _botMatchService);
			_matchRepo = new MatchSessionRepository(matchSessionFactory);
			_botService = new WildbgBotService(_wildBgClient);
			_hub = new MatchLobbyHub(compositeService, _matchRepo, _diceFactory, _botService, analysisQueue.Object);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		public async Task MatchHubCanPlayPlayerVsPlayerCanOfferAndAcceptDoublingCube(WellKnownMatchVariant variant, WellKnownMatchModus modus, WellKnownMatchType type)
		{		
			var result = await SetupPlayerVsPlayerMatchSession(variant, modus, type, _player1Id, _player2Id);
			var matchSession = result.Item1;
			var hub1 = result.Item2;
			var hub2 = result.Item3;
			var mockClients = result.Item4;
			var matchIdStr = matchSession.Id.ToString();
			var player1ConnectionId = matchSession.Player1.ConnectionId;
			var player2ConnectionId = matchSession.Player2.ConnectionId;

			var cubeSession = matchSession as IDoubleCubeMatchSession;
			Assert.NotNull(cubeSession);

			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			Assert.NotNull(gameSession);

			if (gameSession.ActivePlayer == _player1Id)
			{
				Assert.False(cubeSession.IsDoubleOfferPending);
				Assert.True(cubeSession.CanOfferDouble(_player1Id));
				await hub1.OfferDoubleAsync(matchIdStr);
				mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.DoubleOffered, It.IsAny<object[]>(), default), Times.Once);
				Assert.True(cubeSession.IsDoubleOfferPending);
				await hub2.AcceptDoubleAsync(matchIdStr);
				Assert.False(cubeSession.IsDoubleOfferPending);
			}
			else if (gameSession.ActivePlayer == _player2Id)
			{
				Assert.False(cubeSession.IsDoubleOfferPending);
				Assert.True(cubeSession.CanOfferDouble(_player2Id));
				await hub2.OfferDoubleAsync(matchIdStr);
				mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.DoubleOffered, It.IsAny<object[]>(), default), Times.Once);
				Assert.True(cubeSession.IsDoubleOfferPending);
				await hub1.AcceptDoubleAsync(matchIdStr);
				Assert.False(cubeSession.IsDoubleOfferPending);
			}

			Assert.Equal(2, cubeSession.GetDoublingCubeValue());
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		public async Task MatchHubCanPlayPlayerVsPlayerResignMatch(WellKnownMatchVariant variant, WellKnownMatchModus modus, WellKnownMatchType type)
		{
			var result = await SetupPlayerVsPlayerMatchSession(variant, modus, type, _player1Id, _player2Id);
			var matchSession = result.Item1;
			var hub1 = result.Item2;
			var hub2 = result.Item3;
			var mockClients = result.Item4;
			var groupName = result.groupName;
			var matchIdStr = matchSession.Id.ToString();
			var player1ConnectionId = matchSession.Player1.ConnectionId;
			var player2ConnectionId = matchSession.Player2.ConnectionId;

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

				if (gameSession.ActivePlayer == _player1Id)
				{
					await hub1.ResignMatchAsync(matchIdStr);
				}
				else if (gameSession.ActivePlayer == _player2Id)
				{
					await hub2.ResignMatchAsync(matchIdStr);
				}
			}
			while (!matchSession.IsMatchOver());

			Assert.True(matchSession.IsMatchOver());
			Assert.False(matchSession.CanStartNextGame());
			var playedSessions = matchSession.GetGameSessions().Where(gs => gs != null).ToList();
			if (type == WellKnownMatchType.CashGame)
			{
				if (variant == WellKnownMatchVariant.Tavli)
				{
					Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) == 3);
				}
				else
				{
					Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) == 1);
				}
			}
			else
			{
				Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) >= 2);
			}

			Assert.True(matchSession.Player2.Points > 0 || matchSession.Player1.Points > 0);
			Assert.True(matchSession.Player2.Points >= type.GetMaxPoints() || matchSession.Player1.Points >= type.GetMaxPoints());
			mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), default), Times.Never);
			mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), default), Times.Exactly(2));
			mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), default), Times.Never);
			mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), default), Times.Exactly(2));
			mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.ForceDisconnect, It.IsAny<object[]>(), default), Times.Once);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		public async Task MatchHubCanPlayPlayerVsPlayerResignGameMatch(WellKnownMatchVariant variant, WellKnownMatchModus modus, WellKnownMatchType type)
		{
			var result = await SetupPlayerVsPlayerMatchSession(variant, modus, type, _player1Id, _player2Id);
			var matchSession = result.Item1;
			var hub1 = result.Item2;
			var hub2 = result.Item3;
			var mockClients = result.Item4;
			var groupName = result.groupName;
			var matchIdStr = matchSession.Id.ToString();
			var player1ConnectionId = matchSession.Player1.ConnectionId;
			var player2ConnectionId = matchSession.Player2.ConnectionId;

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

				if (gameSession.ActivePlayer == _player1Id)
				{
					await hub1.ResignGameAsync(matchIdStr);
				}
				else if (gameSession.ActivePlayer == _player2Id)
				{
					await hub2.ResignGameAsync(matchIdStr);
				}

				if (gameSession.Phase == GamePhase.GameOver && matchSession.GameRound != matchSession.GetGameSessions().Length)
				{
					await hub1.StartGameAsync(matchIdStr);
					await hub2.StartGameAsync(matchIdStr);
				}
			}
			while (!matchSession.IsMatchOver());

			Assert.True(matchSession.IsMatchOver());
			Assert.False(matchSession.CanStartNextGame());
			var playedSessions = matchSession.GetGameSessions().Where(gs => gs != null).ToList();
			var multiplier = 1;
			if (type == WellKnownMatchType.CashGame)
			{
				if (variant == WellKnownMatchVariant.Tavli)
				{
					Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) == 3);
				}
				else
				{
					Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) == 1);
				}
			}
			else
			{
				Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) >= 2);
				multiplier = 2;
			}

			Assert.True(matchSession.Player2.Points > 0 || matchSession.Player1.Points > 0);
			Assert.True(matchSession.Player2.Points >= type.GetMaxPoints() || matchSession.Player1.Points >= type.GetMaxPoints());
			var gameEndedCount = playedSessions.Count - 1 * multiplier;
			mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), default), gameEndedCount == 0 ? Times.Never() : Times.AtLeast(gameEndedCount));
			mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), default), Times.Exactly(2));
			mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), default), gameEndedCount == 0 ? Times.Never() : Times.AtLeast(gameEndedCount));
			mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), default), Times.Exactly(2));
			mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.ForceDisconnect, It.IsAny<object[]>(), default), Times.Once);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		public async Task MatchHubCanPlayPlayerVsPlayerMatch(WellKnownMatchVariant variant, WellKnownMatchModus modus, WellKnownMatchType type)
		{
			var result = await SetupPlayerVsPlayerMatchSession(variant, modus, type, _player1Id, _player2Id);
			var matchSession = result.Item1;
			var hub1 = result.Item2;
			var hub2 = result.Item3;
			var mockClients = result.Item4;
			var groupName = result.groupName;
			var matchIdStr = matchSession.Id.ToString();
			var player1ConnectionId = matchSession.Player1.ConnectionId;
			var player2ConnectionId = matchSession.Player2.ConnectionId;

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

				if (gameSession.ActivePlayer == _player1Id)
				{
					await ExecutePlayerTurnAsync(matchSession, gameSession, hub1, hub2);
				}
				else if (gameSession.ActivePlayer == _player2Id)
				{
					await ExecutePlayerTurnAsync(matchSession, gameSession, hub2, hub1);
				}
			}
			while (!matchSession.IsMatchOver());

			Assert.True(matchSession.IsMatchOver());
			Assert.False(matchSession.CanStartNextGame());
			var playedSessions = matchSession.GetGameSessions().Where(gs => gs != null).ToList();
			var multiplier = 1;
			if (type == WellKnownMatchType.CashGame)
			{
				if (variant == WellKnownMatchVariant.Tavli)
				{
					Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) == 3);
				}
				else
				{
					Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) == 1);
				}
			}
			else
			{
				Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) >= 2);
				multiplier = 2;
			}

			Assert.True(matchSession.Player2.Points > 0 || matchSession.Player1.Points > 0);
			Assert.True(matchSession.Player2.Points >= type.GetMaxPoints() || matchSession.Player1.Points >= type.GetMaxPoints());
			var gameEndedCount = playedSessions.Count - 1 * multiplier;
			mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), default), gameEndedCount == 0 ? Times.Never() : Times.AtLeast(gameEndedCount));
			mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), default), Times.AtLeast(2));
			mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), default), gameEndedCount == 0 ? Times.Never() : Times.AtLeast(gameEndedCount));
			mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), default), Times.AtLeast(2));
			mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.ForceDisconnect, It.IsAny<object[]>(), default), Times.AtLeastOnce());
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchType.SevenPointGame)]
		public async Task MatchHubCanPlayPlayerVsBotMatch(WellKnownMatchVariant variant, WellKnownMatchType type)
		{
			var queueKey = new QueueKey(variant, WellKnownMatchModus.Bot, type);
			var playerId = Guid.NewGuid();
			var matchId = await CreatePlayerVsBotMatchLobbyAsync(queueKey, playerId);
			var matchIdStr = matchId.ToString();

			var matchService = GetService(WellKnownMatchModus.Bot);
			Assert.True(matchService.TryFindMatchLobby(matchId, out var lobby));
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
			var cubeSession = matchSession as IDoubleCubeMatchSession;

			// bot offered a double
			if (cubeSession != null && cubeSession.IsDoubleOfferPending)
			{
				await _hub.AcceptDoubleAsync(matchIdStr);
			}

			do
			{
				if (gameSession.Phase == GamePhase.GameOver)
				{
					gameSession = matchSession.GetGameSession(matchSession.GameRound);
					Assert.NotNull(gameSession);
				}

				// bot offered a double
				if (cubeSession != null && cubeSession.IsDoubleOfferPending)
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
						if (gameSession.Phase == GamePhase.GameOver && !matchSession.IsMatchOver())
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
				if (variant == WellKnownMatchVariant.Tavli)
				{
					Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) == 3);
				}
				else
				{
					Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) == 1);
				}
			}
			else
			{
				Assert.True(playedSessions.Count(gs => gs.Phase == GamePhase.GameOver) >= 2);
			}
			// bot will win every time :: wow the dumb human can win also
			Assert.True(matchSession.Player2.Points > 0);
			Assert.True(matchSession.Player2.Points >= type.GetMaxPoints() || matchSession.Player1.Points >= type.GetMaxPoints());
			var gameEndedCount = playedSessions.Count - 1;
			mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), default), gameEndedCount == 0 ? Times.Never() : Times.AtLeast(gameEndedCount));
			mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), default), Times.Once);
			mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.ForceDisconnect, It.IsAny<object[]>(), default), Times.Once);
		}

		[Theory]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavla, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.CashGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.FivePointGame)]
		[InlineData(WellKnownMatchVariant.Tavli, WellKnownMatchModus.Ranked, WellKnownMatchType.SevenPointGame)]
		public async Task MatchHubCanPlayPlayerVsPlayerCanUndoMoves(WellKnownMatchVariant variant, WellKnownMatchModus modus, WellKnownMatchType type)
		{
			var result = await SetupPlayerVsPlayerMatchSession(variant, modus, type, _player1Id, _player2Id);
			var matchSession = result.Item1;
			var hub1 = result.Item2;
			var hub2 = result.Item3;
			var matchIdStr = matchSession.Id.ToString();

			var gameSession = matchSession.GetGameSession(matchSession.GameRound);
			Assert.NotNull(gameSession);

			if (gameSession.ActivePlayer == _player1Id)
			{
				await hub1.RollAsync(matchIdStr);
				var ms = gameSession.MoveSequences.FirstOrDefault();
				if (ms != null)
				{
					var moves = ms.Moves.ToList();
					foreach (var move in moves)
					{
						await hub1.MoveAsync(matchIdStr, move.From, move.To);
					}
					foreach (var _ in moves)
					{
						await hub1.UndoMoveAsync(matchIdStr);
					}
				}
			}
			else if (gameSession.ActivePlayer == _player2Id)
			{
				await hub2.RollAsync(matchIdStr);
				var ms = gameSession.MoveSequences.FirstOrDefault();
				if (ms != null)
				{
					var moves = ms.Moves.ToList();
					foreach (var move in moves)
					{
						await hub2.MoveAsync(matchIdStr, move.From, move.To);
					}
					foreach (var _ in moves)
					{
						await hub2.UndoMoveAsync(matchIdStr);
					}
				}
			}
		}

		private static async Task ExecutePlayerTurnAsync(IMatchSessionModel matchSession, IGameSessionModel gameSession, MatchLobbyHub activeHub, MatchLobbyHub otherHub)
		{
			var matchIdStr = matchSession.Id.ToString();

			if (matchSession is IDoubleCubeMatchSession cubeSession && cubeSession.IsDoubleOfferPending)
			{
				await activeHub.AcceptDoubleAsync(matchIdStr);
			}

			await activeHub.RollAsync(matchIdStr);

			var ms = gameSession.MoveSequences.FirstOrDefault();
			if (ms != null)
			{
				var moves = ms.Moves.ToList();
				foreach (var move in moves)
				{
					await activeHub.MoveAsync(matchIdStr, move.From, move.To);
				}
			}

			if (gameSession.Phase != GamePhase.GameOver)
			{
				await activeHub.EndTurnAsync(matchIdStr);
				if (gameSession.Phase == GamePhase.GameOver && matchSession.GameRound != matchSession.GetGameSessions().Length)
				{
					await activeHub.StartGameAsync(matchIdStr);
					await otherHub.StartGameAsync(matchIdStr);
				}
			}
			else if (!matchSession.IsMatchOver())
			{
				await otherHub.StartGameAsync(matchIdStr);
				await activeHub.StartGameAsync(matchIdStr);
			}
		}

		private async Task<(IMatchSessionModel, MatchLobbyHub, MatchLobbyHub, Mock<IHubCallerClients>, string groupName)> SetupPlayerVsPlayerMatchSession(
			WellKnownMatchVariant variant,
			WellKnownMatchModus modus,
			WellKnownMatchType type,
			Guid player1Id,
			Guid player2Id)
		{
			Mock<IMatchAnalysisQueue> analysisQueue = new();
			analysisQueue.Setup(x => x.EnqueueAsync(It.IsAny<MatchAnalysisJob>())).Returns(new ValueTask());
			analysisQueue.Setup(x => x.DequeueAsync(It.IsAny<CancellationToken>())).Returns(new ValueTask<MatchAnalysisJob>());

			var queueKey = new QueueKey(variant, modus, type);
			var matchId = await CreatePlayerVsPlayerMatchLobbyAsync(queueKey, player1Id, player2Id);
			var matchIdStr = matchId.ToString();
			var matchService = GetService(modus);

			Assert.True(matchService.TryFindMatchLobby(matchId, out var lobby));
			Assert.NotNull(lobby);
			var groupName = lobby.GroupName;

			var mockClients = new Mock<IHubCallerClients>();
			var mockGroups = new Mock<IGroupManager>();
			var mockGroup = new Mock<IClientProxy>();

			// client 1
			var player1ConnectionId = Guid.NewGuid().ToString();
			var context1 = new HubCallerContextStub(player1ConnectionId);
			var hub1 = new MatchLobbyHub(matchService, _matchRepo, _diceFactory, _botService, analysisQueue.Object)
			{
				Clients = mockClients.Object,
				Groups = mockGroups.Object,
				Context = context1
			};

			// client 2
			var player2ConnectionId = Guid.NewGuid().ToString();
			var context2 = new HubCallerContextStub(player2ConnectionId);
			var hub2 = new MatchLobbyHub(matchService, _matchRepo, _diceFactory, _botService, analysisQueue.Object)
			{
				Clients = mockClients.Object,
				Groups = mockGroups.Object,
				Context = context2
			};

			mockClients.Setup(c => c.Group(groupName)).Returns(mockGroup.Object);
			mockClients.Setup(mc => mc.Client(It.IsAny<string>())).Returns(new Mock<ISingleClientProxy>().Object);
			mockClients.Setup(mc => mc.Caller).Returns(new Mock<ISingleClientProxy>().Object);

			await hub1.JoinMatchAsync(matchId.ToString(), player1Id.ToString());
			await hub2.JoinMatchAsync(matchId.ToString(), player2Id.ToString());

			var matchSession = _matchRepo.Get(matchId);
			Assert.NotNull(matchSession);

			Assert.Equal(player1ConnectionId, matchSession.Player1.ConnectionId);
			Assert.Equal(player2ConnectionId, matchSession.Player2.ConnectionId);

			mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.MatchLobbyWaitingEvent, It.IsAny<object[]>(), default), Times.Once);
			mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.MatchLobbyFoundEvent, It.IsAny<object[]>(), default), Times.Once);
			mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.MatchStartedEvent, It.IsAny<object[]>(), default), Times.Exactly(2));
			mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.MatchStartedEvent, It.IsAny<object[]>(), default), Times.Exactly(2));

			await hub1.StartGameAsync(matchIdStr);
			await hub2.StartGameAsync(matchIdStr);

			mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.GameStartedEvent, It.IsAny<object[]>(), default), Times.Exactly(2));
			mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.GameStartedEvent, It.IsAny<object[]>(), default), Times.Exactly(2));

			return (matchSession, hub1, hub2, mockClients, groupName);
		}

		private async Task<Guid> CreatePlayerVsBotMatchLobbyAsync(QueueKey queueKey, Guid playerId)
		{
			var matchService = GetService(queueKey.MatchModus);
			var queueEntry = await matchService.JoinQueueAsync(playerId, queueKey);
			var payload = queueEntry.ToPayload();
			Assert.Equal(QueueEntryStatus.WaitingForOpponent, payload.Status);
			matchService.TryFindQueueEntry(queueEntry.Id, out var entry);
			Assert.Null(entry);
			matchService.TryFindMatchLobby(queueEntry.Id, out var lobby);
			Assert.NotNull(lobby);
			return lobby.MatchId;
		}

		private async Task<Guid> CreatePlayerVsPlayerMatchLobbyAsync(QueueKey queueKey, Guid player1Id, Guid player2Id)
		{
			var matchService = GetService(queueKey.MatchModus);
			var queueEntry1 = await matchService.JoinQueueAsync(player1Id, queueKey);
			var queueContract1 = queueEntry1.ToPayload();
			Assert.Equal(QueueEntryStatus.WaitingForOpponent, queueContract1.Status);
			var queueEntry2 = await matchService.JoinQueueAsync(player2Id, queueKey);
			var queueContract2 = queueEntry2.ToPayload();
			Assert.Equal(QueueEntryStatus.WaitingForOpponent, queueContract2.Status);

			await matchService.MatchQueuedPlayersAsync();
			matchService.TryFindMatchLobby(queueEntry1.Id, out var lobby1);
			Assert.NotNull(lobby1);
			matchService.TryFindMatchLobby(queueEntry2.Id, out var lobby2);
			Assert.NotNull(lobby2);
			Assert.Equal(lobby1.MatchId, lobby1.MatchId);
			return lobby1.MatchId;
		}

		private IMatchmakingService GetService(WellKnownMatchModus modus)
		{
			if (modus == WellKnownMatchModus.Normal)
			{
				return _normalService;
			}
			else if (modus == WellKnownMatchModus.Ranked)
			{
				return _rankedService;
			}
			else if (modus == WellKnownMatchModus.Bot)
			{
				return _botMatchService;
			}
			throw new InvalidOperationException("not good");
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
