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
        /// <param name="weightSelector">Weight selector.</param>
        /// <returns>The weighted average double based on the given selectors.</returns>
        public static double WeightedAverage<T>(
			IEnumerable<T> items,
			Func<T, double> valueSelector,
			Func<T, double> weightSelector)
		{
			ArgumentNullException.ThrowIfNull(items);
			ArgumentNullException.ThrowIfNull(valueSelector);
			ArgumentNullException.ThrowIfNull(weightSelector);

			double totalWeight = 0;
			double weightedSum = 0;

			foreach (var item in items)
			{
				if (item is null)
					continue;

				var weight = weightSelector(item);
				if (weight <= 0 || !double.IsFinite(weight))
					continue;

				var value = valueSelector(item);
				if (!double.IsFinite(value))
					continue;

				var contribution = value * weight;
				var nextWeightedSum = weightedSum + contribution;
				var nextTotalWeight = totalWeight + weight;
				if (!double.IsFinite(contribution) || !double.IsFinite(nextWeightedSum) || !double.IsFinite(nextTotalWeight))
					continue;

				weightedSum = nextWeightedSum;
				totalWeight = nextTotalWeight;
			}

			return totalWeight == 0 ? 0 : weightedSum / totalWeight;
		}

        /// <summary>
        /// Calculates the weighted average of TimeSpan values.
        /// </summary>
        /// <typeparam name="T">Type to provide the property selectors.</typeparam>
        /// <param name="items">Item list to analyze.</param>
        /// <param name="valueSelector">TimeSpan property selector.</param>
        /// <param name="weightSelector">Weight selector.</param>
        /// <returns>The weighted average TimeSpan based on the given selectors.</returns>
        public static TimeSpan WeightedAverage<T>(
			IEnumerable<T> items,
			Func<T, TimeSpan> valueSelector,
			Func<T, double> weightSelector)
		{
			ArgumentNullException.ThrowIfNull(items);
			ArgumentNullException.ThrowIfNull(valueSelector);
			ArgumentNullException.ThrowIfNull(weightSelector);

			double totalWeight = 0;
			double weightedSum = 0;

			foreach (var item in items)
			{
				if (item is null)
					continue;

				var weight = weightSelector(item);
				if (weight <= 0 || !double.IsFinite(weight))
					continue;

				var value = valueSelector(item);
				if (value < TimeSpan.Zero)
					continue;

				var contribution = value.Ticks * weight;
				var nextWeightedSum = weightedSum + contribution;
				var nextTotalWeight = totalWeight + weight;
				if (!double.IsFinite(contribution) || !double.IsFinite(nextWeightedSum) || !double.IsFinite(nextTotalWeight))
					continue;

				weightedSum = nextWeightedSum;
				totalWeight = nextTotalWeight;
			}

			if (totalWeight == 0)
				return TimeSpan.Zero;

			var averageTicks = weightedSum / totalWeight;
			if (!double.IsFinite(averageTicks))
				return TimeSpan.Zero;
			if (averageTicks >= TimeSpan.MaxValue.Ticks)
				return TimeSpan.MaxValue;

			return TimeSpan.FromTicks(Convert.ToInt64(averageTicks));
		}

        /// <summary>
        /// Evaluates the current and longest win streak from a list of matches.
        /// </summary>
        /// <param name="matches">Matches to analyze.</param>
        /// <returns>The current and longest streak in the given match list.</returns>
        public static (int CurrentStreak, int LongestStreak) CalculateWinStreaks(IEnumerable<MatchItem> matches)
		{
			ArgumentNullException.ThrowIfNull(matches);

			var ordered = matches
				.Where(m => m is not null)
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
				else if (match.Result == MatchResult.Lost)
				{
					current = 0;
				}
			}

			// current streak is the streak at the END of the list
			return (current, longest);
		}
	}
}
