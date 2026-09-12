using GammonX.DynamoDb.Items;

using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Items
{
    public class PlayerStatsItemFactoryTests
    {
        [Fact]
        public void CreateItemWithConsecutiveMatchesCalculatesCompletedMatchStatistics()
        {
            var playerId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var matches = new[]
            {
                CreateMatch(playerId, MatchResult.Won, now.AddDays(-1), 5, 50, 50, 1, 0),
                CreateMatch(playerId, MatchResult.Won, now.AddDays(-20), 1, 10, 10, 1, 0),
                CreateMatch(playerId, MatchResult.Won, now.AddDays(-2), 4, 40, 40, 0, 0),
                CreateMatch(playerId, MatchResult.Unknown, now.AddDays(-5), 100, 1000, 1000, 100, 100),
                CreateMatch(playerId, MatchResult.Lost, now.AddDays(-8), 3, 30, 30, 1, 0),
                CreateMatch(playerId, MatchResult.Won, now.AddDays(-10), 2, 20, 20, 0, 1)
            };

            var stats = PlayerStatsItemFactory.CreateItem(
                playerId,
                MatchVariant.Backgammon,
                MatchType.CashGame,
                MatchModus.Ranked,
                matches);

            Assert.Equal(5, stats.MatchesPlayed);
            Assert.Equal(4, stats.MatchesWon);
            Assert.Equal(1, stats.MatchesLost);
            Assert.Equal(0.8, stats.WinRate);
            Assert.Equal(2, stats.WinStreak);
            Assert.Equal(2, stats.LongestWinStreak);
            Assert.Equal(TimeSpan.FromMinutes(150), stats.TotalPlayTime);
            Assert.Equal(now.AddDays(-1), stats.LastMatch);
            Assert.Equal(2, stats.MatchesLast7);
            Assert.Equal(5, stats.MatchesLast30);
            Assert.Equal(0.6, stats.AvgGammons);
            Assert.Equal(0.2, stats.AvgBackgammons);
            Assert.Equal(TimeSpan.FromMinutes(30), stats.AvgDuration);

            var expectedWeightedAverage = 550.0 / 15.0;
            Assert.Equal(expectedWeightedAverage, stats.WAvgPipesLeft, precision: 10);
            Assert.Equal(expectedWeightedAverage, stats.WAvgDoubleDices, precision: 10);
            Assert.Equal(expectedWeightedAverage, stats.WAvgTurns, precision: 10);
            Assert.Equal(expectedWeightedAverage, stats.WAvgDoubles, precision: 10);
            Assert.Equal(TimeSpan.FromTicks(TimeSpan.FromMinutes(550).Ticks / 15), stats.WAvgDuration);
        }

        [Fact]
        public void CreateItemWithoutCompletedMatchesThrows()
        {
            var unfinishedMatch = CreateMatch(
                Guid.NewGuid(),
                MatchResult.Unknown,
                DateTime.UtcNow,
                1,
                10,
                10,
                0,
                0);

            Assert.Throws<ArgumentException>(() => PlayerStatsItemFactory.CreateItem(
                unfinishedMatch.PlayerId,
                MatchVariant.Backgammon,
                MatchType.CashGame,
                MatchModus.Ranked,
                new[] { unfinishedMatch }));
        }

        [Fact]
        public void CreateItemIgnoresInvalidDurationsAndCounters()
        {
            var playerId = Guid.NewGuid();
            var matches = new[]
            {
                CreateMatch(playerId, MatchResult.Won, DateTime.UtcNow.AddDays(-2), 0, -10, 10, -1, -1),
                CreateMatch(playerId, MatchResult.Lost, DateTime.UtcNow.AddDays(-1), 2, 10, 20, 0, 0)
            };

            var stats = PlayerStatsItemFactory.CreateItem(
                playerId,
                MatchVariant.Backgammon,
                MatchType.CashGame,
                MatchModus.Ranked,
                matches);

            Assert.Equal(TimeSpan.FromMinutes(10), stats.TotalPlayTime);
            Assert.Equal(TimeSpan.FromMinutes(10), stats.AvgDuration);
            Assert.Equal(0, stats.AvgGammons);
            Assert.Equal(0, stats.AvgBackgammons);
            Assert.Equal(20, stats.WAvgPipesLeft);
        }

        private static MatchItem CreateMatch(
            Guid playerId,
            MatchResult result,
            DateTime endedAt,
            int length,
            int durationMinutes,
            int metricValue,
            int gammons,
            int backgammons)
        {
            return new MatchItem
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                Result = result,
                Length = length,
                StartedAt = endedAt.AddMinutes(-durationMinutes),
                EndedAt = endedAt,
                Duration = TimeSpan.FromMinutes(durationMinutes),
                AvgDuration = TimeSpan.FromMinutes(durationMinutes),
                AvgPipesLeft = metricValue,
                AvgDoubleDices = metricValue,
                AvgTurns = metricValue,
                AvgDoubles = metricValue,
                Gammons = gammons,
                Backgammons = backgammons
            };
        }
    }
}