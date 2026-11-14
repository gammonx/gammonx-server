using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models;
using GammonX.Models.Enums;

using GammonX.Server.Bot;
using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Queue;
using GammonX.Server.Repository;
using GammonX.Server.Services;
using GammonX.Server.Tests.Testdata;
using GammonX.Server.Tests.Utils;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using System.Reflection;
using System.Security.Claims;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests.Integration
{
    public class MatchHubTests
    {
        private readonly HttpClient _wildBgClient = new() { BaseAddress = new Uri("http://localhost:8082/bot/wildbg/") };
        private readonly MarsClient _marsClient = new(null, null) { BaseAddress = new Uri("http://localhost:8083/bot/mars/") };
        private readonly MatchSessionRepository _matchRepo;
        private readonly PlayerConnectionRepository _playerConnRepo;
        private readonly IDiceServiceFactory _diceFactory;
        private readonly IBotService _wildBgBot;
        private readonly IBotService _marsBot;
        private readonly MatchLobbyHub _hub;
        private readonly NormalMatchmakingService _normalService;
        private readonly RankedMatchmakingService _rankedService;
        private readonly BotMatchmakingService _botMatchService;
        private readonly Guid _player1Id = Guid.NewGuid();
        private readonly Guid _player2Id = Guid.NewGuid();
        private readonly Mock<IWorkQueueService> _workQueueService;
        private readonly ICancellationTokenService _cancellationTokenService;
        private readonly Dictionary<Guid, IMatchSessionModel> _completedMatches = new();


        public MatchHubTests()
        {
            _playerConnRepo = new PlayerConnectionRepository();
            _diceFactory = new DiceServiceFactory();
            var gameSessionFactory = new GameSessionFactory(_diceFactory);
            var matchSessionFactory = new MatchSessionFactory(gameSessionFactory);
            _normalService = new NormalMatchmakingService(_playerConnRepo);

            var mockScopeFactory = new Mock<IServiceScopeFactory>();
            Assert.NotNull(mockScopeFactory);

            _workQueueService = new();
            _workQueueService.Setup(x => x.EnqueueGameResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _workQueueService.Setup(x => x.EnqueueMatchResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()))
                .Callback<IMatchSessionModel, CancellationToken>((match, _) => _completedMatches[match.Id] = (IMatchSessionModel)match.Clone())
                .Returns(Task.CompletedTask);
            _workQueueService.Setup(x => x.EnqueueStatProcessingAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _workQueueService.Setup(x => x.EnqueueRatingProcessingAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var stubRepoClient = new SimpleRepositoryClient();

            _rankedService = new RankedMatchmakingService(_playerConnRepo, stubRepoClient);
            _botMatchService = new BotMatchmakingService(_playerConnRepo);
            var compositeService = new CompositeMatchmakingService(_playerConnRepo);
            compositeService.AddService(MatchModus.Normal, _normalService);
            compositeService.AddService(MatchModus.Ranked, _rankedService);
            compositeService.AddService(MatchModus.Bot, _botMatchService);
            _matchRepo = new MatchSessionRepository(matchSessionFactory);
            _wildBgBot = new WildbgBotService(_wildBgClient);
            _marsBot = new MarsBotService(_marsClient);
            _cancellationTokenService = new CancellationTokenServiceImpl();
            _hub = new MatchLobbyHub(BuildServiceProvider(compositeService));
        }

        [Theory]
        [InlineData(MatchModus.Bot, false)]
        [InlineData(MatchModus.Bot, true)]
        [InlineData(MatchModus.Ranked, false)]
        [InlineData(MatchModus.Ranked, true)]
        public async Task BotLobbyStartsAndPublishesRatingsOnlyWhenRanked(MatchModus modus, bool botStarts)
        {
            // Arrange.
            var fixture = await CreateBotLobbyFixture(modus, botStarts);
            var id = fixture.Session.Id;

            // Act.
            await fixture.Hub.StartMatchAsync(id.ToString());

            // Assert.
            var started = _matchRepo.Get(id)!;
            Assert.True(started.Player2.IsBot);
            Assert.Equal(WellKnownBotPlayers.GetBotPlayerId(BotLevel.Hard), started.Player2.Id);
            Assert.Equal(modus, started.Modus);
            Assert.Equal(BotLevel.Hard, started.BotLevel);
            Assert.NotNull(started.StartedAt);
            Assert.Equal(started.Player1.Id, started.GetGameSession(started.GameRound)!.ActivePlayer);
            Assert.False(started.Player1.IsBot);
            fixture.Clients.Verify(value => value.Client(started.Player1.ConnectionId), Times.AtLeastOnce);
            fixture.Clients.Verify(value => value.Client(WellKnownBotPlayers.ConnectionId), Times.Never);

            await fixture.Hub.ResignMatchAsync(id.ToString());
            Assert.Null(_matchRepo.Get(id));
            _workQueueService.Verify(value => value.EnqueueRatingProcessingAsync(
                It.Is<IMatchSessionModel>(match => match.Id == id && match.Player2.Id == started.Player2.Id),
                It.IsAny<CancellationToken>()), modus == MatchModus.Ranked ? Times.Once() : Times.Never());
            Assert.NotNull(_playerConnRepo.Get(started.Player2.Id));
        }

        [Fact]
        public async Task RankedBotCleanupPreservesOtherMatchUsingSameBot()
        {
            // Arrange.
            var first = await CreateBotLobbyFixture(MatchModus.Ranked);
            var second = await CreateBotLobbyFixture(MatchModus.Ranked);
            await first.Hub.StartMatchAsync(first.Session.Id.ToString());
            await second.Hub.StartMatchAsync(second.Session.Id.ToString());
            var botConnection = _playerConnRepo.Get(first.Session.Player2.Id);

            // Act.
            await first.Hub.ResignMatchAsync(first.Session.Id.ToString());

            // Assert.
            Assert.Same(botConnection, _playerConnRepo.Get(second.Session.Player2.Id));
            var surviving = _matchRepo.Get(second.Session.Id)!;
            Assert.False(surviving.IsMatchOver());
            Assert.Equal(0, surviving.Player2.Points);
            await second.Hub.ResignMatchAsync(second.Session.Id.ToString());
            Assert.Null(_matchRepo.Get(second.Session.Id));
            Assert.Same(botConnection, _playerConnRepo.Get(second.Session.Player2.Id));
        }

        [Fact]
        public async Task RankedBotReconnectResumesInterruptedTurn()
        {
            // Arrange: persist the handoff to the bot without running its turn.
            var fixture = await CreateBotLobbyFixture(MatchModus.Ranked);
            await fixture.Hub.StartMatchAsync(fixture.Session.Id.ToString());
            await using var setupLease = await _matchRepo.AcquireStateAsync(fixture.Session.Id);
            var session = setupLease.Get()!;
            var game = session.GetGameSession(session.GameRound)!;
            foreach (var move in game.MoveSequences.First().Moves.ToArray())
            {
                session.MoveCheckers(session.Player1.Id, move.From, move.To);
            }
            session.EndTurn(session.Player1.Id);
            Assert.True(setupLease.Commit());
            await setupLease.DisposeAsync();
            fixture.Hub.Context = new HubCallerContextStub("reconnected-human", session.Player1.Id, session.Id);

            // Act.
            await fixture.Hub.OnConnectedAsync();

            // Assert.
            var resumed = _matchRepo.Get(session.Id)!;
            Assert.Equal("reconnected-human", resumed.Player1.ConnectionId);
            Assert.Equal(resumed.Player1.Id, resumed.GetGameSession(resumed.GameRound)!.ActivePlayer);
            fixture.Clients.Verify(value => value.Client(WellKnownBotPlayers.ConnectionId), Times.Never);
            await fixture.Hub.ResignMatchAsync(session.Id.ToString());
        }

        [Fact]
        public async Task OldSocketDisconnectAfterReconnectPreservesCurrentTimersAndConnection()
        {
            // Arrange
            var fixture = await CreateBotLobbyFixture(MatchModus.Ranked);
            var playerId = fixture.Session.Player1.Id;
            var matchId = fixture.Session.Id;
            var oldConnectionId = fixture.Session.Player1.ConnectionId!;
            fixture.Hub.Context = new HubCallerContextStub("replacement", playerId, matchId);
            await fixture.Hub.OnConnectedAsync();
            var connection = _playerConnRepo.Get(playerId)!;
            var lastSeen = connection.LastSeenUtc;
            var remainingGrace = connection.DisconnectGracePeriod;
            var turnKey = new CancellationTokenKey { MatchId = matchId, PlayerId = playerId, Category = CancellationTokenCategory.Turn };
            Assert.True(_cancellationTokenService.TryGet(turnKey, out var turnTimer));

            // Act
            fixture.Hub.Context = new HubCallerContextStub(oldConnectionId, playerId, matchId);
            await fixture.Hub.OnDisconnectedAsync(null);

            // Assert
            Assert.Equal("replacement", connection.ConnectionId);
            Assert.Equal(lastSeen, connection.LastSeenUtc);
            Assert.Equal(remainingGrace, connection.DisconnectGracePeriod);
            Assert.True(_cancellationTokenService.TryGet(turnKey, out var currentTurnTimer));
            Assert.Same(turnTimer, currentTurnTimer);
            Assert.False(_cancellationTokenService.TryGet(new CancellationTokenKey
            {
                MatchId = matchId, PlayerId = playerId, Category = CancellationTokenCategory.Disconnect
            }, out _));
            _cancellationTokenService.CancelForMatch(matchId, CancellationTokenCategory.Turn);
        }

        [Fact]
        public async Task ExpiryWaitingForMatchLeaseCannotForfeitReplacementConnection()
        {
            // Arrange
            var fixture = await CreateBotLobbyFixture(MatchModus.Ranked);
            var playerId = fixture.Session.Player1.Id;
            var matchId = fixture.Session.Id;
            var oldConnectionId = fixture.Session.Player1.ConnectionId!;
            using var timer = new CancellationTokenSource();
            var key = new CancellationTokenKey { MatchId = matchId, PlayerId = playerId, Category = CancellationTokenCategory.Disconnect };
            _cancellationTokenService.Store(key, timer);
            var lease = await _matchRepo.AcquireStateAsync(matchId);

            // Act: expiry has already fired but must wait for the current match operation.
            var expiry = InvokeDisconnectExpiryAsync(fixture.Hub, playerId, matchId, oldConnectionId, timer);
            Assert.False(expiry.IsCompleted);
            _playerConnRepo.AddOrUpdate(playerId, "replacement");
            await lease.DisposeAsync();
            await expiry.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("replacement", _playerConnRepo.Get(playerId)!.ConnectionId);
            Assert.False(_matchRepo.Get(matchId)!.IsMatchOver());
            _cancellationTokenService.Cancel(key);
            _cancellationTokenService.CancelForMatch(matchId, CancellationTokenCategory.Turn);
        }

        [Fact]
        public async Task OldExpiryCannotCancelSecondDisconnectAndCurrentExpiryStillForfeits()
        {
            // Arrange
            var fixture = await CreateBotLobbyFixture(MatchModus.Ranked);
            var playerId = fixture.Session.Player1.Id;
            var matchId = fixture.Session.Id;
            var connectionId = fixture.Session.Player1.ConnectionId!;
            fixture.Hub.Context = new HubCallerContextStub(connectionId, playerId, matchId);
            var key = new CancellationTokenKey { MatchId = matchId, PlayerId = playerId, Category = CancellationTokenCategory.Disconnect };
            await fixture.Hub.OnDisconnectedAsync(null);
            Assert.True(_cancellationTokenService.TryGet(key, out var oldTimer));
            fixture.Hub.Context = new HubCallerContextStub("replacement", playerId, matchId);
            await fixture.Hub.OnConnectedAsync();
            await fixture.Hub.OnDisconnectedAsync(null);
            Assert.True(_cancellationTokenService.TryGet(key, out var currentTimer));
            var connection = _playerConnRepo.Get(playerId)!;
            var remainingGrace = connection.DisconnectGracePeriod;
            var lastSeen = connection.LastSeenUtc;

            // Act
            await InvokeDisconnectExpiryAsync(fixture.Hub, playerId, matchId, connectionId, oldTimer!);
            // Duplicate callbacks must not restart the current grace period either.
            await fixture.Hub.OnDisconnectedAsync(null);

            // Assert
            Assert.True(_cancellationTokenService.TryGet(key, out var registered));
            Assert.Same(currentTimer, registered);
            Assert.False(currentTimer!.IsCancellationRequested);
            Assert.Equal(remainingGrace, connection.DisconnectGracePeriod);
            Assert.Equal(lastSeen, connection.LastSeenUtc);
            Assert.NotNull(_matchRepo.Get(matchId));
            await InvokeDisconnectExpiryAsync(fixture.Hub, playerId, matchId, "replacement", currentTimer);
            Assert.Null(_matchRepo.Get(matchId));
            Assert.Null(_playerConnRepo.Get(playerId));
            _workQueueService.Verify(value => value.EnqueueRatingProcessingAsync(
                It.Is<IMatchSessionModel>(match => match.Id == matchId), It.IsAny<CancellationToken>()), Times.Once);
        }

        private static Task InvokeDisconnectExpiryAsync(MatchLobbyHub hub, Guid playerId, Guid matchId,
            string connectionId, CancellationTokenSource timer)
        {
            var method = typeof(MatchLobbyHub).GetMethod("HandlePermanentDisconnectAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return (Task)method.Invoke(hub, [playerId, matchId, connectionId, timer])!;
        }

        [Fact]
        public async Task UnregisteredExpiryCannotConsumeCurrentTimerEvenWithSameConnectionId()
        {
            // Arrange
            var fixture = await CreateBotLobbyFixture(MatchModus.Ranked);
            var playerId = fixture.Session.Player1.Id;
            var matchId = fixture.Session.Id;
            using var staleTimer = new CancellationTokenSource();
            using var currentTimer = new CancellationTokenSource();
            var key = new CancellationTokenKey { MatchId = matchId, PlayerId = playerId, Category = CancellationTokenCategory.Disconnect };
            _cancellationTokenService.Store(key, currentTimer);

            // Act
            await InvokeDisconnectExpiryAsync(fixture.Hub, playerId, matchId, fixture.Session.Player1.ConnectionId!, staleTimer);

            // Assert
            Assert.True(_cancellationTokenService.TryGet(key, out var registered));
            Assert.Same(currentTimer, registered);
            Assert.NotNull(_playerConnRepo.Get(playerId));
            Assert.False(_matchRepo.Get(matchId)!.IsMatchOver());
            _cancellationTokenService.Cancel(key);
            _cancellationTokenService.CancelForMatch(matchId, CancellationTokenCategory.Turn);
        }

        [Fact]
        public async Task HumanMatchExpiryResignsOnlyTheSpecifiedMatch()
        {
            // Arrange
            var other = await CreatePersistenceFixture();
            var fixture = await CreatePersistenceFixture();
            var playerId = fixture.Session.Player1.Id;
            var matchId = fixture.Session.Id;
            var connectionId = fixture.Session.Player1.ConnectionId!;
            _playerConnRepo.AddOrUpdate(playerId, connectionId);
            fixture.Hub.Context = new HubCallerContextStub(connectionId, playerId, matchId);
            var key = new CancellationTokenKey { MatchId = matchId, PlayerId = playerId, Category = CancellationTokenCategory.Disconnect };

            // Act
            await fixture.Hub.OnDisconnectedAsync(null);
            Assert.True(_cancellationTokenService.TryGet(key, out var timer));
            await InvokeDisconnectExpiryAsync(fixture.Hub, playerId, matchId, connectionId, timer!);

            // Assert
            Assert.Null(_matchRepo.Get(matchId));
            Assert.False(_matchRepo.Get(other.Session.Id)!.IsMatchOver());
            Assert.False(_cancellationTokenService.TryGet(key, out _));
        }

        [Fact]
        public async Task NonparticipantDisconnectAndExpiryCannotChangeMatchOrConnection()
        {
            // Arrange
            var fixture = await CreatePersistenceFixture();
            var playerId = Guid.NewGuid();
            var matchId = fixture.Session.Id;
            _playerConnRepo.AddOrUpdate(playerId, "unrelated");
            fixture.Hub.Context = new HubCallerContextStub("unrelated", playerId, matchId);
            var key = new CancellationTokenKey { MatchId = matchId, PlayerId = playerId, Category = CancellationTokenCategory.Disconnect };

            // Act / Assert: the socket callback must not start a timer.
            await fixture.Hub.OnDisconnectedAsync(null);
            Assert.False(_cancellationTokenService.TryGet(key, out _));
            using var timer = new CancellationTokenSource();
            _cancellationTokenService.Store(key, timer);
            await InvokeDisconnectExpiryAsync(fixture.Hub, playerId, matchId, "unrelated", timer);
            Assert.NotNull(_playerConnRepo.Get(playerId));
            Assert.False(_matchRepo.Get(matchId)!.IsMatchOver());
            _cancellationTokenService.Cancel(key);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RankedBotReconnectWhileWaitingForHumanAcceptanceRestoresOnlyHumanTimer(bool betweenGames)
        {
            // Arrange.
            var fixture = await CreateBotLobbyFixture(MatchModus.Ranked);
            if (betweenGames)
            {
                await fixture.Hub.StartMatchAsync(fixture.Session.Id.ToString());
                await fixture.Hub.ResignGameAsync(fixture.Session.Id.ToString());
            }
            await using var setupLease = await _matchRepo.AcquireStateAsync(fixture.Session.Id);
            var session = setupLease.Get()!;
            _cancellationTokenService.CancelForMatch(session.Id, CancellationTokenCategory.Turn);
            await setupLease.DisposeAsync();
            fixture.Hub.Context = new HubCallerContextStub("reconnected-human", session.Player1.Id, session.Id);

            // Act.
            await fixture.Hub.OnConnectedAsync();

            // Assert.
            Assert.True(_cancellationTokenService.TryGet(new CancellationTokenKey
            {
                MatchId = session.Id, PlayerId = session.Player1.Id, Category = CancellationTokenCategory.Turn
            }, out _));
            Assert.False(_cancellationTokenService.TryGet(new CancellationTokenKey
            {
                MatchId = session.Id, PlayerId = session.Player2.Id, Category = CancellationTokenCategory.Turn
            }, out _));
            if (!betweenGames)
            {
                await fixture.Hub.StartMatchAsync(session.Id.ToString());
            }
            await fixture.Hub.ResignMatchAsync(session.Id.ToString());
        }

        [Fact]
        public async Task RankedBotHumanTimeoutPublishesRatingResult()
        {
            // Arrange.
            var fixture = await CreateBotLobbyFixture(MatchModus.Ranked);
            await fixture.Hub.StartMatchAsync(fixture.Session.Id.ToString());

            // Act.
            await InvokeTurnTimeoutAsync(fixture.Hub, fixture.Session.Id, fixture.Session.Player1.Id);
            var timedOutGame = _matchRepo.Get(fixture.Session.Id)!;
            Assert.Equal(GamePhase.GameOver, timedOutGame.GetGameSession(timedOutGame.GameRound)!.Phase);
            await InvokeTurnTimeoutAsync(fixture.Hub, fixture.Session.Id, fixture.Session.Player1.Id);

            // Assert.
            Assert.Null(_matchRepo.Get(fixture.Session.Id));
            _workQueueService.Verify(value => value.EnqueueRatingProcessingAsync(
                It.Is<IMatchSessionModel>(match => match.Id == fixture.Session.Id), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, false, false)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(false, false, true)]
        [InlineData(true, true, true)]
        public async Task RankedBotAutomaticallyRespondsToHumanDouble(bool accepts, bool serviceFails, bool reconnect)
        {
            // Arrange.
            var bot = new Mock<IBotService>();
            bot.Setup(value => value.ShouldTakeDouble(It.IsAny<IMatchSessionModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(accepts);
            if (serviceFails)
            {
                bot.Setup(value => value.ShouldTakeDouble(It.IsAny<IMatchSessionModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new HttpRequestException("Bot service unavailable."));
            }
            var fixture = await CreatePersistenceFixture(modus: MatchModus.Ranked, botOpponent: true, botService: bot.Object);
            if (reconnect)
            {
                await using var lease = await _matchRepo.AcquireStateAsync(fixture.Session.Id);
                var match = lease.Get()!;
                Assert.IsAssignableFrom<IDoubleCubeMatchSession>(match).OfferDouble(match.Player1.Id);
                Assert.True(lease.Commit());
                fixture.Hub.Context = new HubCallerContextStub("player1", match.Player1.Id, match.Id);
            }

            // Act.
            if (reconnect)
            {
                await fixture.Hub.OnConnectedAsync();
            }
            else
            {
                await fixture.Hub.OfferDoubleAsync(fixture.Session.Id.ToString());
            }

            // Assert.
            bot.Verify(value => value.ShouldTakeDouble(It.IsAny<IMatchSessionModel>(), _player2Id, It.IsAny<CancellationToken>()), Times.Once);
            var stored = _matchRepo.Get(fixture.Session.Id)!;
            Assert.False(Assert.IsAssignableFrom<IDoubleCubeMatchSession>(stored).IsDoubleOfferPending);
            Assert.Equal(accepts ? GamePhase.WaitingForRoll : GamePhase.GameOver, stored.GetGameSession(stored.GameRound)!.Phase);
            if (accepts)
            {
                Assert.Equal(stored.Player1.Id, stored.GetGameSession(stored.GameRound)!.ActivePlayer);
                Assert.Equal(2, Assert.IsAssignableFrom<IDoubleCubeMatchSession>(stored).GetDoublingCubeValue());
            }
            bot.Verify(value => value.GetNextMovesAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            fixture.Client.Verify(value => value.SendCoreAsync(ServerEventTypes.ErrorEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.True(_cancellationTokenService.TryGet(new CancellationTokenKey
            {
                MatchId = stored.Id, PlayerId = stored.Player1.Id, Category = CancellationTokenCategory.Turn
            }, out _));
            Assert.False(_cancellationTokenService.TryGet(new CancellationTokenKey
            {
                MatchId = stored.Id, PlayerId = stored.Player2.Id, Category = CancellationTokenCategory.Turn
            }, out _));
            await fixture.Hub.ResignMatchAsync(stored.Id.ToString());
        }

        [Fact]
        public async Task RankedBotAcceptsNextGameAutomatically()
        {
            // Arrange.
            var fixture = await CreatePersistenceFixture(modus: MatchModus.Ranked, botOpponent: true, botService: new SimpleBotService());
            await using var setupLease = await _matchRepo.AcquireStateAsync(fixture.Session.Id);
            fixture.Session = setupLease.Get()!;
            fixture.Session.ResignGame(fixture.Session.Player2.Id);
            Assert.True(setupLease.Commit());
            await setupLease.DisposeAsync();

            // Act.
            await fixture.Hub.StartGameAsync(fixture.Session.Id.ToString());

            // Assert.
            Assert.Equal(2, _matchRepo.Get(fixture.Session.Id)!.GameRound);
            await fixture.Hub.ResignMatchAsync(fixture.Session.Id.ToString());
        }

        [Fact]
        public async Task RankedBotTiedOpeningRollsCanBeRetried()
        {
            // Arrange.
            var fixture = await CreatePersistenceFixture(startMatch: false, modus: MatchModus.Ranked,
                botOpponent: true, botService: new SimpleBotService());
            var dice = new Mock<IDiceService>();
            dice.SetupSequence(value => value.Roll(1, 6)).Returns([3]).Returns([3]).Returns([6]).Returns([1]);
            var factory = new Mock<IDiceServiceFactory>();
            factory.Setup(value => value.Create(It.IsAny<DiceServiceType>())).Returns(dice.Object);
            var hub = new MatchLobbyHub(BuildServiceProvider(_rankedService, factory.Object, new SimpleBotService()))
            {
                Clients = fixture.Hub.Clients, Groups = fixture.Hub.Groups, Context = fixture.Hub.Context
            };

            // Act.
            await hub.StartMatchAsync(fixture.Session.Id.ToString());
            var tied = _matchRepo.Get(fixture.Session.Id)!;
            await hub.StartMatchAsync(fixture.Session.Id.ToString());

            // Assert.
            Assert.Equal(3, tied.Player1.StartDiceRoll);
            Assert.Equal(3, tied.Player2.StartDiceRoll);
            Assert.Null(tied.StartedAt);
            Assert.NotNull(_matchRepo.Get(fixture.Session.Id)!.StartedAt);
            await hub.ResignMatchAsync(fixture.Session.Id.ToString());
        }

        private async Task<(IMatchSessionModel Session, MatchLobbyHub Hub, Mock<IHubCallerClients> Clients)> CreateBotLobbyFixture(
            MatchModus modus, bool botStarts = false)
        {
            var playerId = Guid.NewGuid();
            var service = GetService(modus);
            var entry = await service.JoinQueueAsync(playerId, new QueueKey(MatchVariant.Backgammon, modus, MatchType.SevenPointGame, BotLevel.Hard));
            await service.MatchQueuedPlayersAsync();
            Assert.True(service.TryFindMatchLobby(entry.Id, out var lobby));
            var dice = new Mock<IDiceService>();
            dice.SetupSequence(value => value.Roll(1, 6)).Returns([botStarts ? 1 : 6]).Returns([botStarts ? 6 : 1]);
            var factory = new Mock<IDiceServiceFactory>();
            factory.Setup(value => value.Create(It.IsAny<DiceServiceType>())).Returns(dice.Object);
            var clients = new Mock<IHubCallerClients>();
            var client = new Mock<ISingleClientProxy>();
            clients.Setup(value => value.Client(It.IsAny<string>())).Returns(client.Object);
            clients.Setup(value => value.Caller).Returns(client.Object);
            clients.Setup(value => value.Group(It.IsAny<string>())).Returns(client.Object);
            var hub = new MatchLobbyHub(BuildServiceProvider(service, factory.Object, new SimpleBotService()))
            {
                Clients = clients.Object,
                Groups = new Mock<IGroupManager>().Object,
                // A human connection may be GUID-shaped; only the reserved marker identifies bots.
                Context = new HubCallerContextStub(Guid.NewGuid().ToString())
            };
            await hub.JoinMatchAsync(lobby!.MatchId.ToString(), playerId.ToString());
            var session = _matchRepo.Get(lobby.MatchId)!;
            Assert.True(session.Player2.IsBot);
            return (session, hub, clients);
        }

        [Fact]
        public async Task MatchHubWaitsForRepositoryLockBeforeReadingStartingState()
        {
            // Arrange
            var fixture = await CreatePersistenceFixture(startMatch: false);
            var id = fixture.Session.Id;
            var lease = await _matchRepo.AcquireStateAsync(id);

            // Act
            var start = fixture.Hub.StartMatchAsync(id.ToString());
            Assert.False(start.IsCompleted);
            var latest = lease.Get()!;
            latest.Player2.AcceptNextGame();
            Assert.True(lease.Commit());
            await lease.DisposeAsync();
            await start.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // Assert
            Assert.True(_matchRepo.Get(id)!.Player2.NextGameAccepted);
        }

        [Fact]
        public async Task MatchHubCommitsStartingRollBeforeWaitingNotification()
        {
            var fixture = await CreatePersistenceFixture(startMatch: false);
            var waiting = CaptureStoredSessions(fixture.Client, fixture.Session.Id, ServerEventTypes.MatchWaitingEvent);

            await fixture.Hub.StartMatchAsync(fixture.Session.Id.ToString());

            var stored = Assert.Single(waiting);
            Assert.Equal(2, stored.Player1.StartDiceRoll);
            Assert.True(stored.Player1.NextGameAccepted);
            Assert.Null(stored.Player2.StartDiceRoll);

            fixture.Hub.Context = new HubCallerContextStub(fixture.Session.Player2.ConnectionId);
            var started = CaptureStoredSessions(fixture.Client, fixture.Session.Id, ServerEventTypes.GameStartedEvent);
            await fixture.Hub.StartMatchAsync(fixture.Session.Id.ToString());

            Assert.NotEmpty(started);
            Assert.All(started, match =>
            {
                Assert.Equal(2, match.Player1.StartDiceRoll);
                Assert.Equal(1, match.Player2.StartDiceRoll);
                Assert.NotNull(match.GetGameSession(match.GameRound));
            });
        }

        [Fact]
        public async Task MatchHubCommitsTiedStartingRollsBeforeWaitingNotification()
        {
            var fixture = await CreatePersistenceFixture(startMatch: false);
            await using var setupLease = await _matchRepo.AcquireStateAsync(fixture.Session.Id);
            fixture.Session = setupLease.Get()!;
            fixture.Session.Player2.SetStartDiceRoll(2);
            fixture.Session.Player2.AcceptNextGame();
            Assert.True(setupLease.Commit());
            await setupLease.DisposeAsync();
            var waiting = CaptureStoredSessions(fixture.Client, fixture.Session.Id, ServerEventTypes.MatchWaitingForStartEvent);

            await fixture.Hub.StartMatchAsync(fixture.Session.Id.ToString());

            Assert.NotEmpty(waiting);
            Assert.All(waiting, match =>
            {
                Assert.Equal(2, match.Player1.StartDiceRoll);
                Assert.Equal(2, match.Player2.StartDiceRoll);
                Assert.True(match.Player1.NextGameAccepted);
                Assert.True(match.Player2.NextGameAccepted);
                Assert.Null(match.GetGameSession(match.GameRound));
            });
        }

        [Fact]
        public async Task MatchHubCommitsNextGameAcceptanceBeforeWaitingNotification()
        {
            var fixture = await CreatePersistenceFixture();
            await using var setupLease = await _matchRepo.AcquireStateAsync(fixture.Session.Id);
            fixture.Session = setupLease.Get()!;
            fixture.Session.ResignGame(fixture.Session.Player1.Id);
            Assert.True(setupLease.Commit());
            await setupLease.DisposeAsync();
            var waiting = CaptureStoredSessions(fixture.Client, fixture.Session.Id, ServerEventTypes.GameWaitingEvent);

            await fixture.Hub.StartGameAsync(fixture.Session.Id.ToString());

            var stored = Assert.Single(waiting);
            Assert.True(stored.Player1.NextGameAccepted);
            Assert.False(stored.Player2.NextGameAccepted);

            fixture.Hub.Context = new HubCallerContextStub(fixture.Session.Player2.ConnectionId);
            var started = CaptureStoredSessions(fixture.Client, fixture.Session.Id, ServerEventTypes.GameStartedEvent);
            await fixture.Hub.StartGameAsync(fixture.Session.Id.ToString());

            Assert.NotEmpty(started);
            Assert.All(started, match => Assert.Equal(2, match.GameRound));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MatchHubCommitsMoveBeforeGameStateNotification(bool player2Starts)
        {
            var fixture = await CreatePersistenceFixture(player2Starts: player2Starts);
            await using var setupLease = await _matchRepo.AcquireStateAsync(fixture.Session.Id);
            fixture.Session = setupLease.Get()!;
            var game = fixture.Session.GetGameSession(fixture.Session.GameRound)!;
            game.SetDiceRolls([2, 1], !player2Starts);
            Assert.True(setupLease.Commit());
            await setupLease.DisposeAsync();
            var move = game.MoveSequences.First().Moves.First();
            var expected = (IMatchSessionModel)fixture.Session.Clone();
            expected.MoveCheckers(game.ActivePlayer, move.From, move.To);
            var expectedGame = expected.GetGameSession(expected.GameRound)!;
            var notifications = CaptureStoredSessions(fixture.Client, fixture.Session.Id, ServerEventTypes.GameStateEvent);
            var positions = player2Starts
                ? BoardBroker.InvertFromToMove(game.Modus, move.From, move.To)
                : (move.From, move.To);

            await fixture.Hub.MoveAsync(fixture.Session.Id.ToString(), positions.Item1, positions.Item2);

            Assert.NotEmpty(notifications);
            Assert.All(notifications, match =>
            {
                var storedGame = match.GetGameSession(match.GameRound)!;
                Assert.Equal(expectedGame.BoardModel, storedGame.BoardModel);
                Assert.Equal(expectedGame.Phase, storedGame.Phase);
                Assert.Equal(expectedGame.DiceRolls.Select(roll => (roll.Roll, roll.Used)),
                    storedGame.DiceRolls.Select(roll => (roll.Roll, roll.Used)));
                Assert.Equal(expectedGame.BoardModel.History.ToString(), storedGame.BoardModel.History.ToString());
            });
        }

        [Fact]
        public async Task MatchHubRejectedMovePreservesCommittedState()
        {
            var fixture = await CreatePersistenceFixture();
            await using var setupLease = await _matchRepo.AcquireStateAsync(fixture.Session.Id);
            fixture.Session = setupLease.Get()!;
            var game = fixture.Session.GetGameSession(fixture.Session.GameRound)!;
            game.SetDiceRolls([2, 1], true);
            Assert.True(setupLease.Commit());
            await setupLease.DisposeAsync();
            var notifications = CaptureStoredSessions(fixture.Client, fixture.Session.Id, ServerEventTypes.GameStateEvent);

            await fixture.Hub.MoveAsync(fixture.Session.Id.ToString(), -99, -98);

            var storedGame = _matchRepo.Get(fixture.Session.Id)!.GetGameSession(fixture.Session.GameRound)!;
            Assert.Empty(notifications);
            Assert.Equal(game.BoardModel, storedGame.BoardModel);
            Assert.Equal(game.Phase, storedGame.Phase);
            Assert.Equal(game.BoardModel.History.ToString(), storedGame.BoardModel.History.ToString());
            Assert.Equal(game.DiceRolls.Select(roll => (roll.Roll, roll.Used)),
                storedGame.DiceRolls.Select(roll => (roll.Roll, roll.Used)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MatchHubCommitsWinningMoveBeforeResultPublication(bool concludesMatch)
        {
            var fixture = await CreatePersistenceFixture(type: concludesMatch ? MatchType.CashGame : MatchType.SevenPointGame);
            await using var setupLease = await _matchRepo.AcquireStateAsync(fixture.Session.Id);
            fixture.Session = setupLease.Get()!;
            var game = fixture.Session.GetGameSession(fixture.Session.GameRound)!;
            var fields = new int[24];
            fields[23] = -1;
            fields[0] = 1;
            game.BoardModel.SetFields(fields);
            game.BoardModel.BearOffChecker(true, 14);
            game.BoardModel.BearOffChecker(false, 14);
            game.SetDiceRolls([1, 2], true);
            Assert.True(setupLease.Commit());
            await setupLease.DisposeAsync();
            var move = game.MoveSequences.First().Moves.First();
            IMatchSessionModel? published = null;
            _workQueueService.Setup(service => service.EnqueueGameResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback<IMatchSessionModel, int, CancellationToken>((match, _, _) => published = _matchRepo.Get(match.Id))
                .Returns(Task.CompletedTask);

            await fixture.Hub.MoveAsync(fixture.Session.Id.ToString(), move.From, move.To);

            Assert.NotNull(published);
            Assert.Equal(GamePhase.GameOver, published.GetGameSession(published.GameRound)!.Phase);
            Assert.Equal(fixture.Session.Player1.Id, published.GetGameSession(published.GameRound)!.Result.WinnerId);
            Assert.Equal(1, published.Player1.Points);
            Assert.Equal(concludesMatch, published.IsMatchOver());
            Assert.Equal(concludesMatch, _matchRepo.Get(fixture.Session.Id) == null);
        }

        [Theory]
        [InlineData(MatchModus.Bot)]
        [InlineData(MatchModus.Ranked)]
        public async Task MatchHubCommitsBotDoubleOfferBeforeNotification(MatchModus modus)
        {
            var bot = new Mock<IBotService>();
            bot.Setup(service => service.ShouldOfferDouble(It.IsAny<IMatchSessionModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var fixture = await CreatePersistenceFixture(modus: modus, botService: bot.Object, botOpponent: true);
            await using var setupLease = await _matchRepo.AcquireStateAsync(fixture.Session.Id);
            fixture.Session = setupLease.Get()!;
            var game = fixture.Session.GetGameSession(fixture.Session.GameRound)!;
            game.SetDiceRolls([2, 1], true);
            foreach (var move in game.MoveSequences.First().Moves.ToArray())
            {
                fixture.Session.MoveCheckers(fixture.Session.Player1.Id, move.From, move.To);
            }
            Assert.True(setupLease.Commit());
            await setupLease.DisposeAsync();
            var notifications = CaptureStoredSessions(fixture.Client, fixture.Session.Id, ServerEventTypes.DoubleOfferedEvent);

            await fixture.Hub.EndTurnAsync(fixture.Session.Id.ToString());

            Assert.NotEmpty(notifications);
            Assert.All(notifications, match =>
            {
                Assert.True(Assert.IsAssignableFrom<IDoubleCubeMatchSession>(match).IsDoubleOfferPending);
                Assert.Equal(match.Player1.Id, match.GetGameSession(match.GameRound)!.ActivePlayer);
            });
            Assert.True(Assert.IsAssignableFrom<IDoubleCubeMatchSession>(_matchRepo.Get(fixture.Session.Id)).IsDoubleOfferPending);
            bot.Verify(service => service.ShouldOfferDouble(It.IsAny<IMatchSessionModel>(), _player2Id, It.IsAny<CancellationToken>()), Times.Once);
            bot.Verify(service => service.GetNextMovesAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(MatchModus.Bot, false)]
        [InlineData(MatchModus.Ranked, false)]
        [InlineData(MatchModus.Bot, true)]
        [InlineData(MatchModus.Ranked, true)]
        public async Task MatchHubBotFallbackCompletesTurnAfterServiceFailure(MatchModus modus, bool failsImmediately)
        {
            // Arrange
            var bot = new Mock<IBotService>();
            var requests = 0;
            IMatchSessionModel? expected = null;
            bot.Setup(service => service.ShouldOfferDouble(It.IsAny<IMatchSessionModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Cube service unavailable."));
            bot.Setup(service => service.GetNextMovesAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns((IMatchSessionModel match, Guid playerId, CancellationToken _) =>
                {
                    if (++requests > 1 || failsImmediately)
                    {
                        return Task.FromException<MoveSequenceModel>(new InvalidOperationException("Bot service unavailable."));
                    }
                    var move = match.GetGameSession(match.GameRound)!.MoveSequences.First().Moves.First();
                    expected = (IMatchSessionModel)match.Clone();
                    expected.MoveCheckers(playerId, move.From, move.To);
                    return Task.FromResult(new MoveSequenceModel { Moves = [move] });
                });
            var fixture = await CreatePersistenceFixture(modus: modus, botService: bot.Object, botOpponent: true);
            await using var setupLease = await _matchRepo.AcquireStateAsync(fixture.Session.Id);
            fixture.Session = setupLease.Get()!;
            var game = fixture.Session.GetGameSession(fixture.Session.GameRound)!;
            game.SetDiceRolls([2, 1], true);
            foreach (var move in game.MoveSequences.First().Moves.ToArray())
            {
                fixture.Session.MoveCheckers(fixture.Session.Player1.Id, move.From, move.To);
            }
            var dice = new Mock<IDiceService>();
            dice.Setup(service => service.Roll(It.IsAny<int>(), It.IsAny<int>())).Returns([2, 1]);
            game.InjectDiceServiceMock(dice.Object);
            Assert.True(setupLease.Commit());
            await setupLease.DisposeAsync();
            var notifications = CaptureStoredSessions(fixture.Client, fixture.Session.Id, ServerEventTypes.GameStateEvent);

            // Act
            await fixture.Hub.EndTurnAsync(fixture.Session.Id.ToString());

            // Assert: preserve the successfully committed partial move, then finish with unused dice.
            Assert.Equal(failsImmediately ? 1 : 2, requests);
            if (!failsImmediately)
            {
                Assert.NotNull(expected);
                var partial = Assert.Single(notifications, match =>
                    match.GetGameSession(match.GameRound)!.DiceRolls.Count(roll => roll.Used) == 1);
                Assert.Equal(expected.GetGameSession(expected.GameRound)!.BoardModel,
                    partial.GetGameSession(partial.GameRound)!.BoardModel);
            }
            var completedTurn = Assert.Single(notifications, match =>
                match.GetGameSession(match.GameRound)!.DiceRolls.Count(roll => roll.Used) == 2);
            Assert.Equal(GamePhase.WaitingForEndTurn, completedTurn.GetGameSession(completedTurn.GameRound)!.Phase);
            var stored = _matchRepo.Get(fixture.Session.Id)!;
            var storedGame = stored.GetGameSession(stored.GameRound)!;
            Assert.Equal(GamePhase.WaitingForRoll, storedGame.Phase);
            Assert.Equal(stored.Player1.Id, storedGame.ActivePlayer);
            Assert.Equal(completedTurn.GetGameSession(completedTurn.GameRound)!.BoardModel, storedGame.BoardModel);
            Assert.False(Assert.IsAssignableFrom<IDoubleCubeMatchSession>(stored).IsDoubleOfferPending);
            if (modus == MatchModus.Ranked)
            {
                Assert.True(_cancellationTokenService.TryGet(new CancellationTokenKey
                {
                    MatchId = stored.Id, PlayerId = stored.Player1.Id, Category = CancellationTokenCategory.Turn
                }, out _));
            }
            fixture.Client.Verify(value => value.SendCoreAsync(ServerEventTypes.ErrorEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
            await fixture.Hub.ResignMatchAsync(stored.Id.ToString());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MatchHubCommitsDoubleOfferBeforeNotification(bool failSend)
        {
            var fixture = await CreatePersistenceFixture();
            var notifications = CaptureStoredSessions(fixture.Client, fixture.Session.Id, ServerEventTypes.DoubleOfferedEvent, failSend);

            await fixture.Hub.OfferDoubleAsync(fixture.Session.Id.ToString());

            Assert.NotEmpty(notifications);
            Assert.All(notifications, match =>
            {
                Assert.True(Assert.IsAssignableFrom<IDoubleCubeMatchSession>(match).IsDoubleOfferPending);
                Assert.Equal(match.Player2.Id, match.GetGameSession(match.GameRound)!.ActivePlayer);
            });
            Assert.True(Assert.IsAssignableFrom<IDoubleCubeMatchSession>(_matchRepo.Get(fixture.Session.Id)).IsDoubleOfferPending);
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.SevenPointGame)]
        public async Task MatchHubReturnsEndTurnCommandIfNoLegalMovesReturned(MatchVariant variant, MatchModus modus, MatchType type)
        {
            var result = await SetupPlayerVsPlayerMatchSession(variant, modus, type, _player1Id, _player2Id);
            var matchSession = result.Item1;
            var hub1 = result.Item2;
            var hub2 = result.Item3;
            var matchIdStr = matchSession.Id.ToString();

            await using var setupLease = await _matchRepo.AcquireStateAsync(matchSession.Id);
            matchSession = setupLease.Get()!;
            var gameSession = matchSession.GetGameSession(matchSession.GameRound);
            Assert.NotNull(gameSession);

            if (gameSession.ActivePlayer == _player1Id)
            {
                // player 1 plays with white checkers
                gameSession.BoardModel.SetFields(BoardMocks.WhiteCannotMoveWithRoll);
                // we need to clear the dice rolls from the match hub start and its initial roll
                gameSession.DiceRolls.Clear();
                setupLease.Commit();
                await setupLease.DisposeAsync();
                await hub1.RollAsync(matchIdStr);
                matchSession = _matchRepo.Get(matchSession.Id);
                Assert.NotNull(matchSession);
                gameSession = matchSession.GetGameSession(matchSession.GameRound);
                Assert.NotNull(gameSession);
                Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
                var gameState = matchSession.GetGameState(_player1Id);
                Assert.Contains(ServerCommands.EndTurnCommand, gameState.AllowedCommands);
            }
            else if (gameSession.ActivePlayer == _player2Id)
            {
                // player 2 plays with black checkers
                gameSession.BoardModel.SetFields(BoardMocks.BlackCannotMoveWithRoll);
                // we need to clear the dice rolls from the match hub start and its initial roll
                gameSession.DiceRolls.Clear();
                setupLease.Commit();
                await setupLease.DisposeAsync();
                await hub2.RollAsync(matchIdStr);
                matchSession = _matchRepo.Get(matchSession.Id);
                Assert.NotNull(matchSession);
                gameSession = matchSession.GetGameSession(matchSession.GameRound);
                Assert.NotNull(gameSession);
                Assert.Equal(GamePhase.WaitingForEndTurn, gameSession.Phase);
                var gameState = matchSession.GetGameState(_player2Id);
                Assert.Contains(ServerCommands.EndTurnCommand, gameState.AllowedCommands);
            }
        }

        private IGameSessionModel GetGameSession(Guid matchId)
        {
            var matchSession = _matchRepo.Get(matchId);
            Assert.NotNull(matchSession);
            var gameSession = matchSession.GetGameSession(matchSession.GameRound);
            Assert.NotNull(gameSession);
            return gameSession;
        }

        private IMatchSessionModel GetMatchSession(Guid matchId)
        {
            var matchSession = _matchRepo.Get(matchId);
            if (matchSession == null)
            {
                Assert.True(_completedMatches.TryGetValue(matchId, out matchSession), "Match disappeared before its result was queued.");
            }
            Assert.NotNull(matchSession);
            return matchSession;
        }

        private IDoubleCubeMatchSession GetDoubleCubeMatchSession(Guid matchId)
        {
            var matchSession = _matchRepo.Get(matchId);
            var cubeSession = matchSession as IDoubleCubeMatchSession;
            return cubeSession!;
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame)]
        public async Task MatchHubCanPlayPlayerVsPlayerCanOfferAndAcceptDoublingCube(MatchVariant variant, MatchModus modus, MatchType type)
        {
            var result = await SetupPlayerVsPlayerMatchSession(variant, modus, type, _player1Id, _player2Id);
            var matchSession = result.Item1;
            var hub1 = result.Item2;
            var hub2 = result.Item3;
            var mockClients = result.Item4;
            var matchIdStr = matchSession.Id.ToString();
            var player1ConnectionId = matchSession.Player1.ConnectionId;
            var player2ConnectionId = matchSession.Player2.ConnectionId;

            var cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
            Assert.NotNull(cubeSession);

            var gameSession = GetGameSession(matchSession.Id);
            Assert.NotNull(gameSession);
            // the game setup with the hub already includes an initial roll, so we need to move it first
            var firstMs = gameSession.MoveSequences.First();

            if (gameSession.ActivePlayer == _player1Id)
            {
                // we need to player the opening dices first
                foreach (var move in firstMs.Moves)
                {
                    await hub1.MoveAsync(matchIdStr, move.From, move.To);
                }
                await hub1.EndTurnAsync(matchIdStr);

                cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
                Assert.NotNull(cubeSession);

                Assert.False(cubeSession.IsDoubleOfferPending);
                Assert.True(cubeSession.CanOfferDouble(_player2Id));
                await hub2.OfferDoubleAsync(matchIdStr);

                cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
                Assert.NotNull(cubeSession);

                // player1 gets double offered event without allowed commands
                mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.DoubleOfferedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
                // player2 gets double offered event with accept/decline as allowed commands
                mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.DoubleOfferedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
                Assert.True(cubeSession.IsDoubleOfferPending);
                await hub1.AcceptDoubleAsync(matchIdStr);

                cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
                Assert.NotNull(cubeSession);

                Assert.False(cubeSession.IsDoubleOfferPending);
            }
            else if (gameSession.ActivePlayer == _player2Id)
            {
                // we need to player the opening dices first
                foreach (var move in firstMs.Moves)
                {
                    var invertedMove = BoardBroker.InvertFromToMove(gameSession.Modus, move.From, move.To);
                    await hub2.MoveAsync(matchIdStr, invertedMove.Item1, invertedMove.Item2);
                }
                await hub2.EndTurnAsync(matchIdStr);

                cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
                Assert.NotNull(cubeSession);

                Assert.False(cubeSession.IsDoubleOfferPending);
                Assert.True(cubeSession.CanOfferDouble(_player1Id));
                await hub1.OfferDoubleAsync(matchIdStr);

                cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
                Assert.NotNull(cubeSession);

                // player1 gets double offered event without allowed commands
                mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.DoubleOfferedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
                // player2 gets double offered event with accept/decline as allowed commands
                mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.DoubleOfferedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
                Assert.True(cubeSession.IsDoubleOfferPending);
                await hub2.AcceptDoubleAsync(matchIdStr);

                cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
                Assert.NotNull(cubeSession);

                Assert.False(cubeSession.IsDoubleOfferPending);
            }

            Assert.Equal(2, cubeSession.GetDoublingCubeValue());
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame)]
        public async Task MatchHubCanPlayPlayerVsPlayerCanOfferAndDeclineDoublingCube(MatchVariant variant, MatchModus modus, MatchType type)
        {
            var result = await SetupPlayerVsPlayerMatchSession(variant, modus, type, _player1Id, _player2Id);
            var matchSession = result.Item1;
            var hub1 = result.Item2;
            var hub2 = result.Item3;
            var mockClients = result.Item4;
            var matchIdStr = matchSession.Id.ToString();
            var player1ConnectionId = matchSession.Player1.ConnectionId;
            var player2ConnectionId = matchSession.Player2.ConnectionId;

            var cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
            Assert.NotNull(cubeSession);

            var gameSession = matchSession.GetGameSession(matchSession.GameRound);
            Assert.NotNull(gameSession);

            // the game setup with the hub already includes an initial roll, so we need to move it first
            var firstMs = gameSession.MoveSequences.First();

            if (gameSession.ActivePlayer == _player1Id)
            {
                // we need to player the opening dices first
                foreach (var move in firstMs.Moves)
                {
                    await hub1.MoveAsync(matchIdStr, move.From, move.To);
                }
                await hub1.EndTurnAsync(matchIdStr);

                cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
                Assert.NotNull(cubeSession);

                Assert.False(cubeSession.IsDoubleOfferPending);
                Assert.True(cubeSession.CanOfferDouble(_player2Id));
                await hub2.OfferDoubleAsync(matchIdStr);

                cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
                Assert.NotNull(cubeSession);

                // player1 gets double offered event without allowed commands
                mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.DoubleOfferedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
                // player2 gets double offered event with accept/decline as allowed commands
                mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.DoubleOfferedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
                Assert.True(cubeSession.IsDoubleOfferPending);
                await hub1.DeclineDoubleAsync(matchIdStr);
            }
            else if (gameSession.ActivePlayer == _player2Id)
            {
                // we need to player the opening dices first
                foreach (var move in firstMs.Moves)
                {
                    var invertedMove = BoardBroker.InvertFromToMove(gameSession.Modus, move.From, move.To);
                    await hub2.MoveAsync(matchIdStr, invertedMove.Item1, invertedMove.Item2);
                }
                await hub2.EndTurnAsync(matchIdStr);

                cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
                Assert.NotNull(cubeSession);

                Assert.False(cubeSession.IsDoubleOfferPending);
                Assert.True(cubeSession.CanOfferDouble(_player1Id));
                await hub1.OfferDoubleAsync(matchIdStr);

                cubeSession = GetDoubleCubeMatchSession(matchSession.Id);
                Assert.NotNull(cubeSession);

                // player1 gets double offered event without allowed commands
                mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.DoubleOfferedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
                // player2 gets double offered event with accept/decline as allowed commands
                mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.DoubleOfferedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
                Assert.True(cubeSession.IsDoubleOfferPending);
                await hub2.DeclineDoubleAsync(matchIdStr);
            }

            matchSession = GetMatchSession(matchSession.Id);
            cubeSession = Assert.IsAssignableFrom<IDoubleCubeMatchSession>(matchSession);
            var declinedGameSession = matchSession.GetGameSession(1);
            Assert.NotNull(declinedGameSession);
            Assert.Equal(GamePhase.GameOver, declinedGameSession.Phase);
            Assert.Equal(GameResult.DoubleDeclined, declinedGameSession.Result.WinnerResult);
            Assert.Equal(GameResult.LostDoubleDeclined, declinedGameSession.Result.LoserResult);
            Assert.Equal(1, declinedGameSession.Result.Points);
            Assert.Equal(1, cubeSession.GetDoublingCubeValue());
            Assert.False(cubeSession.IsDoubleOfferPending);
            Assert.Equal(type == MatchType.CashGame, matchSession.IsMatchOver());
            Assert.Equal(type == MatchType.CashGame, _matchRepo.Get(matchSession.Id) == null);
            _workQueueService.Verify(c => c.EnqueueGameResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.SevenPointGame)]
        public async Task MatchHubCanPlayPlayerVsPlayerResignMatch(MatchVariant variant, MatchModus modus, MatchType type)
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

            var activeHub = gameSession.ActivePlayer == _player1Id ? hub1 : hub2;
            await activeHub.ResignMatchAsync(matchIdStr);
            matchSession = GetMatchSession(matchSession.Id);

            Assert.True(matchSession.IsMatchOver());
            Assert.Null(_matchRepo.Get(matchSession.Id));
            Assert.False(matchSession.CanStartNextGame());
            Assert.True(matchSession.GetGameSessions().All(gs => gs == null || gs.Result.WinnerResult == GameResult.Resign));
            Assert.True(matchSession.GetGameSessions().All(gs => gs == null || gs.Result.LoserResult == GameResult.LostResign));
            var playedSessions = matchSession.GetGameSessions().Where(gs => gs != null).ToList();
            if (type == MatchType.CashGame)
            {
                if (variant == MatchVariant.Tavli)
                {
                    Assert.True(playedSessions.Count(gs => gs?.Phase == GamePhase.GameOver) == 3);
                }
                else
                {
                    Assert.True(playedSessions.Count(gs => gs?.Phase == GamePhase.GameOver) == 1);
                }
            }
            else
            {
                Assert.Contains(playedSessions, gs => gs?.Phase == GamePhase.GameOver);
            }

            Assert.True(matchSession.Player2.Points > 0 || matchSession.Player1.Points > 0);
            Assert.True(matchSession.Player2.Points >= type.GetMaxPoints() || matchSession.Player1.Points >= type.GetMaxPoints());
            mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Never);
            mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Exactly(2));
            mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Never);
            mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Exactly(2));
            mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.ForceDisconnectEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Once);
            _workQueueService.Verify(c => c.EnqueueGameResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
            _workQueueService.Verify(c => c.EnqueueMatchResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()), Times.Once());
            _workQueueService.Verify(c => c.EnqueueStatProcessingAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()), Times.Once());
            if (modus == MatchModus.Ranked)
            {
                _workQueueService.Verify(c => c.EnqueueRatingProcessingAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()), Times.Once());
            }
        }

        [Fact]
        public async Task MatchHubConcludesMatchWhenTurnExpiresBeforeNextGameStarts()
        {
            var result = await SetupPlayerVsPlayerMatchSession(
                MatchVariant.Tavli,
                MatchModus.Normal,
                MatchType.CashGame,
                _player1Id,
                _player2Id);
            var matchSession = result.Item1;
            var hub1 = result.Item2;
            var mockClients = result.Item4;
            var groupName = result.groupName;
            var matchIdStr = matchSession.Id.ToString();
            var player1ConnectionId = matchSession.Player1.ConnectionId;
            var player2ConnectionId = matchSession.Player2.ConnectionId;
            var gameSession = matchSession.GetGameSession(matchSession.GameRound);
            Assert.NotNull(gameSession);

            IMatchSessionModel? concludedMatch = null;
            _workQueueService
                .Setup(service => service.EnqueueMatchResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()))
                .Callback<IMatchSessionModel, CancellationToken>((match, _) => concludedMatch = (IMatchSessionModel)match.Clone())
                .ThrowsAsync(new InvalidOperationException("match result queue unavailable"));

            await hub1.ResignGameAsync(matchIdStr);

            matchSession = _matchRepo.Get(matchSession.Id)!;
            gameSession = matchSession.GetGameSession(matchSession.GameRound)!;
            Assert.Equal(GamePhase.GameOver, gameSession.Phase);
            Assert.False(matchSession.IsMatchOver());

            await InvokeTurnTimeoutAsync(hub1, matchSession.Id, matchSession.Player1.Id);

            Assert.NotNull(concludedMatch);
            matchSession = concludedMatch;
            Assert.Equal(3, matchSession.GameRound);
            Assert.NotNull(matchSession.EndedAt);
            Assert.All(matchSession.GetGameSessions(), game =>
            {
                Assert.NotNull(game);
                Assert.Equal(GamePhase.GameOver, game.Phase);
            });
            Assert.Null(_matchRepo.Get(matchSession.Id));
            mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.AtLeastOnce);
            mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.AtLeastOnce);
            mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.AtLeastOnce);
            mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.AtLeastOnce);
            mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.ForceDisconnectEvent, It.IsAny<object[]>(), CancellationToken.None), Times.AtLeastOnce);
            _workQueueService.Verify(c => c.EnqueueMatchResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.SevenPointGame)]
        public async Task MatchHubCanPlayPlayerVsPlayerResignGameMatch(MatchVariant variant, MatchModus modus, MatchType type)
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

            var rounds = 0;
            do
            {
                Assert.True(++rounds <= 100, "Match did not finish within 100 resigned games.");
                gameSession = matchSession.GetGameSession(matchSession.GameRound);
                Assert.NotNull(gameSession);

                if (gameSession.ActivePlayer == _player1Id)
                {
                    await hub1.ResignGameAsync(matchIdStr);
                }
                else if (gameSession.ActivePlayer == _player2Id)
                {
                    await hub2.ResignGameAsync(matchIdStr);
                }

                matchSession = GetMatchSession(matchSession.Id);
                if (!matchSession.IsMatchOver())
                {
                    await hub1.StartGameAsync(matchIdStr);
                    await hub2.StartGameAsync(matchIdStr);
                    matchSession = GetMatchSession(matchSession.Id);
                }
            }
            while (!matchSession.IsMatchOver());

            Assert.True(matchSession.IsMatchOver());
            Assert.False(matchSession.CanStartNextGame());
            Assert.True(matchSession.GetGameSessions().Where(gs => gs != null).All(gs => gs?.Result.WinnerResult == GameResult.Resign));
            Assert.True(matchSession.GetGameSessions().Where(gs => gs != null).All(gs => gs?.Result.LoserResult == GameResult.LostResign));
            var playedSessions = matchSession.GetGameSessions().Where(gs => gs != null).ToList();
            var multiplier = 1;
            if (type == MatchType.CashGame)
            {
                if (variant == MatchVariant.Tavli)
                {
                    Assert.True(playedSessions.Count(gs => gs?.Phase == GamePhase.GameOver) == 3);
                }
                else
                {
                    Assert.True(playedSessions.Count(gs => gs?.Phase == GamePhase.GameOver) == 1);
                }
            }
            else
            {
                Assert.True(playedSessions.Count(gs => gs?.Phase == GamePhase.GameOver) >= 2);
                multiplier = 2;
            }

            Assert.True(matchSession.Player2.Points > 0 || matchSession.Player1.Points > 0);
            Assert.True(matchSession.Player2.Points >= type.GetMaxPoints() || matchSession.Player1.Points >= type.GetMaxPoints());
            var gameEndedCount = (playedSessions.Count - 1) * multiplier;
            mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), CancellationToken.None), gameEndedCount == 0 ? Times.Never() : Times.AtLeast(gameEndedCount));
            mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Exactly(2));
            mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), CancellationToken.None), gameEndedCount == 0 ? Times.Never() : Times.AtLeast(gameEndedCount));
            mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Exactly(2));
            mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.ForceDisconnectEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Once);
            _workQueueService.Verify(c => c.EnqueueGameResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(playedSessions.Count));
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.SevenPointGame)]
        public async Task MatchHubCanPlayPlayerVsPlayerMatch(MatchVariant variant, MatchModus modus, MatchType type)
        {
            var result = await SetupPlayerVsPlayerMatchSession(variant, modus, type, _player1Id, _player2Id);
            var matchSession = result.Item1;
            var hub1 = result.Item2;
            var hub2 = result.Item3;
            var mockClients = result.Item4;
            var groupName = result.groupName;
            var matchIdStr = matchSession.Id.ToString();
            Assert.NotNull(matchIdStr);
            var player1ConnectionId = matchSession.Player1.ConnectionId;
            var player2ConnectionId = matchSession.Player2.ConnectionId;

            var gameSession = matchSession.GetGameSession(matchSession.GameRound);
            Assert.NotNull(gameSession);

            var turns = 0;
            do
            {
                Assert.True(++turns <= 10000, "Match did not finish within 10000 turns.");
                gameSession = matchSession.GetGameSession(matchSession.GameRound);
                Assert.NotNull(gameSession);

                if (gameSession.ActivePlayer == _player1Id)
                {
                    matchSession = await ExecutePlayerTurnAsync(matchSession, hub1, hub2);
                }
                else if (gameSession.ActivePlayer == _player2Id)
                {
                    matchSession = await ExecutePlayerTurnAsync(matchSession, hub2, hub1);
                }
            }
            while (!matchSession.IsMatchOver());

            Assert.True(matchSession.IsMatchOver());
            Assert.False(matchSession.CanStartNextGame());
            var playedSessions = matchSession.GetGameSessions().Where(gs => gs != null).ToList();
            var multiplier = 1;
            if (type == MatchType.CashGame)
            {
                if (variant == MatchVariant.Tavli)
                {
                    Assert.True(playedSessions.Count(gs => gs?.Phase == GamePhase.GameOver) == 3);
                }
                else
                {
                    Assert.True(playedSessions.Count(gs => gs?.Phase == GamePhase.GameOver) == 1);
                }
            }
            else
            {
                Assert.True(playedSessions.Count(gs => gs?.Phase == GamePhase.GameOver) >= 2);
                multiplier = 2;
            }

            Assert.True(matchSession.Player2.Points > 0 || matchSession.Player1.Points > 0);
            Assert.True(matchSession.Player2.Points >= type.GetMaxPoints() || matchSession.Player1.Points >= type.GetMaxPoints());
            var gameEndedCount = (playedSessions.Count - 1) * multiplier;
            mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), CancellationToken.None), gameEndedCount == 0 ? Times.Never() : Times.AtLeast(gameEndedCount));
            mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.AtLeast(2));
            mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), CancellationToken.None), gameEndedCount == 0 ? Times.Never() : Times.AtLeast(gameEndedCount));
            mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.AtLeast(2));
            mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.ForceDisconnectEvent, It.IsAny<object[]>(), CancellationToken.None), Times.AtLeastOnce());
            _workQueueService.Verify(c => c.EnqueueGameResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(playedSessions.Count));
            _workQueueService.Verify(c => c.EnqueueMatchResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()), Times.Once());
            _workQueueService.Verify(c => c.EnqueueStatProcessingAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()), Times.Once());
            if (modus == MatchModus.Ranked)
            {
                _workQueueService.Verify(c => c.EnqueueRatingProcessingAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()), Times.Once());
            }
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchType.SevenPointGame)]
        public async Task MatchHubCanPlayPlayerVsBotMatch(MatchVariant variant, MatchType type)
        {
            var queueKey = new QueueKey(variant, MatchModus.Bot, type, BotLevel.Hard);
            var playerId = Guid.NewGuid();
            var matchId = await CreatePlayerVsBotMatchLobbyAsync(queueKey, playerId);
            var matchIdStr = matchId.ToString();

            var matchService = GetService(MatchModus.Bot);
            Assert.True(matchService.TryFindMatchLobby(matchId, out var lobby));
            Assert.NotNull(lobby);
            var groupName = lobby.GroupName;

            var mockClients = new Mock<IHubCallerClients>();
            var mockGroup = new Mock<IClientProxy>();
            _hub.Clients = mockClients.Object;
            _hub.Context = new HubCallerContextStub("human-" + Guid.NewGuid());
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
            mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.MatchLobbyFoundEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Once);
            // player receives event with roll command
            mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.MatchWaitingForStartEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Once);
            // bot player does not receive start game event
            mockClients.Verify(c => c.Client(botConnectionId).SendCoreAsync(ServerEventTypes.MatchWaitingForStartEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Never);
            // bot lobby is always instantly ready
            mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.MatchWaitingForStartEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Never);

            var startingRolls = 0;
            do
            {
                Assert.True(++startingRolls <= 100, "Starting rolls remained tied after 100 attempts.");
                await _hub.StartMatchAsync(matchIdStr);
                matchSession = GetMatchSession(matchId);
            }
            while (matchSession.Player1.StartDiceRoll == matchSession.Player2.StartDiceRoll);

            // if player won starting roll
            if (matchSession.Player1.StartDiceRoll > matchSession.Player2.StartDiceRoll)
            {
                mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.MatchStartedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Once);
                mockClients.Verify(c => c.Client(botConnectionId).SendCoreAsync(ServerEventTypes.MatchStartedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Never);
                mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.GameStartedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Once);
                mockClients.Verify(c => c.Client(botConnectionId).SendCoreAsync(ServerEventTypes.GameStartedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Never);
            }
            // if bot won starting roll
            else
            {
                mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.GameStateEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Exactly(3));
                mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.MatchStartedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Once);
                mockClients.Verify(c => c.Client(botConnectionId).SendCoreAsync(ServerEventTypes.MatchStartedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Never);
                mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.GameStartedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Once);
                mockClients.Verify(c => c.Client(botConnectionId).SendCoreAsync(ServerEventTypes.GameStartedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Never);
            }

            var gameSession = matchSession.GetGameSession(matchSession.GameRound);
            Assert.NotNull(gameSession);

            var turns = 0;
            do
            {
                Assert.True(++turns <= 10000, "Bot match did not finish within 10000 turns.");
                if (gameSession.Phase == GamePhase.GameOver)
                {
                    await _hub.StartGameAsync(matchIdStr);
                    matchSession = GetMatchSession(matchId);
                    gameSession = matchSession.GetGameSession(matchSession.GameRound);
                    Assert.NotNull(gameSession);
                }

                // bot offered a double
                if (matchSession is IDoubleCubeMatchSession { IsDoubleOfferPending: true })
                {
                    await _hub.AcceptDoubleAsync(matchIdStr);
                    matchSession = GetMatchSession(matchId);
                    gameSession = matchSession.GetGameSession(matchSession.GameRound)!;
                    if (gameSession.Phase == GamePhase.GameOver)
                    {
                        continue;
                    }
                }

                Assert.Equal(playerId, gameSession.ActivePlayer);
                if (gameSession.Phase == GamePhase.WaitingForRoll)
                {
                    await _hub.RollAsync(matchIdStr);
                    matchSession = GetMatchSession(matchId);
                    gameSession = matchSession.GetGameSession(matchSession.GameRound)!;
                }

                var ms = gameSession.MoveSequences.FirstOrDefault();
                if (ms != null)
                {
                    var moves = ms.Moves.ToArray();
                    foreach (var move in moves)
                    {
                        var isWhite = matchSession.Player1.Id == playerId;
                        if (isWhite)
                        {
                            await _hub.MoveAsync(matchIdStr, move.From, move.To);
                        }
                        else
                        {
                            var invertedMove = BoardBroker.InvertFromToMove(gameSession.Modus, move.From, move.To);
                            await _hub.MoveAsync(matchIdStr, invertedMove.Item1, invertedMove.Item2);
                        }
                        matchSession = GetMatchSession(matchId);
                        gameSession = matchSession.GetGameSession(matchSession.GameRound)!;
                        if (gameSession.Phase == GamePhase.GameOver)
                        {
                            break;
                        }
                    }
                }

                if (gameSession.Phase != GamePhase.GameOver)
                {
                    await _hub.EndTurnAsync(matchIdStr);
                    matchSession = GetMatchSession(matchId);
                    gameSession = matchSession.GetGameSession(matchSession.GameRound)!;
                }
            }
            while (!matchSession.IsMatchOver());

            Assert.True(matchSession.IsMatchOver());
            Assert.False(matchSession.CanStartNextGame());
            var playedSessions = matchSession.GetGameSessions().Where(gs => gs != null).ToList();
            if (type == MatchType.CashGame)
            {
                if (variant == MatchVariant.Tavli)
                {
                    Assert.True(playedSessions.Count(gs => gs?.Phase == GamePhase.GameOver) == 3);
                }
                else
                {
                    Assert.True(playedSessions.Count(gs => gs?.Phase == GamePhase.GameOver) == 1);
                }
            }
            else
            {
                Assert.True(playedSessions.Count(gs => gs?.Phase == GamePhase.GameOver) >= 2);
            }
            // bot will win every time :: wow the dumb human can win also
            Assert.True(matchSession.Player2.Points > 0 || matchSession.Player1.Points > 0);
            Assert.True(matchSession.Player2.Points >= type.GetMaxPoints() || matchSession.Player1.Points >= type.GetMaxPoints());
            var gameEndedCount = playedSessions.Count - 1;
            mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.GameEndedEvent, It.IsAny<object[]>(), CancellationToken.None), gameEndedCount == 0 ? Times.Never() : Times.AtLeast(gameEndedCount));
            mockClients.Verify(c => c.Client(playerConnectionId).SendCoreAsync(ServerEventTypes.MatchEndedEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Once);
            mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.ForceDisconnectEvent, It.IsAny<object[]>(), CancellationToken.None), Times.Once);
            _workQueueService.Verify(c => c.EnqueueGameResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(playedSessions.Count));
            _workQueueService.Verify(c => c.EnqueueMatchResultAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()), Times.Once());
            _workQueueService.Verify(c => c.EnqueueStatProcessingAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()), Times.Once());
            _workQueueService.Verify(c => c.EnqueueRatingProcessingAsync(It.IsAny<IMatchSessionModel>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Normal, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Ranked, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Ranked, MatchType.SevenPointGame)]
        public async Task MatchHubCanPlayPlayerVsPlayerCanUndoMoves(MatchVariant variant, MatchModus modus, MatchType type)
        {
            var result = await SetupPlayerVsPlayerMatchSession(variant, modus, type, _player1Id, _player2Id);
            var matchSession = result.Item1;
            var hub1 = result.Item2;
            var hub2 = result.Item3;
            var matchIdStr = matchSession.Id.ToString();

            var gameSession = matchSession.GetGameSession(matchSession.GameRound);
            Assert.NotNull(gameSession);

            var activeHub = gameSession.ActivePlayer == _player1Id ? hub1 : hub2;
            var isWhite = gameSession.ActivePlayer == matchSession.Player1.Id;
            var moves = gameSession.MoveSequences.First().Moves.ToArray();
            Assert.NotEmpty(moves);

            foreach (var move in moves)
            {
                var positions = isWhite
                    ? (move.From, move.To)
                    : BoardBroker.InvertFromToMove(gameSession.Modus, move.From, move.To);
                await activeHub.MoveAsync(matchIdStr, positions.Item1, positions.Item2);
            }

            Assert.NotEqual(gameSession.BoardModel, GetGameSession(matchSession.Id).BoardModel);
            foreach (var _ in moves)
            {
                await activeHub.UndoMoveAsync(matchIdStr);
            }

            var restoredGame = GetGameSession(matchSession.Id);
            Assert.Equal(gameSession.BoardModel, restoredGame.BoardModel);
            Assert.Equal(GamePhase.Moving, restoredGame.Phase);
            Assert.Equal(gameSession.DiceRolls.Select(roll => (roll.Roll, roll.Used)),
                restoredGame.DiceRolls.Select(roll => (roll.Roll, roll.Used)));
            Assert.Equal(gameSession.BoardModel.History.ToString(), restoredGame.BoardModel.History.ToString());
        }

        private async Task<IMatchSessionModel> ExecutePlayerTurnAsync(IMatchSessionModel matchSession, MatchLobbyHub activeHub, MatchLobbyHub otherHub)
        {
            var matchIdStr = matchSession.Id.ToString();

            if (matchSession is IDoubleCubeMatchSession cubeSession && cubeSession.IsDoubleOfferPending)
            {
                await activeHub.AcceptDoubleAsync(matchIdStr);
                matchSession = GetMatchSession(matchSession.Id);
            }

            var gameSession = matchSession.GetGameSession(matchSession.GameRound)!;
            if (gameSession.Phase == GamePhase.WaitingForRoll)
            {
                await activeHub.RollAsync(matchIdStr);
                matchSession = GetMatchSession(matchSession.Id);
                gameSession = matchSession.GetGameSession(matchSession.GameRound)!;
            }

            var ms = gameSession.MoveSequences.FirstOrDefault();
            if (ms != null)
            {
                var moves = ms.Moves.ToList();
                foreach (var move in moves)
                {
                    var isWhite = matchSession.Player1.Id == gameSession.ActivePlayer;

                    if (gameSession.GameOver(isWhite))
                    {
                        break;
                    }

                    if (isWhite)
                    {
                        await activeHub.MoveAsync(matchIdStr, move.From, move.To);
                    }
                    else
                    {
                        var invertedMove = BoardBroker.InvertFromToMove(gameSession.Modus, move.From, move.To);
                        await activeHub.MoveAsync(matchIdStr, invertedMove.Item1, invertedMove.Item2);
                    }
                    matchSession = GetMatchSession(matchSession.Id);
                    gameSession = matchSession.GetGameSession(matchSession.GameRound)!;
                }
            }

            if (gameSession.Phase != GamePhase.GameOver)
            {
                await activeHub.EndTurnAsync(matchIdStr);
                matchSession = GetMatchSession(matchSession.Id);
                gameSession = matchSession.GetGameSession(matchSession.GameRound)!;
            }
            if (gameSession.Phase == GamePhase.GameOver && !matchSession.IsMatchOver())
            {
                if (gameSession.Result.Equals(GameResultModel.Draw()))
                {
                    Assert.Null(gameSession.Result.WinnerId);
                    Assert.True(gameSession.Result.IsConcluded);
                    Assert.Equal(GameResult.Draw, gameSession.Result.WinnerResult);
                    Assert.Equal(GameResult.Draw, gameSession.Result.LoserResult);
                    Assert.Equal(0, gameSession.Result.Points);
                    Assert.True(gameSession.Result.IsDraw);
                }

                await otherHub.StartGameAsync(matchIdStr);
                await activeHub.StartGameAsync(matchIdStr);
                matchSession = GetMatchSession(matchSession.Id);
            }
            return matchSession;
        }

        private static async Task InvokeTurnTimeoutAsync(MatchLobbyHub hub, Guid matchId, Guid playerId)
        {
            var timeoutMethod = typeof(MatchLobbyHub).GetMethod(
                "HandleTurnTimeoutAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(timeoutMethod);

            var timeoutTask = timeoutMethod.Invoke(hub, [matchId, playerId]) as Task;
            Assert.NotNull(timeoutTask);
            await timeoutTask;
        }

        private async Task<(IMatchSessionModel, MatchLobbyHub, MatchLobbyHub, Mock<IHubCallerClients>, string groupName)> SetupPlayerVsPlayerMatchSession(
            MatchVariant variant,
            MatchModus modus,
            MatchType type,
            Guid player1Id,
            Guid player2Id)
        {
            var queueKey = new QueueKey(variant, modus, type, BotLevel.Hard);
            var matchId = await CreatePlayerVsPlayerMatchLobbyAsync(queueKey, player1Id, player2Id);
            var matchIdStr = matchId.ToString();
            var matchService = GetService(modus);

            Assert.True(matchService.TryFindMatchLobby(matchId, out var lobby));
            Assert.NotNull(lobby);
            var groupName = lobby.GroupName;

            var mockClients = new Mock<IHubCallerClients>();
            var mockGroups = new Mock<IGroupManager>();
            var mockGroup = new Mock<IClientProxy>();
            var dice = new Mock<IDiceService>();
            dice.SetupSequence(service => service.Roll(1, 6)).Returns([2]).Returns([1]);
            var diceFactory = new Mock<IDiceServiceFactory>();
            diceFactory.Setup(factory => factory.Create(It.IsAny<DiceServiceType>())).Returns(dice.Object);

            // client 1
            var player1ConnectionId = "human-" + Guid.NewGuid();
            var context1 = new HubCallerContextStub(player1ConnectionId);
            var hub1 = new MatchLobbyHub(BuildServiceProvider(matchService, diceFactory.Object))
            {
                Clients = mockClients.Object,
                Groups = mockGroups.Object,
                Context = context1
            };

            // client 2
            var player2ConnectionId = "human-" + Guid.NewGuid();
            var context2 = new HubCallerContextStub(player2ConnectionId);
            var hub2 = new MatchLobbyHub(BuildServiceProvider(matchService, diceFactory.Object))
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

            mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.MatchLobbyWaitingEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
            mockClients.Verify(c => c.Group(groupName).SendCoreAsync(ServerEventTypes.MatchLobbyFoundEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
            mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.MatchWaitingForStartEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.MatchWaitingForStartEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

            await hub1.StartMatchAsync(matchIdStr);
            await hub2.StartMatchAsync(matchIdStr);

            matchSession = _matchRepo.Get(matchId)!;
            Assert.NotNull(matchSession);

            // only called once for the first start match command call for the second player
            mockClients.Verify(c => c.Caller.SendCoreAsync(ServerEventTypes.MatchWaitingEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
            // the start match command exposes a match start event for the current match state
            mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.MatchStartedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.MatchStartedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            // and simultaneously a game started event for the initial board state including the starting rolls
            mockClients.Verify(c => c.Client(player1ConnectionId).SendCoreAsync(ServerEventTypes.GameStartedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            mockClients.Verify(c => c.Client(player2ConnectionId).SendCoreAsync(ServerEventTypes.GameStartedEvent, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

            // we ensure that one player has a higher starting roll than the other
            Assert.NotEqual(matchSession.Player1.StartDiceRoll, matchSession.Player2.StartDiceRoll);
            Assert.NotNull(matchSession.Player1.StartDiceRoll);
            Assert.NotNull(matchSession.Player2.StartDiceRoll);
            Assert.True(matchSession.Player1.StartDiceRoll > matchSession.Player2.StartDiceRoll || matchSession.Player2.StartDiceRoll > matchSession.Player1.StartDiceRoll);

            // the match start command should clean up the match lobby
            var matchmakingService = GetService(modus);
            var found = matchmakingService.TryFindMatchLobby(matchId, out var startedLobby);
            Assert.False(found);
            Assert.Null(startedLobby);

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

        private async Task<(IMatchSessionModel Session, MatchLobbyHub Hub, Mock<ISingleClientProxy> Client)> CreatePersistenceFixture(
            bool startMatch = true,
            bool player2Starts = false,
            MatchModus modus = MatchModus.Normal,
            IBotService? botService = null,
            MatchType type = MatchType.SevenPointGame,
            bool botOpponent = false)
        {
            await using var setupLease = await _matchRepo.AcquireStateAsync(Guid.NewGuid());
            var session = setupLease.Create(new QueueKey(MatchVariant.Backgammon, modus, type, BotLevel.Hard));
            var player1 = new PlayerConnection(_player1Id);
            player1.SetConnectionId("player1");
            var player2 = new PlayerConnection(_player2Id);
            player2.SetConnectionId(modus == MatchModus.Bot || botOpponent ? WellKnownBotPlayers.ConnectionId : "player2");
            session.JoinSession(player1);
            session.JoinSession(player2);
            if (startMatch)
            {
                session.Player1.SetStartDiceRoll(2);
                session.Player2.SetStartDiceRoll(1);
                session.Player1.AcceptNextGame();
                session.Player2.AcceptNextGame();
                session.StartMatch(player2Starts ? player2.Id : player1.Id);
            }
            Assert.True(setupLease.Commit());

            var dice = new Mock<IDiceService>();
            dice.SetupSequence(service => service.Roll(1, 6)).Returns([2]).Returns([1]);
            var diceFactory = new Mock<IDiceServiceFactory>();
            diceFactory.Setup(factory => factory.Create(It.IsAny<DiceServiceType>())).Returns(dice.Object);
            var client = new Mock<ISingleClientProxy>();
            var clients = new Mock<IHubCallerClients>();
            clients.Setup(value => value.Client(It.IsAny<string>())).Returns(client.Object);
            clients.Setup(value => value.Caller).Returns(client.Object);
            clients.Setup(value => value.Group(It.IsAny<string>())).Returns(client.Object);
            var hub = new MatchLobbyHub(BuildServiceProvider(GetService(modus), diceFactory.Object, botService))
            {
                Clients = clients.Object,
                Groups = new Mock<IGroupManager>().Object,
                Context = new HubCallerContextStub(player2Starts ? player2.ConnectionId! : player1.ConnectionId!)
            };
            return (session, hub, client);
        }

        private List<IMatchSessionModel> CaptureStoredSessions(Mock<ISingleClientProxy> client, Guid matchId, string eventName, bool failSend = false)
        {
            var snapshots = new List<IMatchSessionModel>();
            client.Setup(value => value.SendCoreAsync(eventName, It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Callback(() => snapshots.Add(_matchRepo.Get(matchId)!))
                .Returns(() => failSend ? Task.FromException(new InvalidOperationException("Client send failed.")) : Task.CompletedTask);
            return snapshots;
        }

        private ServiceProvider BuildServiceProvider(IMatchmakingService matchmakingService, IDiceServiceFactory? diceFactory = null, IBotService? botService = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton(_workQueueService.Object);
            services.AddSingleton(matchmakingService);
            services.AddSingleton(_matchRepo);
            services.AddSingleton(diceFactory ?? _diceFactory);
            services.AddKeyedSingleton(WellKnownBotServices.WildBg, _wildBgBot);
            services.AddKeyedSingleton(WellKnownBotServices.Mars, botService ?? _marsBot);
            services.AddSingleton(_playerConnRepo);
            services.AddSingleton(_cancellationTokenService);
            _ = services.AddSingleton(new Mock<IHubContext<MatchLobbyHub>>().Object);
            return services.BuildServiceProvider();
        }

        private IMatchmakingService GetService(MatchModus modus)
        {
            if (modus == MatchModus.Normal)
            {
                return _normalService;
            }
            else if (modus == MatchModus.Ranked)
            {
                return _rankedService;
            }
            else if (modus == MatchModus.Bot)
            {
                return _botMatchService;
            }
            throw new InvalidOperationException("not good");
        }

        public class HubCallerContextStub : HubCallerContext
        {
            private readonly string _connectionId;
            private readonly ClaimsPrincipal? _user;

            public HubCallerContextStub(string connectionId, Guid? playerId = null, Guid? matchId = null)
            {
                _connectionId = connectionId;
                if (playerId.HasValue && matchId.HasValue)
                {
                    _user = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim("playerId", playerId.Value.ToString()),
                        new Claim("matchId", matchId.Value.ToString())
                    }));
                }
            }

            public override string ConnectionId => _connectionId;

            public override string? UserIdentifier => null;

            public override ClaimsPrincipal? User => _user;

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
