using Amazon.DynamoDBv2.Model;

using System.Globalization;

using GammonX.Models.Enums;
using GammonX.Models.Helpers;

namespace GammonX.DynamoDb.Items
{
    // <inheritdoc />
    internal class RatingPeriodItemFactory : IItemFactory<RatingPeriodItem>
    {
        // <inheritdoc />
        public string PKFormat => "PLAYER#{0:D}";

        /// <summary>
        /// Gets the sk format for a rating period. E.g. MATCH#{variant}#{type}#{modus}#{matchId}.
        /// </summary>
        public string SKFormat => "MATCH#{0}#{1}#{2}#{3:D}";

        // <inheritdoc />
        public string SKPrefix => "MATCH#";

        // <inheritdoc />
        public string GSI1PKFormat => throw new InvalidOperationException("Global search index not applicable for this item type");

        // <inheritdoc />
        public string GSI1SKFormat => throw new InvalidOperationException("Global search index not applicable for this item type");

        // <inheritdoc />
        public string GSI1SKPrefix => throw new InvalidOperationException("Global search index not applicable for this item type");

        // <inheritdoc />
        public RatingPeriodItem CreateItem(Dictionary<string, AttributeValue> item)
        {
            var ratingPeriodItem = new RatingPeriodItem
            {
                PlayerId = Guid.Parse(item["PlayerId"].S),
                OpponentId = Guid.Parse(item["OpponentId"].S),
                MatchId = Guid.Parse(item["MatchId"].S),
                MatchScore = double.Parse(item["MatchScore"].N, CultureInfo.InvariantCulture),
                Variant = Enum.Parse<MatchVariant>(item["Variant"].S, true),
                Type = Enum.Parse<Models.Enums.MatchType>(item["Type"].S, true),
                Modus = Enum.Parse<MatchModus>(item["Modus"].S, true),
                PlayerRating = double.Parse(item["PlayerRating"].N, CultureInfo.InvariantCulture),
                PlayerRatingDeviation = double.Parse(item["PlayerRatingDeviation"].N, CultureInfo.InvariantCulture),
                PlayerSigma = double.Parse(item["PlayerSigma"].N, CultureInfo.InvariantCulture),
                OpponentRating = double.Parse(item["OpponentRating"].N, CultureInfo.InvariantCulture),
                OpponentRatingDeviation = double.Parse(item["OpponentRatingDeviation"].N, CultureInfo.InvariantCulture),
                OpponentSigma = double.Parse(item["OpponentSigma"].N, CultureInfo.InvariantCulture),
                CreatedAt = DateTimeHelper.ParseUtc(item["CreatedAt"].S)
            };
            return ratingPeriodItem;
        }

        // <inheritdoc />
        public Dictionary<string, AttributeValue> CreateItem(RatingPeriodItem item)
        {
            var variantStr = item.Variant.ToString();
            var modusStr = item.Modus.ToString();
            var typeStr = item.Type.ToString();
            var itemDict = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue(item.PK) },
                { "SK", new AttributeValue(item.SK) },
                { "PlayerId", new AttributeValue(item.PlayerId.ToString("D")) },
                { "OpponentId", new AttributeValue(item.OpponentId.ToString("D")) },
                { "MatchId", new AttributeValue(item.MatchId.ToString("D")) },
                { "MatchScore", new AttributeValue() { N = item.MatchScore.ToString(CultureInfo.InvariantCulture) } },
                { "ItemType", new AttributeValue(item.ItemType) },
                { "Variant", new AttributeValue(variantStr) },
                { "Modus", new AttributeValue(modusStr) },
                { "Type", new AttributeValue(typeStr) },
                { "PlayerRating", new AttributeValue() { N = item.PlayerRating.ToString(CultureInfo.InvariantCulture) } },
                { "PlayerRatingDeviation", new AttributeValue() { N = item.PlayerRatingDeviation.ToString(CultureInfo.InvariantCulture) } },
                { "PlayerSigma", new AttributeValue() { N = item.PlayerSigma.ToString(CultureInfo.InvariantCulture) } },
                { "OpponentRating", new AttributeValue() { N = item.OpponentRating.ToString(CultureInfo.InvariantCulture) } },
                { "OpponentRatingDeviation", new AttributeValue() { N = item.OpponentRatingDeviation.ToString(CultureInfo.InvariantCulture) } },
                { "OpponentSigma", new AttributeValue() { N = item.OpponentSigma.ToString(CultureInfo.InvariantCulture) } },
                { "CreatedAt", new AttributeValue { S = DateTimeHelper.FormatUtc(item.CreatedAt) } },
            };
            return itemDict;
        }
    }
}
