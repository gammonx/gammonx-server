using Amazon.DynamoDBv2.Model;

using GammonX.DynamoDb.Stats;

using GammonX.Models.Enums;
using GammonX.Models.Helpers;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Items
{
	public class PlayerStatsItemFactory : IItemFactory<PlayerStatsItem>
	{
		/// <summary>
		/// Gets a primary key like 'PLAYER#{playerId}'
		/// </summary>
		public string PKFormat => "PLAYER#{0}";

		/// <summary>
		/// Gets a sort key like 'STATS#{Variant}#{Type}#{Modus}'
		/// </summary>
		public string SKPrefix => "STATS#";

		// <inheritdoc />
		public string SKFormat => "STATS#{0}#{1}#{2}";

		// <inheritdoc />
		public string GSI1PKFormat => throw new InvalidOperationException("Global search index not applicable for this item type");

		// <inheritdoc />
		public string GSI1SKFormat => throw new InvalidOperationException("Global search index not applicable for this item type");

		// <inheritdoc />
		public string GSI1SKPrefix => throw new InvalidOperationException("Global search index not applicable for this item type");

		// <inheritdoc />
		public PlayerStatsItem CreateItem(Dictionary<string, AttributeValue> item)
		{
			var playerStatsItem = new PlayerStatsItem
			{
				PlayerId = Guid.Parse(item["PlayerId"].S),
				Variant = Enum.Parse<MatchVariant>(item["Variant"].S),
				Type = Enum.Parse<MatchType>(item["Type"].S),
				Modus = Enum.Parse<MatchModus>(item["Modus"].S),
				MatchesPlayed = int.Parse(item["MatchesPlayed"].N),
				MatchesWon = int.Parse(item["MatchesWon"].N),
				MatchesLost = int.Parse(item["MatchesLost"].N),
				WinRate = double.Parse(item["WinRate"].N),
				WinStreak = int.Parse(item["WinStreak"].N),
				LongestWinStreak = int.Parse(item["LongestWinStreak"].N),
				TotalPlayTime = TimeSpan.Parse(item["TotalPlayTime"].S),
				LastMatch = DateTimeHelper.ParseFlexible(item["LastMatch"].S),
				MatchesLast7 = int.Parse(item["MatchesLast7"].N),
				MatchesLast30 = int.Parse(item["MatchesLast30"].N),
				AvgGammons = double.Parse(item["AvgGammons"].N),
				AvgBackgammons = double.Parse(item["AvgBackgammons"].N),
				AvgDuration = TimeSpan.Parse(item["AvgDuration"].S),
				WAvgPipesLeft = double.Parse(item["WAvgPipesLeft"].N),
				WAvgDoubleDices = double.Parse(item["WAvgDoubleDices"].N),
				WAvgTurns = double.Parse(item["WAvgTurns"].N),
				WAvgDoubles = double.Parse(item["WAvgDoubles"].N),
				WAvgDuration = TimeSpan.Parse(item["WAvgDuration"].S),
			};
			return playerStatsItem;
		}

		// <inheritdoc />
		public Dictionary<string, AttributeValue> CreateItem(PlayerStatsItem item)
		{
			var variantStr = item.Variant.ToString();
			var modusStr = item.Modus.ToString();
			var typeStr = item.Type.ToString();
			var itemDict = new Dictionary<string, AttributeValue>
			{
				{ "PK", new AttributeValue(item.PK) },
				{ "SK", new AttributeValue(item.SK) },
				{ "ItemType", new AttributeValue(item.ItemType) },
				{ "PlayerId", new AttributeValue(item.PlayerId.ToString()) },
				{ "Variant", new AttributeValue(variantStr) },
				{ "Modus", new AttributeValue(modusStr) },
				{ "Type", new AttributeValue(typeStr) },
				{ "MatchesPlayed", new AttributeValue() { N = item.MatchesPlayed.ToString() } },
				{ "MatchesWon", new AttributeValue() { N = item.MatchesWon.ToString() } },
				{ "MatchesLost", new AttributeValue() { N = item.MatchesLost.ToString() } },
				{ "WinRate", new AttributeValue() { N = item.WinRate.ToString() } },
				{ "WinStreak", new AttributeValue() { N = item.WinStreak.ToString() } },
				{ "LongestWinStreak", new AttributeValue() { N = item.LongestWinStreak.ToString() } },
				{ "TotalPlayTime", new AttributeValue() { S = item.TotalPlayTime.ToString() } },
				{ "LastMatch", new AttributeValue() { S = item.LastMatch.ToString() } },
				{ "MatchesLast7", new AttributeValue() { N = item.MatchesLast7.ToString() } },
				{ "MatchesLast30", new AttributeValue() { N = item.MatchesLast30.ToString() } },
				{ "AvgGammons", new AttributeValue() { N = item.AvgGammons.ToString() } },
				{ "AvgBackgammons", new AttributeValue() { N = item.AvgBackgammons.ToString() } },
				{ "AvgDuration", new AttributeValue() { S = item.AvgDuration.ToString() } },
				{ "WAvgPipesLeft", new AttributeValue() { N = item.WAvgPipesLeft.ToString() } },
				{ "WAvgDoubleDices", new AttributeValue() { N = item.WAvgDoubleDices.ToString() } },
				{ "WAvgTurns", new AttributeValue() { N = item.WAvgTurns.ToString() } },
				{ "WAvgDoubles", new AttributeValue() { N = item.WAvgDoubles.ToString() } },
				{ "WAvgDuration", new AttributeValue() { S = item.WAvgDuration.ToString() } },
			};
			return itemDict;
		}

		public static PlayerStatsItem CreateItem(
			Guid playerId,
			MatchVariant variant,
			MatchType type,
			MatchModus modus,
			IEnumerable<MatchItem> matchItems)
		{
			var matches = matchItems.OrderBy(mi => mi.EndedAt).ToList();

			if (matches.Count == 0)
				throw new ArgumentException("The match list must not be empty for stat calculation");

			var matchesPlayed = matches.Count;
			var matchesWon = matches.Count(m => m.Result == MatchResult.Won);
			var matchesLost = matches.Count(m => m.Result == MatchResult.Lost);
			var winRate = (double)matchesWon / matchesPlayed * 100.0;
			var (CurrentStreak, LongestStreak) = StatsAggregator.CalculateWinStreaks(matches);
			var winStreak = CurrentStreak;
			var longestWinStreak = LongestStreak;
			var lastMatch = matches.Last().EndedAt;
			var totalPlayTime = TimeSpan.FromTicks(matches.Sum(m => m.Duration.Ticks));
			var now = DateTime.UtcNow;
			var matchesLast7 = matches.Count(m => m.EndedAt > now.AddDays(-7));
			var matchesLast30 = matches.Count(m => m.EndedAt > now.AddDays(-30));
			var gammonCount = matches.Sum(m => m.Gammons);
			var avgGammons = 0.0;
			if (gammonCount > 0)
				avgGammons = (double)gammonCount / matchesPlayed;
			var backgammonCount = matches.Sum(m => m.Backgammons);
			var avgBackgammons = 0.0;
			if (backgammonCount > 0)
				avgBackgammons = (double)backgammonCount / matchesPlayed;
			var avgDuration = TimeSpan.FromTicks((long)matches.Average(m => m.Duration.Ticks));
			var wAvgPipesLeft = StatsAggregator.WeightedAverage(matches, m => m.AvgPipesLeft, m => m.Length);
			var wAvgDoubleDices = StatsAggregator.WeightedAverage(matches, m => m.AvgDoubleDices, m => m.Length);
			var wAvgTurns = StatsAggregator.WeightedAverage(matches, m => m.AvgTurns, m => m.Length);
			var wAvgDoubles = StatsAggregator.WeightedAverage(matches, m => m.AvgDoubles, m => m.Length);
			var wAvgDuration = StatsAggregator.WeightedAverage(matches, m => m.AvgDuration, m => m.Length);

			var playerStatsItem = new PlayerStatsItem
			{
				PlayerId = playerId,
				Variant = variant,
				Modus = modus,
				Type = type,
				MatchesPlayed = matchesPlayed,
				MatchesWon = matchesWon,
				MatchesLost = matchesLost,
				WinRate = winRate,
				WinStreak = winStreak,
				LongestWinStreak = longestWinStreak,
				TotalPlayTime = totalPlayTime,
				LastMatch = lastMatch,
				MatchesLast7 = matchesLast7,
				MatchesLast30 = matchesLast30,
				AvgGammons = avgGammons,
				AvgBackgammons = avgBackgammons,
				AvgDuration = avgDuration,
				WAvgPipesLeft = wAvgPipesLeft,
				WAvgDoubleDices = wAvgDoubleDices,
				WAvgTurns = wAvgTurns,
				WAvgDoubles = wAvgDoubles,
				WAvgDuration = wAvgDuration
			};
			return playerStatsItem;
		}
	}
}
