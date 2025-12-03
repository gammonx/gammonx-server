using Amazon.DynamoDBv2.Model;

using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Items
{
    // <inheritdoc />
    internal class RatingPeriodItemFactory : IItemFactory<RatingPeriodItem>
    {
        // <inheritdoc />
        public string PKFormat => "PLAYER#{0}";

        /// <summary>
        /// Gets the sk format for a rating period. E.g. MATCH#{variant}#{type}#{modus}#{matchId}.
        /// </summary>
        public string SKFormat => "MATCH#{0}#{1}#{2}#{3}";

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
                MatchScore = int.Parse(item["MatchScore"].N),
                Variant = Enum.Parse<MatchVariant>(item["Variant"].S, true),
                Type = Enum.Parse<Models.Enums.MatchType>(item["Type"].S, true),
                Modus = Enum.Parse<MatchModus>(item["Modus"].S, true),
                PlayerRating = double.Parse(item["PlayerRating"].N),
                PlayerRatingDeviation = double.Parse(item["PlayerRatingDeviation"].N),
                PlayerSigma = double.Parse(item["PlayerSigma"].N),
                OpponentRating = double.Parse(item["OpponentRating"].N),
                OpponentRatingDeviation = double.Parse(item["OpponentRatingDeviation"].N),
                OpponentSigma = double.Parse(item["OpponentSigma"].N),
                CreatedAt = DateTime.Parse(item["CreatedAt"].S)
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
                { "PlayerId", new AttributeValue(item.PlayerId.ToString()) },
                { "OpponentId", new AttributeValue(item.OpponentId.ToString()) },
                { "MatchId", new AttributeValue(item.MatchId.ToString()) },
                { "MatchScore", new AttributeValue() { N = item.MatchScore.ToString() } },
                { "ItemType", new AttributeValue(item.ItemType) },
                { "Variant", new AttributeValue(variantStr) },
                { "Modus", new AttributeValue(modusStr) },
                { "Type", new AttributeValue(typeStr) },
                { "PlayerRating", new AttributeValue() { N = item.PlayerRating.ToString() } },
                { "PlayerRatingDeviation", new AttributeValue() { N = item.PlayerRatingDeviation.ToString() } },
                { "PlayerSigma", new AttributeValue() { N = item.PlayerSigma.ToString() } },
                { "OpponentRating", new AttributeValue() { N = item.OpponentRating.ToString() } },
                { "OpponentRatingDeviation", new AttributeValue() { N = item.OpponentRatingDeviation.ToString() } },
                { "OpponentSigma", new AttributeValue() { N = item.OpponentSigma.ToString() } },
                { "CreatedAt", new AttributeValue { S = item.CreatedAt.ToString("o") } },
            };
            return itemDict;
        }
    }
}
