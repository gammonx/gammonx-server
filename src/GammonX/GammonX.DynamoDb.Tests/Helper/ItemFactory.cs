using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Stats;

using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Helper
{
    internal static class ItemFactory
    {
        private readonly static MatchVariant[] _variants = new[]
        {
            MatchVariant.Backgammon,
            MatchVariant.Tavli,
            MatchVariant.Tavla
        };

        private readonly static MatchModus[] _modi = new[]
        {
            MatchModus.Ranked,
            MatchModus.Bot,
            MatchModus.Normal
        };

        private readonly static MatchType[] _types = new[]
        {
            MatchType.CashGame,
            MatchType.FivePointGame,
            MatchType.SevenPointGame
        };


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

        public static MatchItem CreateMatch(Guid matchId, PlayerItem playerItem, MatchResult result, MatchVariant variant, MatchModus modus, MatchType type)
        {
            var matchItem = new MatchItem()
            {
                PlayerId = playerItem.Id,
                Id = matchId,
                Variant = variant,
                Modus = modus,
                Type = type,
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                EndedAt = DateTime.UtcNow,
                Result = result,
                Points = 7,
                Length = 4,
                Gammons = 3,
                Backgammons = 0,
                AvgDoubleDices = 0.4,
                AvgDoubles = 0.3,
                AvgPipesLeft = 2.5,
                AvgTurns = 15,
                AvgDuration = TimeSpan.FromMinutes(10),
                Duration = TimeSpan.FromMinutes(40),
            };
            return matchItem;
        }

        public static PlayerRatingItem CreatePlayerRating(PlayerItem playerItem, MatchVariant variant, MatchModus modus, MatchType type)
        {
            var playerRatingItem = new PlayerRatingItem()
            {
                PlayerId = playerItem.Id,
                Variant = variant,
                Modus = modus,
                Type = type,
                Rating = Glicko2Constants.DefaultRating,
                RatingDeviation = Glicko2Constants.DefaultRD,
                Sigma = Glicko2Constants.DefaultSigma,
                HighestRating = 1800,
                LowestRating = 1000,
                MatchesPlayed = 30
            };
            return playerRatingItem;
        }

        public static RatingPeriodItem CrateRatingPeriod(PlayerItem playerItem, PlayerItem opponentItem, MatchItem matchItem)
        {
            var ratingPeriodItem = new RatingPeriodItem()
            {
                PlayerId = playerItem.Id,
                OpponentId = opponentItem.Id,
                Variant = matchItem.Variant,
                Modus = matchItem.Modus,
                Type = matchItem.Type,
                PlayerRating = Glicko2Constants.DefaultRating,
                PlayerRatingDeviation = Glicko2Constants.DefaultRD,
                PlayerSigma = Glicko2Constants.DefaultSigma,
                OpponentRating = Glicko2Constants.DefaultRating,
                OpponentRatingDeviation = Glicko2Constants.DefaultRD,
                OpponentSigma = Glicko2Constants.DefaultSigma,
                CreatedAt = DateTime.UtcNow,
                MatchId = matchItem.Id,
                MatchScore = 1
                
            };
            return ratingPeriodItem;
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

            foreach (var variant in _variants)
            {
                foreach (var modus in _modi)
                {
                    foreach (var type in _types)
                    {
                        var item = CreatePlayerStats(playerItem, variant, modus, type);
                        list.Add(item);
                    }
                }
            }

            return list;
        }

        public static IEnumerable<PlayerRatingItem> CreateAllPlayerRatings(PlayerItem playerItem)
        {
            var list = new List<PlayerRatingItem>();

            foreach (var variant in _variants)
            {
                var item = CreatePlayerRating(playerItem, variant, MatchModus.Ranked, MatchType.SevenPointGame);
                list.Add(item);
            }

            return list;
        }
    }
}
