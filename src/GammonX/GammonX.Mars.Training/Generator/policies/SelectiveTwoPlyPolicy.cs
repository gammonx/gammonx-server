namespace GammonX.Mars.Training.Generator;

/// <summary>
/// Represents the reasons for performing a full search audit in <see cref="SelectiveTwoPlyPolicy"/>.
/// </summary>
public enum SelectiveTwoPlyReason
{
    /// <summary>
    /// Policy not enabled.
    /// </summary>
    Disabled,
    /// <summary>
    /// Only a single candidate exists.
    /// </summary>
    SingleCandidate,
    /// <summary>
    /// The score gap between candidates is too large.
    /// </summary>
    GapTooLarge,
    /// <summary>
    /// The 1-ply evaluation is too close to call, an evaluation is required.
    /// </summary>
    Ambiguous,
    /// <summary>
    /// The audit roll hit and a evaluation is required.
    /// </summary>
    Audit
}

public readonly record struct SelectiveTwoPlyDecision(SelectiveTwoPlyReason Reason, int CandidateCount)
{
    public bool ShouldEvaluate => Reason is SelectiveTwoPlyReason.Ambiguous or SelectiveTwoPlyReason.Audit;
}

/// <summary>
/// Represents a policy for selectively performing a 2-ply evaluation of candidate moves based on the results of a 1-ply evaluation.
/// </summary>
/// <remarks>
/// This policy is designed to reduce the number of 2-ply evaluations performed during training by only performing a 2-ply evaluation
/// when it is deemed necessary based on the results of a 1-ply evaluation.
/// </remarks>
internal static class SelectiveTwoPlyPolicy
{
    /// <summary>
    /// Determines whether a 2-ply evaluation should be performed based on the results of a 1-ply evaluation.
    /// </summary>
    /// <param name="resultCount">The number of candidate moves.</param>
    /// <param name="scoreGap">The score gap between the top candidate moves.</param>
    /// <param name="auditRoll">A random value used to determine whether to perform an audit.</param>
    /// <param name="options">The options controlling the selective 2-ply policy.</param>
    /// <returns>A <see cref="SelectiveTwoPlyDecision"/> indicating whether a 2-ply evaluation should be performed and the reason for the decision.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="resultCount"/> is less than or equal to zero,
    /// when <paramref name="auditRoll"/> is not finite or not in [0, 1], or when <paramref name="scoreGap"/> is not finite or negative
    /// when multiple candidates exist.</exception>
    public static SelectiveTwoPlyDecision Select(int resultCount, double? scoreGap, float auditRoll, SelectiveTwoPlyOptions options)
    {
        if (resultCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(resultCount), resultCount, "The result count must be greater than zero.");

        if (!float.IsFinite(auditRoll) || auditRoll < 0f || auditRoll > 1f)
            throw new ArgumentOutOfRangeException(nameof(auditRoll), auditRoll, "The audit roll must be finite and in [0, 1].");

        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!options.Enabled)
            return new SelectiveTwoPlyDecision(SelectiveTwoPlyReason.Disabled, 0);

        if (resultCount == 1)
            return new SelectiveTwoPlyDecision(SelectiveTwoPlyReason.SingleCandidate, 0);

        if (!scoreGap.HasValue || !double.IsFinite(scoreGap.Value) || scoreGap.Value < 0d)
            throw new ArgumentOutOfRangeException(nameof(scoreGap), scoreGap, "The score gap must be finite and non-negative when multiple candidates exist.");

        if (auditRoll < options.FullSearchAuditProbability)
            return new SelectiveTwoPlyDecision(SelectiveTwoPlyReason.Audit, resultCount);

        if (scoreGap.Value <= options.MaximumOnePlyGap)
        {
            return new SelectiveTwoPlyDecision(
                SelectiveTwoPlyReason.Ambiguous,
                Math.Min(options.MaximumCandidates, resultCount));
        }

        return new SelectiveTwoPlyDecision(SelectiveTwoPlyReason.GapTooLarge, 0);
    }
}