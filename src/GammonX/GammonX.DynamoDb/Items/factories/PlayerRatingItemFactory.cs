using Amazon.DynamoDBv2.Model;

using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Items 
{
    // <inheritdoc />
    public class PlayerRatingItemFactory : IItemFactory<PlayerRatingItem>
    {
        // <inheritdoc />
        public string PKFormat => "PLAYER#{0}";

        // <inheritdoc />
        public string SKFormat => "RATING#{0}";

        // <inheritdoc />
        public string SKPrefix => "RATING#";

        // <inheritdoc />
        public string GSI1PKFormat => throw new InvalidOperationException("Global search index not applicable for this item type");

        // <inheritdoc />
        public string GSI1SKFormat => throw new InvalidOperationException("Global search index not applicable for this item type");

        // <inheritdoc />
        public string GSI1SKPrefix => throw new InvalidOperationException("Global search index not applicable for this item type");

        // <inheritdoc />
        public PlayerRatingItem CreateItem(Dictionary<string, AttributeValue> item)
        {
            var playerRatingItem = new PlayerRatingItem
            {
                PlayerId = Guid.Parse(item["Id"].S),
                Variant = Enum.Parse<MatchVariant>(item["Variant"].S, true),
                Type = Enum.Parse<MatchType>(item["Type"].S, true),
                Modus = Enum.Parse<MatchModus>(item["Modus"].S, true),
                Rating = double.Parse(item["Rating"].N),
                RatingDeviation = double.Parse(item["RatingDeviation"].N),
                Sigma = double.Parse(item["Sigma"].N),
                HighestRating = double.Parse(item["HighestRating"].N),
                LowestRating = double.Parse(item["LowestRating"].N),
                MatchesPlayed = int.Parse(item["MatchesPlayed"].N)
            };
            return playerRatingItem;
        }

        // <inheritdoc />
        public Dictionary<string, AttributeValue> CreateItem(PlayerRatingItem item)
        {
            var variantStr = item.Variant.ToString();
            var modusStr = item.Modus.ToString();
            var typeStr = item.Type.ToString();
            var itemDict = new Dictionary<string, AttributeValue>
            {
                { "PK", new AttributeValue(item.PK) },
                { "SK", new AttributeValue(item.SK) },
                { "Id", new AttributeValue(item.PlayerId.ToString()) },
                { "ItemType", new AttributeValue(item.ItemType) },
                { "Variant", new AttributeValue(variantStr) },
                { "Modus", new AttributeValue(modusStr) },
                { "Type", new AttributeValue(typeStr) },
                { "Rating", new AttributeValue() { N = item.Rating.ToString() } },
                { "RatingDeviation", new AttributeValue() { N = item.RatingDeviation.ToString() } },
                { "Sigma", new AttributeValue() { N = item.Sigma.ToString() } },
                { "HighestRating", new AttributeValue() { N = item.HighestRating.ToString() } },
                { "LowestRating", new AttributeValue() { N = item.LowestRating.ToString() } },
                { "MatchesPlayed", new AttributeValue() { N = item.MatchesPlayed.ToString() } }
            };
            return itemDict;
        }

        public static PlayerRatingItem CreateInitial(Guid playerId, MatchVariant variant, MatchType type)
        {
            return new PlayerRatingItem()
            {
                PlayerId = playerId,
                MatchesPlayed = 0,
                Variant = variant,
                Type = type,
            };
        }
    }
}
