namespace GammonX.Mars.Training.Sidecars;

/// <summary>
/// Builds overall and grouped diagnostics from exploration decisions.
/// </summary>
public static class ExplorationBroker
{
    /// <summary>
    /// Analyzes decisions overall and by mode, phase, opponent, and candidate-count bucket.
    /// </summary>
    /// <param name="decisions">The exploration decisions to analyze.</param>
    /// <param name="reservoirCapacity">The bounded sample size used for score-gap quantiles.</param>
    /// <returns>A report containing the overall summary and grouped summaries.</returns>
    public static ExplorationDiagnosticsReport Analyze(
        IEnumerable<ExplorationDecision> decisions,
        int reservoirCapacity = 10_000)
    {
        ArgumentNullException.ThrowIfNull(decisions);

        var overall = new ExplorationGapAccumulator(reservoirCapacity);
        var groups = new Dictionary<ExplorationDiagnosticsKey, ExplorationGapAccumulator>();

        // Each decision contributes to both the overall summary and exactly one group.
        foreach (var decision in decisions)
        {
            overall.Add(decision);

            var key = new ExplorationDiagnosticsKey(
                decision.Modus,
                decision.EarlyPhase,
                decision.AgainstBot,
                GetCandidateBucket(decision.CandidateCount));

            if (!groups.TryGetValue(key, out var accumulator))
            {
                accumulator = new ExplorationGapAccumulator(reservoirCapacity);
                groups.Add(key, accumulator);
            }

            accumulator.Add(decision);
        }

        return new ExplorationDiagnosticsReport(
            overall.Complete(),
            groups.ToDictionary(pair => pair.Key, pair => pair.Value.Complete()));
    }

    /// <summary>
    /// Maps a positive candidate count to a stable diagnostics bucket.
    /// </summary>
    /// <param name="candidateCount">The number of candidate moves.</param>
    /// <returns>One of <c>1</c>, <c>2-3</c>, <c>4-7</c>, <c>8-15</c>, or <c>16+</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the candidate count is not positive.</exception>
    public static string GetCandidateBucket(int candidateCount)
    {
        if (candidateCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(candidateCount), candidateCount, "The candidate count must be greater than zero.");
        }

        return candidateCount switch
        {
            1 => "1",
            <= 3 => "2-3",
            <= 7 => "4-7",
            <= 15 => "8-15",
            _ => "16+"
        };
    }
}