using Amazon.DynamoDBv2.Model;

using System.Globalization;

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
        public string SKFormat => "RATING#{0}#{1}";

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
                PlayerId = Guid.Parse(item["PlayerId"].S),
                Variant = Enum.Parse<MatchVariant>(item["Variant"].S, true),
                Type = Enum.Parse<MatchType>(item["Type"].S, true),
                Modus = Enum.Parse<MatchModus>(item["Modus"].S, true),
                Rating = double.Parse(item["Rating"].N, CultureInfo.InvariantCulture),
                RatingDeviation = double.Parse(item["RatingDeviation"].N, CultureInfo.InvariantCulture),
                Sigma = double.Parse(item["Sigma"].N, CultureInfo.InvariantCulture),
                HighestRating = double.Parse(item["HighestRating"].N, CultureInfo.InvariantCulture),
                LowestRating = double.Parse(item["LowestRating"].N, CultureInfo.InvariantCulture),
                MatchesPlayed = int.Parse(item["MatchesPlayed"].N, CultureInfo.InvariantCulture)
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
                { "PlayerId", new AttributeValue(item.PlayerId.ToString()) },
                { "ItemType", new AttributeValue(item.ItemType) },
                { "Variant", new AttributeValue(variantStr) },
                { "Modus", new AttributeValue(modusStr) },
                { "Type", new AttributeValue(typeStr) },
                { "Rating", new AttributeValue() { N = item.Rating.ToString(CultureInfo.InvariantCulture) } },
                { "RatingDeviation", new AttributeValue() { N = item.RatingDeviation.ToString(CultureInfo.InvariantCulture) } },
                { "Sigma", new AttributeValue() { N = item.Sigma.ToString(CultureInfo.InvariantCulture) } },
                { "HighestRating", new AttributeValue() { N = item.HighestRating.ToString(CultureInfo.InvariantCulture) } },
                { "LowestRating", new AttributeValue() { N = item.LowestRating.ToString(CultureInfo.InvariantCulture) } },
                { "MatchesPlayed", new AttributeValue() { N = item.MatchesPlayed.ToString(CultureInfo.InvariantCulture) } }
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
                Modus = MatchModus.Ranked,
                Type = type,
            };
        }
    }
}
