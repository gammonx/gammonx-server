using GammonX.Engine.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Repository;
using GammonX.Server.Services;

using Microsoft.AspNetCore.SignalR;

using Moq;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests
{
    public class MatchLobbyCleanupTests
    {
        private readonly PlayerConnectionRepository _connections = new();
        private readonly MatchSessionRepository _matches = new(new MatchSessionFactory(new GameSessionFactory(new DiceServiceFactory())));
        private readonly CancellationTokenServiceImpl _timers = new();
        private readonly Mock<IClientProxy> _client = new();
        private readonly Mock<IGroupManager> _groups = new();

        private async Task<(IMatchmakingService Mode, MatchLobby Lobby, MatchLobbyCleanupService Cleanup, List<Guid> QueueIds)> CreateLobby(MatchModus modus)
        {
            var repository = new Mock<IRepositoryClient>();
            repository.Setup(value => value.GetRatingAsync(It.IsAny<Guid>(), It.IsAny<MatchVariant>(), It.IsAny<MatchType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlayerRatingResponseContract { Rating = 1200 });
            IMatchmakingService mode = modus switch
            {
                MatchModus.Normal => new NormalMatchmakingService(_connections),
                MatchModus.Ranked => new RankedMatchmakingService(_connections, repository.Object),
                _ => new BotMatchmakingService(_connections)
            };
            var key = new QueueKey(MatchVariant.Backgammon, modus, MatchType.SevenPointGame, BotLevel.Hard);
            var entry = await mode.JoinQueueAsync(Guid.NewGuid(), key);
            var queueIds = new List<Guid> { entry.Id };
            if (modus == MatchModus.Normal)
            {
                var opponent = await mode.JoinQueueAsync(Guid.NewGuid(), key);
                queueIds.Add(opponent.Id);
            }
            await mode.MatchQueuedPlayersAsync();
            Assert.True(mode.TryFindMatchLobby(entry.Id, out var lobby));
            var clients = new Mock<IHubClients>();
            clients.Setup(value => value.Group(It.IsAny<string>())).Returns(_client.Object);
            var hub = new Mock<IHubContext<MatchLobbyHub>>();
            hub.SetupGet(value => value.Clients).Returns(clients.Object);
            hub.SetupGet(value => value.Groups).Returns(_groups.Object);
            return (mode, lobby!, new MatchLobbyCleanupService(mode, _matches, _connections, _timers, hub.Object), queueIds);
        }

        [Theory]
        [InlineData(MatchModus.Normal)]
        [InlineData(MatchModus.Ranked)]
        [InlineData(MatchModus.Bot)]
        public async Task UnconnectedLobbiesExpireOnceWithoutRemovingSharedBots(MatchModus modus)
        {
            // Arrange
            var fixture = await CreateLobby(modus);
            var lobby = fixture.Lobby;
            var deadline = lobby.CreatedAtUtc + MatchLobby.StartTimeout;

            // Act / Assert: polling does not extend the creation-based deadline.
            await fixture.Cleanup.CleanupExpiredAsync(fixture.Mode, deadline.AddTicks(-1));
            Assert.True(fixture.Mode.TryFindMatchLobby(lobby.MatchId, out _));
            await fixture.Cleanup.CleanupExpiredAsync(fixture.Mode, deadline);
            await fixture.Cleanup.CleanupExpiredAsync(fixture.Mode, deadline);

            Assert.Empty(fixture.Mode.GetMatchLobbies());
            Assert.Null(_matches.Get(lobby.MatchId));
            Assert.Null(_connections.Get(lobby.Player1.Id));
            if (lobby.Player2 is { IsBot: true } bot)
            {
                Assert.Same(bot, _connections.Get(bot.Id));
            }
            else if (lobby.Player2 != null)
            {
                Assert.Null(_connections.Get(lobby.Player2.Id));
            }
            _client.Verify(value => value.SendCoreAsync(ServerEventTypes.ForceDisconnectEvent,
                It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public async Task ExpiryCleansConnectedOrPartiallyJoinedLobbyAndBothTimerCategories(int joinedPlayers)
        {
            // Arrange
            var fixture = await CreateLobby(MatchModus.Normal);
            var lobby = fixture.Lobby;
            lobby.Player1.SetConnectionId("one");
            lobby.Player2!.SetConnectionId("two");
            if (joinedPlayers > 0)
            {
                await using var lease = await _matches.AcquireStateAsync(lobby.MatchId);
                var session = lease.Create(lobby.QueueKey);
                session.JoinSession(lobby.Player1);
                if (joinedPlayers == 2)
                {
                    session.JoinSession(lobby.Player2);
                }
                Assert.True(lease.Commit());
            }
            using var turn = new CancellationTokenSource();
            using var disconnect = new CancellationTokenSource();
            _timers.Store(new CancellationTokenKey { MatchId = lobby.MatchId, PlayerId = lobby.Player1.Id, Category = CancellationTokenCategory.Turn }, turn);
            _timers.Store(new CancellationTokenKey { MatchId = lobby.MatchId, PlayerId = lobby.Player2.Id, Category = CancellationTokenCategory.Disconnect }, disconnect);

            // Act
            await fixture.Cleanup.CleanupExpiredAsync(fixture.Mode, lobby.CreatedAtUtc + MatchLobby.StartTimeout);

            // Assert
            Assert.Empty(fixture.Mode.GetMatchLobbies());
            Assert.Null(_matches.Get(lobby.MatchId));
            Assert.Null(_connections.Get(lobby.Player1.Id));
            Assert.Null(_connections.Get(lobby.Player2.Id));
            Assert.True(turn.IsCancellationRequested);
            Assert.True(disconnect.IsCancellationRequested);
            _groups.Verify(value => value.RemoveFromGroupAsync("one", lobby.GroupName, It.IsAny<CancellationToken>()), Times.Once);
            _groups.Verify(value => value.RemoveFromGroupAsync("two", lobby.GroupName, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MatchStartingWhileExpiryWaitsForLeaseIsPreserved()
        {
            // Arrange
            var fixture = await CreateLobby(MatchModus.Normal);
            var lobby = fixture.Lobby;
            var lease = await _matches.AcquireStateAsync(lobby.MatchId);
            var session = lease.Create(lobby.QueueKey);
            lobby.Player1.SetConnectionId("one");
            lobby.Player2!.SetConnectionId("two");
            session.JoinSession(lobby.Player1);
            session.JoinSession(lobby.Player2);
            using var timer = new CancellationTokenSource();
            var key = new CancellationTokenKey { MatchId = lobby.MatchId, PlayerId = lobby.Player1.Id, Category = CancellationTokenCategory.Turn };
            _timers.Store(key, timer);
            var expiry = fixture.Cleanup.CleanupExpiredAsync(fixture.Mode, lobby.CreatedAtUtc + MatchLobby.StartTimeout);
            Assert.False(expiry.IsCompleted);

            // Act
            session.Player1.SetStartDiceRoll(6);
            session.Player2.SetStartDiceRoll(1);
            session.Player1.AcceptNextGame();
            session.Player2.AcceptNextGame();
            session.StartMatch(session.Player1.Id);
            Assert.True(lease.Commit());
            await lease.DisposeAsync();
            await expiry.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

            // Assert
            Assert.False(fixture.Mode.TryFindMatchLobby(lobby.MatchId, out _));
            Assert.All(fixture.QueueIds, id => Assert.False(fixture.Mode.TryFindMatchLobby(id, out _)));
            var started = _matches.Get(lobby.MatchId)!;
            Assert.NotNull(started.StartedAt);
            Assert.False(started.IsMatchOver());
            Assert.Same(lobby.Player1, _connections.Get(lobby.Player1.Id));
            Assert.Same(lobby.Player2, _connections.Get(lobby.Player2.Id));
            Assert.True(_timers.TryGet(key, out var current));
            Assert.Same(timer, current);
            Assert.False(timer.IsCancellationRequested);
            _groups.VerifyNoOtherCalls();
            _client.Verify(value => value.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task FailedNotificationDoesNotPreventCleanup()
        {
            // Arrange
            var fixture = await CreateLobby(MatchModus.Bot);
            _client.Setup(value => value.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Socket unavailable."));

            // Act
            await fixture.Cleanup.CleanupExpiredAsync(fixture.Mode, fixture.Lobby.CreatedAtUtc + MatchLobby.StartTimeout);

            // Assert
            Assert.Empty(fixture.Mode.GetMatchLobbies());
            Assert.Null(_connections.Get(fixture.Lobby.Player1.Id));
        }
    }
}
