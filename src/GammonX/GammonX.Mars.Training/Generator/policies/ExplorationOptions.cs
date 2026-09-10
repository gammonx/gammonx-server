namespace GammonX.Mars.Training.Generator
{
    /// <summary>
    /// Keeps exploration near the ranked moves while reserving a small probability for any legal move.
    /// </summary>
    public sealed record ExplorationOptions
    {
        /// <summary>
        /// Gets the count of top-ranked moves to consider for exploration.
        /// </summary>
        public int TopRankedCount { get; init; } = 10;

        /// <summary>
        /// Gets the count of turns which are considered "early" for exploration.
        /// </summary>
        public int EarlyTurnCount { get; init; } = 20;

        /// <summary>
        /// Exploration probability for the <see cref="EarlyTurnCount"/> turns.
        /// </summary>
        public float EarlyRankedExplorationProbability { get; init; } = 0.24f;

        /// <summary>
        /// Exploration probability for the remaining turns.
        /// </summary>
        public float LateRankedExplorationProbability { get; init; } = 0.08f;

        /// <summary>
        /// Gets the probability of exploring any legal move, regardless of rank.
        /// </summary>
        public float AllLegalExplorationProbability { get; init; } = 0.01f;

        /// <summary>
        /// Gets a boolean indicating whether the score gap diagnostics is enabled.
        /// </summary>
        public bool CollectScoreGapDiagnostics { get; init; }

        /// <summary>
        /// Gets a boolean indicating whether score gap-aware exploration is enabled.
        /// </summary>
        public bool ScoreGapAwareExplorationEnabled { get; init; }

        /// <summary>
        /// Gets a lower score gap threshold which indicates identical scores.
        /// </summary>
        public double ScoreGapSmallThreshold { get; init; } = 0.1d;

        /// <summary>
        /// Gets a higher score gap threshold which indicates a significant score gap.
        /// </summary>
        public double ScoreGapLargeThreshold { get; init; } = 0.5d;

        /// <summary>
        /// Gets the exploration multiplier applied if a small gap is detected.
        /// </summary>
        public float SmallGapRankedExplorationMultiplier { get; init; } = 1.5f;

        /// <summary>
        /// Gets the exploration multiplier applied if a large gap is detected.
        /// </summary>
        public float LargeGapRankedExplorationMultiplier { get; init; } = 0.5f;

        internal void Validate()
        {
            if (TopRankedCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(TopRankedCount), TopRankedCount, "The top-ranked count must be greater than zero.");

            if (EarlyTurnCount < 0)
                throw new ArgumentOutOfRangeException(nameof(EarlyTurnCount), EarlyTurnCount, "The early turn count cannot be negative.");

            if (!float.IsFinite(EarlyRankedExplorationProbability)
                || EarlyRankedExplorationProbability < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(EarlyRankedExplorationProbability), EarlyRankedExplorationProbability, "The exploration probability must be finite and non-negative.");
            }

            if (!float.IsFinite(LateRankedExplorationProbability)
                || LateRankedExplorationProbability < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(LateRankedExplorationProbability), LateRankedExplorationProbability, "The exploration probability must be finite and non-negative.");
            }

            if (!float.IsFinite(AllLegalExplorationProbability)
                || AllLegalExplorationProbability < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(AllLegalExplorationProbability), AllLegalExplorationProbability, "The exploration probability must be finite and non-negative.");
            }

            if (!double.IsFinite(ScoreGapSmallThreshold) || ScoreGapSmallThreshold < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(ScoreGapSmallThreshold), ScoreGapSmallThreshold, "The small score-gap threshold must be finite and non-negative.");
            }

            if (!double.IsFinite(ScoreGapLargeThreshold) || ScoreGapLargeThreshold < ScoreGapSmallThreshold)
            {
                throw new ArgumentOutOfRangeException(nameof(ScoreGapLargeThreshold), ScoreGapLargeThreshold, "The large score-gap threshold must be finite and at least the small threshold.");
            }

            if (!float.IsFinite(SmallGapRankedExplorationMultiplier)
                || SmallGapRankedExplorationMultiplier < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(SmallGapRankedExplorationMultiplier), SmallGapRankedExplorationMultiplier, "The small-gap multiplier must be finite and non-negative.");
            }

            if (!float.IsFinite(LargeGapRankedExplorationMultiplier)
                || LargeGapRankedExplorationMultiplier < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(LargeGapRankedExplorationMultiplier), LargeGapRankedExplorationMultiplier, "The large-gap multiplier must be finite and non-negative.");
            }

            var maximumGapMultiplier = MathF.Max(
                SmallGapRankedExplorationMultiplier,
                LargeGapRankedExplorationMultiplier);
            var maximumExplorationProbability = AllLegalExplorationProbability
                + MathF.Max(EarlyRankedExplorationProbability, LateRankedExplorationProbability)
                * (ScoreGapAwareExplorationEnabled ? maximumGapMultiplier : 1f);
            if (maximumExplorationProbability > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(AllLegalExplorationProbability), "The all-legal and ranked exploration probabilities cannot exceed one together.");
            }
        }
    }
}
