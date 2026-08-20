using GammonX.Mars.Training.Generator;

namespace GammonX.Mars.Training.Sidecars
{
    /// <summary>
    /// Accumulates exploration score-gap statistics using a bounded random reservoir.
    /// </summary>
    /// <remarks>
    /// The reservoir keeps memory usage bounded while providing an approximate sample for
    /// percentile calculations. Counts, sums, minimums, and maximums are maintained exactly.
    /// </remarks>
    internal sealed class ExplorationGapAccumulator
    {
        private readonly int _reservoirCapacity;
        private readonly List<double> _reservoir;
        private readonly Dictionary<ExplorationChoice, long> _choiceCounts = [];
        private readonly Dictionary<int, long> _selectedRankCounts = [];
        private long _decisionCount;
        private long _gapCount;
        private double _gapSum;
        private double _minGap = double.PositiveInfinity;
        private double _maxGap = double.NegativeInfinity;

        /// <summary>
        /// Initializes an accumulator with the maximum number of gaps retained for quantiles.
        /// </summary>
        /// <param name="reservoirCapacity">The positive reservoir sample capacity.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the capacity is not positive.</exception>
        public ExplorationGapAccumulator(int reservoirCapacity)
        {
            if (reservoirCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(reservoirCapacity), reservoirCapacity, "The reservoir capacity must be greater than zero.");
            }

            _reservoirCapacity = reservoirCapacity;
            _reservoir = new List<double>(reservoirCapacity);
        }

        /// <summary>
        /// Adds one exploration decision to the aggregate statistics.
        /// </summary>
        /// <param name="decision">The decision to record.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a present score gap is not finite or is negative.</exception>
        public void Add(ExplorationDecision decision)
        {
            _decisionCount++;
            _choiceCounts[decision.Choice] = _choiceCounts.GetValueOrDefault(decision.Choice) + 1;
            if (decision.SelectedRank.HasValue)
            {
                _selectedRankCounts[decision.SelectedRank.Value] =
                    _selectedRankCounts.GetValueOrDefault(decision.SelectedRank.Value) + 1;
            }

            if (!decision.ScoreGap.HasValue)
                return;

            var gap = decision.ScoreGap.Value;
            if (!double.IsFinite(gap) || gap < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(decision), "The score gap must be finite and non-negative.");
            }

            _gapCount++;
            _gapSum += gap;
            _minGap = Math.Min(_minGap, gap);
            _maxGap = Math.Max(_maxGap, gap);

            // Vitter-style reservoir sampling gives each observed gap a bounded chance of retention.
            if (_reservoir.Count < _reservoirCapacity)
            {
                _reservoir.Add(gap);
                return;
            }

            var replacementIndex = Random.Shared.NextInt64(_gapCount);
            if (replacementIndex < _reservoirCapacity)
            {
                _reservoir[(int)replacementIndex] = gap;
            }
        }

        /// <summary>
        /// Creates a summary of the decisions and score gaps accumulated so far.
        /// </summary>
        /// <returns>A summary containing exact aggregates and sample-based quantiles.</returns>
        public ExplorationGapSummary Complete()
        {
            // We sort only the retained sample, the full gap history is intentionally not stored.
            var sortedGaps = _reservoir.OrderBy(value => value).ToArray();
            return new ExplorationGapSummary(
                _decisionCount,
                _gapCount,
                _decisionCount - _gapCount,
                _gapCount == 0 ? null : _minGap,
                _gapCount == 0 ? null : _gapSum / _gapCount,
                _gapCount == 0 ? null : _maxGap,
                GetQuantile(sortedGaps, 0.10d),
                GetQuantile(sortedGaps, 0.25d),
                GetQuantile(sortedGaps, 0.50d),
                GetQuantile(sortedGaps, 0.75d),
                GetQuantile(sortedGaps, 0.90d),
                GetQuantile(sortedGaps, 0.95d),
                new Dictionary<ExplorationChoice, long>(_choiceCounts),
                new Dictionary<int, long>(_selectedRankCounts));
        }

        /// <summary>
        /// Interpolates a quantile from an ascending sequence of sampled values.
        /// </summary>
        /// <param name="sortedValues">The sampled values in ascending order.</param>
        /// <param name="quantile">The requested quantile in the range [0, 1].</param>
        /// <returns>The interpolated quantile, or <see langword="null"/> when no values exist.</returns>
        private static double? GetQuantile(IReadOnlyList<double> sortedValues, double quantile)
        {
            if (sortedValues.Count == 0)
                return null;

            var position = (sortedValues.Count - 1) * quantile;
            var lowerIndex = (int)Math.Floor(position);
            var upperIndex = (int)Math.Ceiling(position);
            if (lowerIndex == upperIndex)
                return sortedValues[lowerIndex];

            var fraction = position - lowerIndex;
            return sortedValues[lowerIndex]
                + (sortedValues[upperIndex] - sortedValues[lowerIndex]) * fraction;
        }
    }
}
