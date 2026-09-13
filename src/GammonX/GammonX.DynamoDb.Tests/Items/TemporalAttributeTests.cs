using System.Globalization;

using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Tests.Helper;

using GammonX.Models.Enums;
using GammonX.Models.Helpers;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Items
{
    public class TemporalAttributeTests
    {
        [Fact]
        public void FactoriesUseCanonicalTemporalAttributeTypes()
        {
            var player = ItemFactory.CreatePlayer();
            var match = ItemFactory.CreateMatch(Guid.NewGuid(), player, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);
            var game = ItemFactory.CreateGame(Guid.NewGuid(), match, player, GameResult.Single, GameModus.Backgammon);

            var playerAttributes = ItemFactoryCreator.Create<PlayerItem>().CreateItem(player);
            AssertStringAttribute(playerAttributes, "CreatedAt", DateTimeHelper.FormatUtc(player.CreatedAt));

            var matchAttributes = ItemFactoryCreator.Create<MatchItem>().CreateItem(match);
            AssertStringAttribute(matchAttributes, "StartedAt", DateTimeHelper.FormatUtc(match.StartedAt));
            AssertStringAttribute(matchAttributes, "EndedAt", DateTimeHelper.FormatUtc(match.EndedAt));
            AssertNumberAttribute(matchAttributes, "Duration", DateTimeHelper.FormatDurationTicks(match.Duration));
            AssertNumberAttribute(matchAttributes, "AvgDuration", DateTimeHelper.FormatDurationTicks(match.AvgDuration));

            var gameAttributes = ItemFactoryCreator.Create<GameItem>().CreateItem(game);
            AssertStringAttribute(gameAttributes, "StartedAt", DateTimeHelper.FormatUtc(game.StartedAt));
            AssertStringAttribute(gameAttributes, "EndedAt", DateTimeHelper.FormatUtc(game.EndedAt));
            AssertNumberAttribute(gameAttributes, "Duration", DateTimeHelper.FormatDurationTicks(game.Duration));
            Assert.True(gameAttributes["DoublingCubeValue"].NULL);

            var ratingPeriod = ItemFactory.CrateRatingPeriod(player, ItemFactory.CreatePlayer(), match);
            var ratingPeriodAttributes = ItemFactoryCreator.Create<RatingPeriodItem>().CreateItem(ratingPeriod);
            AssertStringAttribute(ratingPeriodAttributes, "CreatedAt", DateTimeHelper.FormatUtc(ratingPeriod.CreatedAt));

            var playerStats = ItemFactory.CreatePlayerStats(player, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);
            var statsAttributes = ItemFactoryCreator.Create<PlayerStatsItem>().CreateItem(playerStats);
            AssertStringAttribute(statsAttributes, "LastMatch", DateTimeHelper.FormatUtc(playerStats.LastMatch!.Value));
            AssertNumberAttribute(statsAttributes, "TotalPlayTime", DateTimeHelper.FormatDurationTicks(playerStats.TotalPlayTime));
            AssertNumberAttribute(statsAttributes, "AvgDuration", DateTimeHelper.FormatDurationTicks(playerStats.AvgDuration));
            AssertNumberAttribute(statsAttributes, "WAvgDuration", DateTimeHelper.FormatDurationTicks(playerStats.WAvgDuration));

            playerStats.LastMatch = null;
            statsAttributes = ItemFactoryCreator.Create<PlayerStatsItem>().CreateItem(playerStats);
            Assert.True(statsAttributes["LastMatch"].NULL);
            Assert.Null(statsAttributes["LastMatch"].S);
        }

        [Fact]
        public void NumericAttributesRemainInvariantUnderGermanCulture()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

                var player = ItemFactory.CreatePlayer();
                var match = ItemFactory.CreateMatch(Guid.NewGuid(), player, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);
                var attributes = ItemFactoryCreator.Create<MatchItem>().CreateItem(match);

                Assert.Equal("0.4", attributes["AvgDoubleDices"].N);
                Assert.Equal("0.3", attributes["AvgDoubles"].N);
                Assert.Equal(DateTimeHelper.FormatDurationTicks(match.Duration), attributes["Duration"].N);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        private static void AssertStringAttribute(Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> attributes, string name, string expected)
        {
            Assert.Equal(expected, attributes[name].S);
            Assert.Null(attributes[name].N);
            Assert.NotEqual(true, attributes[name].NULL);
        }

        private static void AssertNumberAttribute(Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> attributes, string name, string expected)
        {
            Assert.Equal(expected, attributes[name].N);
            Assert.Null(attributes[name].S);
            Assert.NotEqual(true, attributes[name].NULL);
        }
    }
}