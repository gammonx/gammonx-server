using GammonX.DynamoDb.Items;
using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Stats
{
	internal static class StatsAggregator
	{
		public static double WeightedAverage<T>(
			IEnumerable<T> items,
			Func<T, double> valueSelector,
			Func<T, double> weightSelector)
		{
			double totalWeight = 0;
			double weightedSum = 0;

			foreach (var item in items)
			{
				var weight = weightSelector(item);
				weightedSum += valueSelector(item) * weight;
				totalWeight += weight;
			}

			return totalWeight == 0 ? 0 : weightedSum / totalWeight;
		}

		public static TimeSpan WeightedAverage<T>(
			IEnumerable<T> items,
			Func<T, TimeSpan> valueSelector,
			Func<T, double> weightSelector)
		{
			double totalWeight = 0;
			double weightedSum = 0;

			foreach (var item in items)
			{
				var weight = weightSelector(item);
				weightedSum += valueSelector(item).Ticks * weight;
				totalWeight += weight;
			}

			return totalWeight == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(Convert.ToInt64(weightedSum / totalWeight));
		}

		public static (int CurrentStreak, int LongestStreak) CalculateWinStreaks(IEnumerable<MatchItem> matches)
		{
			var ordered = matches
				.OrderBy(m => m.EndedAt)
				.ToList();

			int longest = 0;
			int current = 0;

			foreach (var match in ordered)
			{
				if (match.Result == MatchResult.Won)
				{
					current++;
					if (current > longest)
						longest = current;
				}
				else
				{
					// reset current streak on loss detected
					current = 0;
				}
			}

			// current streak is the streak at the END of the list
			return (current, longest);
		}
	}
}
