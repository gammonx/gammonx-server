namespace GammonX.Mars.Training;

/// <summary>
/// Keeps exploration near the ranked moves while reserving a small probability for any legal move.
/// </summary>
public sealed record ExplorationOptions
{
    public int TopRankedCount { get; init; } = 10;

    public int EarlyTurnCount { get; init; } = 20;

    // Keep the existing 25% early and 5% late total exploration rates.
    public float EarlyRankedExplorationProbability { get; init; } = 0.24f;

    public float LateRankedExplorationProbability { get; init; } = 0.04f;

    public float AllLegalExplorationProbability { get; init; } = 0.01f;

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

        var maximumExplorationProbability = AllLegalExplorationProbability
            + MathF.Max(EarlyRankedExplorationProbability, LateRankedExplorationProbability);
        if (maximumExplorationProbability > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(AllLegalExplorationProbability), "The all-legal and ranked exploration probabilities cannot exceed one together.");
        }
    }
}

internal enum ExplorationChoice
{
    Greedy,
    Ranked,
    AllLegal
}

internal static class RankAwareExplorationPolicy
{
    public static ExplorationChoice Select(
        int turnCount,
        int resultCount,
        bool explorationEnabled,
        float explorationRoll,
        ExplorationOptions options)
    {
        if (turnCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(turnCount), turnCount, "The turn count must be greater than zero.");

        if (resultCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(resultCount), resultCount, "The result count must be greater than zero.");

        if (!float.IsFinite(explorationRoll) || explorationRoll < 0f || explorationRoll > 1f)
            throw new ArgumentOutOfRangeException(nameof(explorationRoll), explorationRoll, "The exploration roll must be finite and in [0, 1].");

        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!explorationEnabled)
            return ExplorationChoice.Greedy;

        var rankedProbability = turnCount <= options.EarlyTurnCount
            ? options.EarlyRankedExplorationProbability
            : options.LateRankedExplorationProbability;

        if (explorationRoll < options.AllLegalExplorationProbability)
            return ExplorationChoice.AllLegal;

        if (explorationRoll < options.AllLegalExplorationProbability + rankedProbability)
            return ExplorationChoice.Ranked;

        return ExplorationChoice.Greedy;
    }

    public static int GetRankedCandidateIndex(
        int resultCount,
        int randomIndex,
        ExplorationOptions options)
    {
        if (resultCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(resultCount), resultCount, "The result count must be greater than zero.");

        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var topRankedCount = Math.Min(options.TopRankedCount, resultCount);
        if ((uint)randomIndex >= (uint)topRankedCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(randomIndex),
                randomIndex,
                $"The ranked index must be in [0, {topRankedCount}).");
        }

        return randomIndex;
    }
}