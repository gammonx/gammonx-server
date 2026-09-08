namespace GammonX.Mars.Training.Generator;

internal static class RankAwareExplorationPolicy
{
    /// <summary>
    /// Selects the exploration choice for a given turn.
    /// </summary>
    public static ExplorationChoice Select(
        int turnCount,
        int resultCount,
        bool explorationEnabled,
        float explorationRoll,
        ExplorationOptions options,
        double? scoreGap = null)
    {
        if (turnCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(turnCount), turnCount, "The turn count must be greater than zero.");

        if (resultCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(resultCount), resultCount, "The result count must be greater than zero.");

        if (!float.IsFinite(explorationRoll) || explorationRoll < 0f || explorationRoll > 1f)
            throw new ArgumentOutOfRangeException(nameof(explorationRoll), explorationRoll, "The exploration roll must be finite and in [0, 1].");

        if (scoreGap.HasValue && (!double.IsFinite(scoreGap.Value) || scoreGap.Value < 0d))
            throw new ArgumentOutOfRangeException(nameof(scoreGap), scoreGap, "The score gap must be finite and non-negative.");

        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!explorationEnabled)
            return ExplorationChoice.Greedy;

        var rankedProbability = GetRankedExplorationProbability(turnCount, scoreGap, options);

        if (explorationRoll < options.AllLegalExplorationProbability)
            return ExplorationChoice.AllLegal;

        if (explorationRoll < options.AllLegalExplorationProbability + rankedProbability)
            return ExplorationChoice.Ranked;

        return ExplorationChoice.Greedy;
    }

    private static float GetRankedExplorationProbability(int turnCount, double? scoreGap, ExplorationOptions options)
    {
        // we calculate the ranked exploration probability based on the turn count and score gap
        var phaseProbability = turnCount <= options.EarlyTurnCount ? options.EarlyRankedExplorationProbability : options.LateRankedExplorationProbability;

        if (!options.ScoreGapAwareExplorationEnabled || !scoreGap.HasValue)
        {
            return phaseProbability;
        }

        var multiplier = scoreGap.Value <= options.ScoreGapSmallThreshold
            // we apply the small gap multiplier if score is lower than threshold.
            ? options.SmallGapRankedExplorationMultiplier
            : scoreGap.Value >= options.ScoreGapLargeThreshold
                // we apply the large gap multiplier if score is higher than threshold.
                ? options.LargeGapRankedExplorationMultiplier
                // if score gap is between the two thresholds, we interpolate the multiplier linearly.
                : options.SmallGapRankedExplorationMultiplier
                    + (float)((scoreGap.Value - options.ScoreGapSmallThreshold)
                        / (options.ScoreGapLargeThreshold - options.ScoreGapSmallThreshold)
                        * (options.LargeGapRankedExplorationMultiplier - options.SmallGapRankedExplorationMultiplier));

        return phaseProbability * multiplier;
    }

    public static int GetRankedCandidateIndex(int resultCount, int randomIndex, ExplorationOptions options)    {
        if (resultCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(resultCount), resultCount, "The result count must be greater than zero.");
        }

        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var topRankedCount = Math.Min(options.TopRankedCount, resultCount);
        if ((uint)randomIndex >= (uint)topRankedCount)
        {
            throw new ArgumentOutOfRangeException(nameof(randomIndex), randomIndex, $"The ranked index must be in [0, {topRankedCount}).");
        }

        return randomIndex;
    }
}