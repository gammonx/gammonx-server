using Amazon.DynamoDBv2.Model;

using GammonX.DynamoDb.Items;

using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Items
{
    public class UuidPersistenceTests
    {
        private static readonly Guid PlayerId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");
        private static readonly Guid OpponentId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
        private static readonly Guid MatchId = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC");
        private static readonly Guid GameId = Guid.Parse("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD");

        [Fact]
        public void ItemFactoriesUseCanonicalUuidStrings()
        {
            var player = new PlayerItem { Id = PlayerId, UserName = "player" };
            var match = new MatchItem
            {
                Id = MatchId,
                PlayerId = PlayerId,
                Variant = MatchVariant.Backgammon,
                Modus = MatchModus.Ranked,
                Type = MatchType.SevenPointGame,
                Result = MatchResult.Unknown
            };
            var game = new GameItem
            {
                Id = GameId,
                PlayerId = PlayerId,
                MatchId = MatchId,
                Modus = GameModus.Portes,
                Result = GameResult.Unknown
            };
            var playerRating = new PlayerRatingItem
            {
                PlayerId = PlayerId,
                Variant = MatchVariant.Backgammon,
                Modus = MatchModus.Ranked,
                Type = MatchType.SevenPointGame
            };
            var playerStats = new PlayerStatsItem
            {
                PlayerId = PlayerId,
                Variant = MatchVariant.Backgammon,
                Modus = MatchModus.Ranked,
                Type = MatchType.SevenPointGame
            };
            var ratingPeriod = new RatingPeriodItem
            {
                PlayerId = PlayerId,
                OpponentId = OpponentId,
                MatchId = MatchId,
                Variant = MatchVariant.Backgammon,
                Modus = MatchModus.Ranked,
                Type = MatchType.SevenPointGame
            };
            var matchHistory = new MatchHistoryItem { MatchId = MatchId, Format = HistoryFormat.MAT };
            var gameHistory = new GameHistoryItem { GameId = GameId, Format = HistoryFormat.MAT };

            var playerAttributes = ItemFactoryCreator.Create<PlayerItem>().CreateItem(player);
            AssertUuidAttribute(playerAttributes, "Id", PlayerId);
            AssertKey(playerAttributes, "PK", $"PLAYER#{PlayerId:D}");

            var matchAttributes = ItemFactoryCreator.Create<MatchItem>().CreateItem(match);
            AssertUuidAttribute(matchAttributes, "Id", MatchId);
            AssertUuidAttribute(matchAttributes, "PlayerId", PlayerId);
            AssertKey(matchAttributes, "PK", $"MATCH#{MatchId:D}");
            AssertKey(matchAttributes, "GSI1PK", $"PLAYER#{PlayerId:D}");
            AssertKey(matchAttributes, "SK", $"DETAILS#NOTFINISHED#{PlayerId:D}");

            var gameAttributes = ItemFactoryCreator.Create<GameItem>().CreateItem(game);
            AssertUuidAttribute(gameAttributes, "Id", GameId);
            AssertUuidAttribute(gameAttributes, "PlayerId", PlayerId);
            AssertUuidAttribute(gameAttributes, "MatchId", MatchId);
            AssertKey(gameAttributes, "PK", $"MATCH#{MatchId:D}");
            AssertKey(gameAttributes, "SK", $"GAME#{GameId:D}#NOTFINISHED#{PlayerId:D}");
            AssertKey(gameAttributes, "GSI1PK", $"PLAYER#{PlayerId:D}");

            var playerRatingAttributes = ItemFactoryCreator.Create<PlayerRatingItem>().CreateItem(playerRating);
            AssertUuidAttribute(playerRatingAttributes, "PlayerId", PlayerId);
            AssertKey(playerRatingAttributes, "PK", $"PLAYER#{PlayerId:D}");

            var playerStatsAttributes = ItemFactoryCreator.Create<PlayerStatsItem>().CreateItem(playerStats);
            AssertUuidAttribute(playerStatsAttributes, "PlayerId", PlayerId);
            AssertKey(playerStatsAttributes, "PK", $"PLAYER#{PlayerId:D}");

            var ratingPeriodAttributes = ItemFactoryCreator.Create<RatingPeriodItem>().CreateItem(ratingPeriod);
            AssertUuidAttribute(ratingPeriodAttributes, "PlayerId", PlayerId);
            AssertUuidAttribute(ratingPeriodAttributes, "OpponentId", OpponentId);
            AssertUuidAttribute(ratingPeriodAttributes, "MatchId", MatchId);
            AssertKey(ratingPeriodAttributes, "PK", $"PLAYER#{PlayerId:D}");
            AssertKey(ratingPeriodAttributes, "SK", $"MATCH#Backgammon#SevenPointGame#Ranked#{MatchId:D}");

            var matchHistoryAttributes = ItemFactoryCreator.Create<MatchHistoryItem>().CreateItem(matchHistory);
            AssertUuidAttribute(matchHistoryAttributes, "MatchId", MatchId);
            AssertKey(matchHistoryAttributes, "PK", $"MATCH#{MatchId:D}");

            var gameHistoryAttributes = ItemFactoryCreator.Create<GameHistoryItem>().CreateItem(gameHistory);
            AssertUuidAttribute(gameHistoryAttributes, "GameId", GameId);
            AssertKey(gameHistoryAttributes, "PK", $"GAME#{GameId:D}");
        }

        private static void AssertUuidAttribute(Dictionary<string, AttributeValue> attributes, string name, Guid expected)
        {
            Assert.Equal(expected.ToString("D"), attributes[name].S);
            Assert.Null(attributes[name].N);
            Assert.NotEqual(true, attributes[name].NULL);
        }

        private static void AssertKey(Dictionary<string, AttributeValue> attributes, string name, string expected)
        {
            Assert.Equal(expected, attributes[name].S);
        }
    }
}