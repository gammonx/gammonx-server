using GammonX.DynamoDb.Items;
using GammonX.Models.Enums;

namespace GammonX.DynamoDb.Stats
{
	internal static class StatsAggregator
	{
        /// <summary>
        /// Calculates the weighted average of double values.
        /// </summary>
        /// <typeparam name="T">Type to provide the property selectors.</typeparam>
        /// <param name="items">Item list to analyze.</param>
        /// <param name="valueSelector">Double property selector.</param>
        /// <param name="weightSelector">Weigth selector.</param>
        /// <returns>The weighted average double based on the given selectors.</returns>
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

        /// <summary>
        /// Calculates the weighted average of TimeSpan values.
        /// </summary>
        /// <typeparam name="T">Type to provide the property selectors.</typeparam>
        /// <param name="items">Item list to analyze.</param>
        /// <param name="valueSelector">TimeSpan property selector.</param>
        /// <param name="weightSelector">Weigth selector.</param>
        /// <returns>The weighted average TimeSpan based on the given selectors.</returns>
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

        /// <summary>
        /// Evaluates the current and longest win streak from a list of matches.
        /// </summary>
        /// <param name="matches">Matches to analyze.</param>
        /// <returns>The current and longest streak in the given match list.</returns>
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
