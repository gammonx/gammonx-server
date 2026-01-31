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
            var rankedMatcher = new RankedMatchmakingService(new SimpleRepositoryClient());
            var normalMatcher = new NormalMatchmakingService();
            var botMatcher = new BotMatchmakingService();
            var matcher = new CompositeMatchmakingService();
            matcher.AddService(MatchModus.Ranked, rankedMatcher);
            matcher.AddService(MatchModus.Normal, normalMatcher);
            matcher.AddService(MatchModus.Bot, botMatcher);

            var queueKey = new QueueKey(variant, modus, type);

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
            var rankedMatcher = new RankedMatchmakingService(new SimpleRepositoryClient());
            var normalMatcher = new NormalMatchmakingService();
            var botMatcher = new BotMatchmakingService();
            var matcher = new CompositeMatchmakingService();
            matcher.AddService(MatchModus.Ranked, rankedMatcher);
            matcher.AddService(MatchModus.Normal, normalMatcher);
            matcher.AddService(MatchModus.Bot, botMatcher);

            var queueKey = new QueueKey(variant, modus, type);

            var initialEntries = Enumerable.Range(0, 20)
                .Select(_ => CreateQueueEntry(queueKey, 1500))
                .ToList();

            foreach (var entry in initialEntries)
            {
                matcher.Enqueue(entry);
            }

            var matchTask = Task.Run(() => matcher.MatchQueuedPlayersAsync());

            var enqueueTask = Task.Run(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    matcher.Enqueue(CreateQueueEntry(queueKey, 1500));
                }
            });

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
            var matcher = new RankedMatchmakingService(new SimpleRepositoryClient());
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);

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
            var matcher = new RankedMatchmakingService(new SimpleRepositoryClient());
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);
            var entry = CreateQueueEntry(queueKey, 1200);
            var lastSeenUtc = entry.LastSeenUtc;
            matcher.Enqueue(entry);

            matcher.TouchQueueEntry(entry.Id);

            Assert.NotEqual(lastSeenUtc, entry.LastSeenUtc);
        }

        [Fact]
        public async Task QueueCleansUpExpiredEntries()
        {
            var matcher = new RankedMatchmakingService(new SimpleRepositoryClient());
            var queueKey = new QueueKey(MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);
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


        private static QueueEntry CreateQueueEntry(QueueKey key, double rating)
        {
            return new QueueEntry(Guid.NewGuid(), Guid.NewGuid(), key, DateTime.UtcNow, rating);
        }
    }
}
