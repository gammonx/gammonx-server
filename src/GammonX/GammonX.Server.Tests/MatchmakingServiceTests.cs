using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Repository;
using GammonX.Server.Services;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests
{
    public class MatchmakingServiceTests
    {
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

            // we only want a raating increase up to 50 (first search range)
            var entries = Enumerable.Range(0, playerCount).Select(i => CreateQueueEntry(queueKey, 1500 + i / 2));

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
                .SelectMany(l => new[] {l.Player1.Id, l.Player2?.Id ?? Guid.Empty})
                .Where(id => id != Guid.Empty)
                .ToList();

            // no duplicates
            // we have each match lobby twice (once per player)
            Assert.True(matchedPlayers.Count / 2 <= playerCount);
            Assert.True(matchLobbies.Length / 2 <= playerCount / 2);
            if (modus == MatchModus.Bot)
            {
                // except for bot matches
                Assert.Equal(matchedPlayers.Count, matchedPlayers.Distinct().Count());
            }
            else
            {
                Assert.Equal(matchedPlayers.Count / 2, matchedPlayers.Distinct().Count());
            }

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

            var matchTask = Task.Run(() => matcher.MatchQueuedPlayersAsync(), TestContext.Current.CancellationToken);

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

            if (modus == MatchModus.Bot)
            {
                var totalKnown = entries.Length + (lobbies.Length / 2);
                Assert.Equal(40, totalKnown);
            }
            else
            {
                var totalKnown = entries.Length + lobbies.Length;
                Assert.Equal(40, totalKnown);
            }
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
            // we expect a single match lobby per player
            Assert.Equal(2, lobbies.Length);
        }

        [Fact]
        public async Task LastSeenTimeStampIsUpdated()
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
        public async Task QueueCleansUpExpiredEntries()
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
            var entry2 = new QueueEntry(Guid.NewGuid(), playerId, queueKey, DateTime.UtcNow, 1500);
            
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

            // we should have exactly 5 unique lobbies but 10 entries for 10 players
            Assert.Equal(10, lobbies.Length);
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

            Assert.Equal(8, allPlayerIds.Count);
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

            // we check if match was made based on rating difference and wait time
            if (secondsWaiting < 5 && ratingDiff > 50)
                Assert.Empty(lobbies); // should not match
            else if (secondsWaiting < 15 && ratingDiff > 100)
                Assert.Empty(lobbies); // should not match
            else if (secondsWaiting < 30 && ratingDiff > 200)
                Assert.Empty(lobbies); // should not match
            else
            {
                if (ratingDiff <= 400)
                {
                    Assert.Equal(2, lobbies.Length); // should match
                    var matchIds = lobbies.Select(l => l.MatchId).Distinct().ToList();
                    Assert.Single(matchIds);
                }
                else
                {
                    Assert.Empty(lobbies);
                }
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
            // old player's search radius is 400, so should match despite 500 rating diff
            // new player's search radius is 50, so should NOT match
            // the condition requires both to be within allowed diff, so this should not match
            Assert.Empty(lobbies);
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
            Assert.Equal(2, lobbies.Length);
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

            // we should have matched 2 unqiue lobbies but 4 entries and have 0 unmatched players
            Assert.Equal(4, lobbies.Length);
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

            // we should have 3 unique lobbies and 6 entries + 1 unmatched player
            Assert.Equal(6, lobbies.Length);
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
            // we should have 2 lobbies total (1 normal, 1 bot)
            Assert.Equal(2, lobbies.Length);
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

            // we should have 2 unique lobbies but 4 entries and 0 unmatched players
            Assert.Equal(4, lobbies.Length);
            var matchIds = lobbies.Select(l => l.MatchId).Distinct().ToList();
            Assert.Equal(2, matchIds.Count);
            Assert.Empty(entries);

            // we verify variants are correct
            var backgammonLobbies = lobbies.Where(l => l.QueueKey.MatchVariant == MatchVariant.Backgammon).Count();
            var tavlaLobbies = lobbies.Where(l => l.QueueKey.MatchVariant == MatchVariant.Tavla).Count();
            Assert.Equal(2, backgammonLobbies);
            Assert.Equal(2, tavlaLobbies);
        }

        private static QueueEntry CreateQueueEntry(QueueKey key, double rating)
        {
            return new QueueEntry(Guid.NewGuid(), Guid.NewGuid(), key, DateTime.UtcNow, rating);
        }
    }
}
