using GammonX.DynamoDb.Items;

using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Helper
{
    internal static class ItemFactory
    {
        public static PlayerItem CreatePlayer()
        {
            var id = Guid.NewGuid();
            var playerItem = new PlayerItem()
            {
                Id = id,
                CreatedAt = DateTime.Now,
                UserName = $"babahaft-{id}"
            };
            return playerItem;
        }

        public static PlayerStatsItem CreatePlayerStats(PlayerItem playerItem, MatchVariant variant, MatchModus modus, MatchType type)
        {
            var playerStatsItem = new PlayerStatsItem()
            {
                PlayerId = playerItem.Id,
                Variant = variant,
                Modus = modus,
                Type = type,
                MatchesPlayed = 100,
                MatchesWon = 60,
                MatchesLost = 40,
                WinRate = 60.0,
                WinStreak = 5,
                LongestWinStreak = 10,
                TotalPlayTime = TimeSpan.FromHours(50),
                LastMatch = DateTime.UtcNow.AddDays(-1),
                MatchesLast7 = 20,
                MatchesLast30 = 80,
                AvgBackgammons = 1.5,
                AvgGammons = 2.5,
                AvgDuration = TimeSpan.FromMinutes(25),
                WAvgDoubles = 0.3,
                WAvgDoubleDices = 0.4,
                WAvgDuration = TimeSpan.FromMinutes(27),
                WAvgPipesLeft = 3.2,
                WAvgTurns = 15.0,
            };
            return playerStatsItem;
        }

        public static IEnumerable<PlayerStatsItem> CreateAllPlayerStats(PlayerItem playerItem)
        {
            var list = new List<PlayerStatsItem>();

            var variants = new[]
            {
                MatchVariant.Backgammon,
                MatchVariant.Tavli,
                MatchVariant.Tavla
            };

            var modi = new[]
            {
                MatchModus.Ranked,
                MatchModus.Bot,
                MatchModus.Normal
            };

            var types = new[]
            {
                MatchType.CashGame,
                MatchType.FivePointGame,
                MatchType.SevenPointGame
            };

            foreach (var variant in variants)
            {
                foreach (var modus in modi)
                {
                    foreach (var type in types)
                    {
                        var item = ItemFactory.CreatePlayerStats(playerItem, variant, modus, type);
                        list.Add(item);
                    }
                }
            }

            return list;
        }
    }
}
