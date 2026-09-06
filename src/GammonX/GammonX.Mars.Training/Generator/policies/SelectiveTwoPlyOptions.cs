namespace GammonX.Mars.Training.Generator;

/// <summary>
/// Represents the options for <see cref="SelectiveTwoPlyPolicy"/>.
/// </summary>
public sealed record SelectiveTwoPlyOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the selective two-ply search is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets or sets the maximum one-ply score gap that is considered ambiguous and warrants a two-ply search.
    /// </summary>
    public double MaximumOnePlyGap { get; init; } = 0.02d;

    /// <summary>
    /// Gets the maximum number of candidate (K) that will be evaluated with a two-ply search when the one-ply score gap is below the configured threshold.
    /// </summary>
    public int MaximumCandidates { get; init; } = 5;

    /// <summary>
    /// Gets the probability of performing a full search audit.
    /// </summary>
    public float FullSearchAuditProbability { get; init; } = 0.01f;

    /// <summary>
    /// Gets a value indicating whether to collect diagnostics for the selective two-ply search.
    /// </summary>
    public bool CollectDiagnostics { get; init; } = true;

    public void Validate()
    {
        if (!double.IsFinite(MaximumOnePlyGap) || MaximumOnePlyGap < 0d)
            throw new ArgumentOutOfRangeException(nameof(MaximumOnePlyGap), MaximumOnePlyGap, "The maximum one-ply gap must be finite and non-negative.");

        if (MaximumCandidates < 2)
            throw new ArgumentOutOfRangeException(nameof(MaximumCandidates), MaximumCandidates, "At least two candidates must be evaluated.");

        if (!float.IsFinite(FullSearchAuditProbability) || FullSearchAuditProbability < 0f || FullSearchAuditProbability > 1f)
            throw new ArgumentOutOfRangeException(nameof(FullSearchAuditProbability), FullSearchAuditProbability, "The audit probability must be finite and in [0, 1].");
    }
}