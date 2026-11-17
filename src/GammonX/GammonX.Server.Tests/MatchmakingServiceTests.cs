using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Repository;
using GammonX.Server.Services;

using Moq;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests
{
    public class MatchmakingServiceTests
    {
        [Theory]
        [InlineData(MatchModus.Normal, false)]
        [InlineData(MatchModus.Ranked, false)]
        [InlineData(MatchModus.Bot, true)]
        [InlineData(MatchModus.Ranked, true)]
        public async Task LobbyIndexResolvesAllIdsAndRemovesOnlyTheSelectedMatch(MatchModus modus, bool botOpponent)
        {
            // Arrange
            var connections = new PlayerConnectionRepository();
            IMatchmakingService mode = modus switch
            {
                MatchModus.Normal => new NormalMatchmakingService(connections),
                MatchModus.Ranked => new RankedMatchmakingService(connections, new Mock<IRepositoryClient>().Object),
                _ => new BotMatchmakingService(connections)
            };
            var matcher = new CompositeMatchmakingService(connections);
            matcher.AddService(modus, mode);
            var key = new QueueKey(MatchVariant.Backgammon, modus, MatchType.FivePointGame, BotLevel.Hard);
            var first = await CreateLobby();
            var second = await CreateLobby();
            Assert.Equal(2, matcher.GetMatchLobbies().Length);
            var unknownId = Guid.NewGuid();
            Assert.False(matcher.TryFindMatchLobby(unknownId, out var missing));
            Assert.Null(missing);
            Assert.False(matcher.TryRemoveMatchLobby(unknownId));

            // Act: competing removals must remove both queue aliases exactly once.
            var removals = await Task.WhenAll(Enumerable.Range(0, 16)
                .Select(_ => Task.Run(() => matcher.TryRemoveMatchLobby(first.Lobby.MatchId))));

            // Assert
            Assert.Single(removals, removed => removed);
            foreach (var id in first.QueueIds.Append(first.Lobby.MatchId))
            {
                Assert.False(matcher.TryFindMatchLobby(id, out var removed));
                Assert.Null(removed);
            }
            foreach (var id in second.QueueIds.Append(second.Lobby.MatchId))
            {
                Assert.True(matcher.TryFindMatchLobby(id, out var remaining));
                Assert.Same(second.Lobby, remaining);
            }
            Assert.Same(second.Lobby, Assert.Single(matcher.GetMatchLobbies()));

            async Task<(MatchLobby Lobby, Guid[] QueueIds)> CreateLobby()
            {
                var entry1 = await matcher.JoinQueueAsync(Guid.NewGuid(), key);
                var ids = new List<Guid> { entry1.Id };
                if (!botOpponent)
                {
                    var entry2 = await matcher.JoinQueueAsync(Guid.NewGuid(), key);
                    ids.Add(entry2.Id);
                }
                await matcher.MatchQueuedPlayersAsync();
                Assert.True(matcher.TryFindMatchLobby(entry1.Id, out var lobby));
                Assert.NotNull(lobby);
                foreach (var id in ids.Append(lobby.MatchId))
                {
                    Assert.True(matcher.TryFindMatchLobby(id, out var byId));
                    Assert.Same(lobby, byId);
                }
                foreach (var queueId in ids)
                {
                    Assert.False(matcher.TryRemoveMatchLobby(queueId));
                    Assert.True(matcher.TryFindMatchLobby(queueId, out var byQueueId));
                    Assert.Same(lobby, byQueueId);
                }

                return (lobby, ids.ToArray());
            }
        }

        [Theory]
        [InlineData("match-to-match")]
        [InlineData("match-to-queue")]
        [InlineData("queue-to-match")]
        [InlineData("queue-to-queue")]
        [InlineData("same-registration")]
        [InlineData("duplicate-queue")]
        public void LobbyIdCollisionDoesNotPartiallyRegisterLobby(string collision)
        {
            // Arrange
            var connections = new PlayerConnectionRepository();
            var matcher = new TestMatchmakingService(connections);
            var key = new QueueKey(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame, BotLevel.Unknown);
            var existingEntry = CreateQueueEntry(key, 1500);
            var existingLobby = new MatchLobby(Guid.NewGuid(), key, connections.GetOrCreate(existingEntry.PlayerId));
            matcher.Register(existingLobby, existingEntry);

            var matchId = collision switch
            {
                "match-to-match" => existingLobby.MatchId,
                "match-to-queue" => existingEntry.Id,
                _ => Guid.NewGuid()
            };
            var queueId = collision switch
            {
                "queue-to-match" => existingLobby.MatchId,
                "queue-to-queue" => existingEntry.Id,
                "same-registration" => matchId,
                _ => Guid.NewGuid()
            };
            var entry = new QueueEntry(queueId, Guid.NewGuid(), key, DateTime.UtcNow, 1500);
            var lobby = new MatchLobby(matchId, key, connections.GetOrCreate(entry.PlayerId));
            var entries = collision == "duplicate-queue" ? new[] { entry, entry } : new[] { entry };

            // Act
            Assert.Throws<InvalidOperationException>(() => matcher.Register(lobby, entries));

            // Assert
            Assert.Same(existingLobby, Assert.Single(matcher.GetMatchLobbies()));
            foreach (var id in new[] { existingLobby.MatchId, existingEntry.Id })
            {
                Assert.True(matcher.TryFindMatchLobby(id, out var found));
                Assert.Same(existingLobby, found);
            }

            foreach (var id in new[] { matchId, queueId }.Except(new[] { existingLobby.MatchId, existingEntry.Id }))
            {
                Assert.False(matcher.TryFindMatchLobby(id, out var found));
                Assert.Null(found);
            }
        }

        [Fact]
        public async Task RankedJoinUsesRatingForRequestedMatchType()
        {
            var playerId = Guid.NewGuid();
            var repository = new Mock<IRepositoryClient>();

            repository
                .Setup(value => value.GetRatingAsync(
                    playerId,
                    MatchVariant.Tavli,
                    MatchType.FivePointGame,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PlayerRatingResponseContract { Rating = 1375 });
            
            var service = new RankedMatchmakingService(new PlayerConnectionRepository(), repository.Object);
            var queueKey = new QueueKey(MatchVariant.Tavli, MatchModus.Ranked, MatchType.FivePointGame, BotLevel.Hard);

            var entry = await service.JoinQueueAsync(playerId, queueKey);

            Assert.Equal(1375, entry.CurrentRating);
            repository.VerifyAll();
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
        [InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.SevenPointGame)]
        public async Task ConcurrentMatchingDoesNotDoubleMatchPlayers(MatchVariant variant, MatchModus modus, MatchType type)
        {
            // setup services
            var playerConnRepo = new PlayerConnectionRepository();
            var rankedMatcher = new RankedMatchmakingService(playerConnRepo, new SimpleRepositoryClient());
            var normalMatcher = new NormalMatchmakingService(playerConnRepo);
            var botMatcher = new BotMatchmakingService(playerConnRepo);
            var matcher = new CompositeMatchmakingService(playerConnRepo);
            matcher.AddService(MatchModus.Ranked, rankedMatcher);
            matcher.AddService(MatchModus.Normal, normalMatcher);
            matcher.AddService(MatchModus.Bot, botMatcher);

            var queueKey = new QueueKey(variant, modus, type, BotLevel.Hard);

            const int playerCount = 100;

            // we only want a rating increase up to 50 (first search range)
            var entries = Enumerable.Range(0, playerCount).Select(i => CreateQueueEntry(queueKey, 1500 + (double)i / 2));

            foreach (var entry in entries)
            {
                matcher.Enqueue(entry);
            }

            // run matcher concurrently
            var tasks = Enumerable.Range(0, 8)
                .Select(_ => Task.Run(async () => await matcher.MatchQueuedPlayersAsync()))
                .ToArray();

            await Task.WhenAll(tasks);

            // assert
            var matchLobbies = matcher.GetMatchLobbies();
            var matchedPlayers = matchLobbies                
                .SelectMany(l => new[] { l.Player1, l.Player2 })
                .Where(player => player != null && !player.IsBot)
                .Select(player => player!.Id)
                .ToList();

            // no duplicates
            Assert.True(matchedPlayers.Count <= playerCount);
            Assert.Equal(matchLobbies.Length, matchLobbies.Select(lobby => lobby.MatchId).Distinct().Count());
            Assert.Equal(matchedPlayers.Count, matchedPlayers.Distinct().Count());

            // no missing players
            var unmatched = matcher.GetQueueEntries().Select(e => e.PlayerId);

            Assert.Empty(matchedPlayers.Intersect(unmatched));
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
        [InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.CashGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Backgammon, MatchModus.Bot, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavla, MatchModus.Bot, MatchType.SevenPointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.CashGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.FivePointGame)]
        [InlineData(MatchVariant.Tavli, MatchModus.Bot, MatchType.SevenPointGame)]
        public async Task ConcurrentEnqueueDoesNotLosePlayers(MatchVariant variant, MatchModus modus, MatchType type)
        {
            // setup services
            var playerConnRepo = new PlayerConnectionRepository();
            var rankedMatcher = new RankedMatchmakingService(playerConnRepo, new SimpleRepositoryClient());
            var normalMatcher = new NormalMatchmakingService(playerConnRepo);
            var botMatcher = new BotMatchmakingService(playerConnRepo);
            var matcher = new CompositeMatchmakingService(playerConnRepo);
            matcher.AddService(MatchModus.Ranked, rankedMatcher);
            matcher.AddService(MatchModus.Normal, normalMatcher);
            matcher.AddService(MatchModus.Bot, botMatcher);

            var queueKey = new QueueKey(variant, modus, type, BotLevel.Hard);

            var initialEntries = Enumerable.Range(0, 20)
                .Select(_ => CreateQueueEntry(queueKey, 1500))
                .ToList();

            foreach (var entry in initialEntries)
            {
                matcher.Enqueue(entry);
            }

            var matchTask = Task.Run(matcher.MatchQueuedPlayersAsync, TestContext.Current.CancellationToken);

            var enqueueTask = Task.Run(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    matcher.Enqueue(CreateQueueEntry(queueKey, 1500));
                }
            },
            TestContext.Current.CancellationToken);

            await Task.WhenAll(matchTask, enqueueTask);

            var lobbies = matcher.GetMatchLobbies();
            var entries = matcher.GetQueueEntries();

            var matchedPlayerCount = lobbies.Sum(lobby =>
                (lobby.Player1.IsBot ? 0 : 1) + (lobby.Player2 is { IsBot: false } ? 1 : 0));
            Assert.Equal(40, entries.Length + matchedPlayerCount);
        }

        [Fact]
        public async Task LongWaitingPlayerEventuallyMatches()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new RankedMatchmakingService(playerConnRepo, new SimpleRepositoryClient());
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame, BotLevel.Hard);

            var oldPlayer = CreateQueueEntry(queueKey, 1200);
            oldPlayer.EnqueuedAtUtc = DateTime.UtcNow.AddSeconds(-60);

            var newPlayer = CreateQueueEntry(queueKey, 1600);
            newPlayer.EnqueuedAtUtc = DateTime.UtcNow.AddSeconds(-60);

            matcher.Enqueue(oldPlayer);
            matcher.Enqueue(newPlayer);

            await matcher.MatchQueuedPlayersAsync();

            var lobbies = matcher.GetMatchLobbies();
            // Both players share one match lobby.
            Assert.Single(lobbies);
        }

        [Fact]
        public void LastSeenTimeStampIsUpdated()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new RankedMatchmakingService(playerConnRepo, new SimpleRepositoryClient());
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame, BotLevel.Hard);
            var entry = CreateQueueEntry(queueKey, 1200);
            var lastSeenUtc = entry.LastSeenUtc;
            matcher.Enqueue(entry);

            matcher.TouchQueueEntry(entry.Id);

            Assert.NotEqual(lastSeenUtc, entry.LastSeenUtc);
        }

        [Fact]
        public void QueueCleansUpExpiredEntries()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new RankedMatchmakingService(playerConnRepo, new SimpleRepositoryClient());
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame, BotLevel.Hard);
            var entry = CreateQueueEntry(queueKey, 1200);
            matcher.Enqueue(entry);

            var entries= matcher.GetQueueEntries();
            Assert.Single(entries);

            matcher.CleanupExpiredQueueEntries(TimeSpan.FromSeconds(1));
            entries = matcher.GetQueueEntries();
            Assert.Single(entries);

            matcher.CleanupExpiredQueueEntries(TimeSpan.FromTicks(1));
            entries = matcher.GetQueueEntries();
            Assert.Empty(entries);
        }

        [Fact]
        public async Task DuplicatePlayerEnqueueThrows()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new NormalMatchmakingService(playerConnRepo);
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame, BotLevel.Hard);
            var playerId = Guid.NewGuid();

            var entry1 = new QueueEntry(Guid.NewGuid(), playerId, queueKey, DateTime.UtcNow, 1500);
            matcher.Enqueue(entry1);

            // we attempt to enqueue same player again
            _ = new QueueEntry(Guid.NewGuid(), playerId, queueKey, DateTime.UtcNow, 1500);
            
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => matcher.JoinQueueAsync(playerId, queueKey)
            );
            Assert.Contains("already part of a match lobby queue", exception.Message);
        }

        [Fact]
        public async Task SinglePlayerCannotMatch()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new NormalMatchmakingService(playerConnRepo);
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame, BotLevel.Hard);

            // we enqueue single player
            var entry = CreateQueueEntry(queueKey, 1500);
            matcher.Enqueue(entry);

            await matcher.MatchQueuedPlayersAsync();

            // player should still be in queue
            var entries = matcher.GetQueueEntries();
            Assert.Single(entries);
            
            var lobbies = matcher.GetMatchLobbies();
            Assert.Empty(lobbies);
        }

        [Fact]
        public async Task NoMatchLobbiesDuplicates()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new NormalMatchmakingService(playerConnRepo);
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame, BotLevel.Hard);

            // we enqueue 10 players
            for (int i = 0; i < 10; i++)
            {
                matcher.Enqueue(CreateQueueEntry(queueKey, 1500));
            }

            await matcher.MatchQueuedPlayersAsync();

            var lobbies = matcher.GetMatchLobbies();

            // We should have exactly 5 lobbies for 10 players.
            Assert.Equal(5, lobbies.Length);
            var matchIds = lobbies.Select(l => l.MatchId).Distinct().ToList();
            Assert.Equal(5, matchIds.Count);
        }

        [Fact]
        public async Task MatchedPlayersAreNotDuplicated()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new NormalMatchmakingService(playerConnRepo);
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame, BotLevel.Hard);

            // we enqueue 4 players
            for (int i = 0; i < 4; i++)
            {
                matcher.Enqueue(CreateQueueEntry(queueKey, 1500));
            }

            await matcher.MatchQueuedPlayersAsync();

            var lobbies = matcher.GetMatchLobbies();
            var allPlayerIds = new List<Guid>();

            foreach (var lobby in lobbies)
            {
                allPlayerIds.Add(lobby.Player1.Id);
                if (lobby.Player2 != null)
                    allPlayerIds.Add(lobby.Player2.Id);
            }

            Assert.Equal(4, allPlayerIds.Count);
            Assert.Equal(4, allPlayerIds.Distinct().Count());
        }

        [Theory]
        [InlineData(1500, 1510, 5)]   // Within 50 rating diff, < 5 seconds
        [InlineData(1500, 1510, 15)]  // Within 100 rating diff, < 15 seconds
        [InlineData(1500, 1510, 35)]  // Within 200 rating diff, < 30 seconds
        [InlineData(1500, 1510, 65)]  // Within 400 rating diff, > 30 seconds
        [InlineData(1500, 1600, 5)]   // 100 rating diff, shouldn't match at 5 seconds
        [InlineData(1200, 1800, 65)]  // 600 rating diff, should not match after 30 seconds
        public async Task RankedMatchmakingRatingDifference(double rating1, double rating2, int secondsWaiting)
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new RankedMatchmakingService(playerConnRepo, new SimpleRepositoryClient());
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame, BotLevel.Hard);

            var entry1 = CreateQueueEntry(queueKey, rating1);
            entry1.EnqueuedAtUtc = DateTime.UtcNow.AddSeconds(-secondsWaiting);

            var entry2 = CreateQueueEntry(queueKey, rating2);
            entry2.EnqueuedAtUtc = DateTime.UtcNow.AddSeconds(-secondsWaiting);

            matcher.Enqueue(entry1);
            matcher.Enqueue(entry2);

            await matcher.MatchQueuedPlayersAsync();

            var lobbies = matcher.GetMatchLobbies();
            var ratingDiff = Math.Abs(rating1 - rating2);

            var allowed = secondsWaiting switch { < 5 => 50, < 15 => 100, < 30 => 200, _ => 400 };
            if (ratingDiff <= allowed)
            {
                Assert.Single(lobbies);
                Assert.All(lobbies, lobby => Assert.False(lobby.Player2!.IsBot));
            }
            else
            {
                Assert.Equal(2, lobbies.Length);
                Assert.Equal(2, lobbies.Select(lobby => lobby.MatchId).Distinct().Count());
                Assert.All(lobbies, lobby => Assert.True(lobby.Player2!.IsBot));
            }
        }

        [Fact]
        public async Task RankedOldPlayerExpandsSearchRadius()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new RankedMatchmakingService(playerConnRepo, new SimpleRepositoryClient());
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame, BotLevel.Hard);

            // we enqueue old player with rating 1500
            var oldPlayer = CreateQueueEntry(queueKey, 1500);
            oldPlayer.EnqueuedAtUtc = DateTime.UtcNow.AddSeconds(-40);

            // we enqueue new player with very different rating 2000
            var newPlayer = CreateQueueEntry(queueKey, 2000);
            newPlayer.EnqueuedAtUtc = DateTime.UtcNow;

            matcher.Enqueue(oldPlayer);
            matcher.Enqueue(newPlayer);

            await matcher.MatchQueuedPlayersAsync();

            var lobbies = matcher.GetMatchLobbies();
            // Both humans must meet the radius requirement; otherwise each receives a bot.
            Assert.Equal(2, lobbies.Select(lobby => lobby.MatchId).Distinct().Count());
            Assert.All(lobbies, lobby => Assert.True(lobby.Player2!.IsBot));
        }

        [Fact]
        public async Task RankedBothOldPlayersMatch()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new RankedMatchmakingService(playerConnRepo, new SimpleRepositoryClient());
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame, BotLevel.Hard);

            // we enqueue two old players with different ratings
            var oldPlayer1 = CreateQueueEntry(queueKey, 1500);
            oldPlayer1.EnqueuedAtUtc = DateTime.UtcNow.AddSeconds(-40);

            var oldPlayer2 = CreateQueueEntry(queueKey, 1800);
            oldPlayer2.EnqueuedAtUtc = DateTime.UtcNow.AddSeconds(-40);

            matcher.Enqueue(oldPlayer1);
            matcher.Enqueue(oldPlayer2);

            await matcher.MatchQueuedPlayersAsync();

            var lobbies = matcher.GetMatchLobbies();
            // both have 400 rating diff allowed, 300 rating diff between them
            // both meet criteria, so should match
            Assert.Single(lobbies);
            var matchIds = lobbies.Select(l => l.MatchId).Distinct().ToList();
            Assert.Single(matchIds);
        }

        [Fact]
        public async Task FailedClaimRollsBackBothPlayers()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var normalMatcher = new NormalMatchmakingService(playerConnRepo);
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame, BotLevel.Hard);

            //we enqueue 4 players
            var players = Enumerable.Range(0, 4)
                .Select(_ => CreateQueueEntry(queueKey, 1500))
                .ToList();

            foreach (var player in players)
            {
                normalMatcher.Enqueue(player);
            }

            // we run matcher multiple times - simulating concurrent access
            for (int i = 0; i < 3; i++)
            {
                await normalMatcher.MatchQueuedPlayersAsync();
            }

            var entries = normalMatcher.GetQueueEntries();
            var lobbies = normalMatcher.GetMatchLobbies();

            // We should have 2 lobbies and no unmatched players.
            Assert.Equal(2, lobbies.Length);
            var matchIds = lobbies.Select(l => l.MatchId).Distinct().ToList();
            Assert.Equal(2, matchIds.Count);
            Assert.Empty(entries);
        }

        [Fact]
        public async Task OddNumberOfPlayersLeavesOneUnmatched()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new NormalMatchmakingService(playerConnRepo);
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame, BotLevel.Hard);

            // we enqueue 7 players (odd number)
            for (int i = 0; i < 7; i++)
            {
                matcher.Enqueue(CreateQueueEntry(queueKey, 1500));
            }

            await matcher.MatchQueuedPlayersAsync();

            var entries = matcher.GetQueueEntries();
            var lobbies = matcher.GetMatchLobbies();

            // We should have 3 lobbies and 1 unmatched player.
            Assert.Equal(3, lobbies.Length);
            var matchIds = lobbies.Select(l => l.MatchId).Distinct().ToList();
            Assert.Equal(3, matchIds.Count);
            Assert.Single(entries);
        }

        [Fact]
        public async Task DifferentModusQueuesAreSeparate()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var normalMatcher = new NormalMatchmakingService(playerConnRepo);
            var botMatcher = new BotMatchmakingService(playerConnRepo);
            var composite = new CompositeMatchmakingService(playerConnRepo);
            composite.AddService(MatchModus.Normal, normalMatcher);
            composite.AddService(MatchModus.Bot, botMatcher);

            var queueKeyNormal = new QueueKey(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame, BotLevel.Hard);
            var queueKeyBot = new QueueKey(MatchVariant.Backgammon, MatchModus.Bot, MatchType.CashGame, BotLevel.Hard);

            // we enqueue 2 normal and 2 bot players
            composite.Enqueue(CreateQueueEntry(queueKeyNormal, 1500));
            composite.Enqueue(CreateQueueEntry(queueKeyNormal, 1500));
            composite.Enqueue(CreateQueueEntry(queueKeyBot, 1500));
            composite.Enqueue(CreateQueueEntry(queueKeyBot, 1500));

            await composite.MatchQueuedPlayersAsync();

            var lobbies = composite.GetMatchLobbies();
            // Normal players match; directly enqueued bot entries remain queued.
            Assert.Equal(MatchModus.Normal, Assert.Single(lobbies).QueueKey.MatchModus);
            Assert.Equal(2, composite.GetQueueEntries().Length);
        }

        [Fact]
        public async Task DifferentVariantQueuesAreNotMatched()
        {
            var playerConnRepo = new PlayerConnectionRepository();
            var matcher = new NormalMatchmakingService(playerConnRepo);
            var queueKeyBackgammon = new QueueKey(MatchVariant.Backgammon, MatchModus.Normal, MatchType.CashGame, BotLevel.Hard);
            var queueKeyTavla = new QueueKey(MatchVariant.Tavla, MatchModus.Normal, MatchType.CashGame, BotLevel.Hard);

            // we enqueue 2 backgammon and 2 tavla players
            matcher.Enqueue(CreateQueueEntry(queueKeyBackgammon, 1500));
            matcher.Enqueue(CreateQueueEntry(queueKeyBackgammon, 1500));
            matcher.Enqueue(CreateQueueEntry(queueKeyTavla, 1500));
            matcher.Enqueue(CreateQueueEntry(queueKeyTavla, 1500));

            await matcher.MatchQueuedPlayersAsync();

            var lobbies = matcher.GetMatchLobbies();
            var entries = matcher.GetQueueEntries();

            // We should have 2 lobbies and no unmatched players.
            Assert.Equal(2, lobbies.Length);
            var matchIds = lobbies.Select(l => l.MatchId).Distinct().ToList();
            Assert.Equal(2, matchIds.Count);
            Assert.Empty(entries);

            // we verify variants are correct
            var backgammonLobbies = lobbies.Count(l => l.QueueKey.MatchVariant == MatchVariant.Backgammon);
            var tavlaLobbies = lobbies.Count(l => l.QueueKey.MatchVariant == MatchVariant.Tavla);
            Assert.Equal(1, backgammonLobbies);
            Assert.Equal(1, tavlaLobbies);
        }

        private sealed class TestMatchmakingService : NormalMatchmakingService
        {
            public TestMatchmakingService(PlayerConnectionRepository connections) : base(connections)
            {
            }

            public void Register(MatchLobby lobby, params QueueEntry[] entries)
            {
                AddMatchLobby(lobby, entries);
            }
        }

        private static QueueEntry CreateQueueEntry(QueueKey key, double rating)
        {
            return new QueueEntry(Guid.NewGuid(), Guid.NewGuid(), key, DateTime.UtcNow, rating);
        }
    }
}
