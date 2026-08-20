namespace GammonX.Mars.NN.Models;

/// <summary>
/// Describes violations found in a five-head game-outcome prediction.
/// </summary>
/// <param name="IsValid">Gets whether all constraints are satisfied within the requested tolerance and every value is finite.</param>
/// <param name="ViolationCount">Gets the number of constraint categories whose violation exceeds the tolerance.</param>
/// <param name="HasNonFiniteValue">Gets whether at least one prediction is NaN or infinite.</param>
/// <param name="RangeViolation">Gets the largest distance of a prediction outside the inclusive range [0, 1].</param>
/// <param name="WinGammonHierarchyViolation">Gets the amount by which the win-gammon probability exceeds the win probability.</param>
/// <param name="WinBackgammonHierarchyViolation">Gets the amount by which the win-backgammon probability exceeds the win-gammon probability.</param>
/// <param name="LoseGammonComplementViolation">Gets the amount by which the lose-gammon probability exceeds the probability of losing.</param>
/// <param name="LoseBackgammonHierarchyViolation">Gets the amount by which the lose-backgammon probability exceeds the lose-gammon probability.</param>
/// <param name="MaxViolation">Gets the largest individual violation.</param>
/// <param name="TotalViolation">Gets the sum of all individual violations.</param>
public sealed record GameOutcomeConstraintReport(
    bool IsValid,
    int ViolationCount,
    bool HasNonFiniteValue,
    double RangeViolation,
    double WinGammonHierarchyViolation,
    double WinBackgammonHierarchyViolation,
    double LoseGammonComplementViolation,
    double LoseBackgammonHierarchyViolation,
    double MaxViolation,
    double TotalViolation);

/// <summary>
/// Contains probabilities adjusted to satisfy the game-outcome constraints.
/// </summary>
/// <param name="WinP">Gets the probability of winning.</param>
/// <param name="WinGammonP">Gets the probability of winning with a gammon.</param>
/// <param name="WinBackgammonP">Gets the probability of winning with a backgammon.</param>
/// <param name="LoseGammonP">Gets the probability of losing with a gammon.</param>
/// <param name="LoseBackgammonP">Gets the probability of losing with a backgammon.</param>
/// <param name="CorrectionMagnitude">Gets the sum of the absolute corrections applied to the five input heads.</param>
public sealed record ProjectedGameOutcome(
    double WinP,
    double WinGammonP,
    double WinBackgammonP,
    double LoseGammonP,
    double LoseBackgammonP,
    double CorrectionMagnitude)
{
    /// <summary>
    /// Gets the complementary probability of losing.
    /// </summary>
    public double LoseP => 1d - WinP;

    /// <summary>
    /// Gets whether projection changed at least one input value.
    /// </summary>
    public bool WasProjected => CorrectionMagnitude > 0d;
}

/// <summary>
/// Validates and projects neural-network game-outcome probabilities.
/// </summary>
/// <remarks>
/// The five heads are ordered as win, win gammon, win backgammon, lose gammon,
/// and lose backgammon. The hierarchy requires win backgammon ≤ win gammon ≤ win,
/// and lose backgammon ≤ lose gammon ≤ 1 - win. For example, a win probability
/// of 0.7 permits at most 0.7 for win gammon and at most 0.3 for lose gammon.
/// </remarks>
public static class GameOutcomeConstraintValidator
{
    /// <summary>
    /// Gets the number of probability heads required by this validator.
    /// </summary>
    public const int FullHeadCount = 5;

    /// <summary>
    /// Gets the default tolerance used when comparing violations.
    /// </summary>
    public const double DefaultTolerance = 1e-6;

    /// <summary>
    /// Validates single-precision predictions using the default constraint definitions.
    /// </summary>
    /// <param name="predictions">The five predictions in the documented head order.</param>
    /// <param name="tolerance">The maximum permitted violation for a constraint category.</param>
    /// <returns>A report containing validity and detailed violation measurements.</returns>
    public static GameOutcomeConstraintReport Validate(
        IReadOnlyList<float> predictions,
        double tolerance = DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(predictions);
        return Validate(predictions.Select(value => (double)value).ToArray(), tolerance);
    }

    /// <summary>
    /// Validates double-precision predictions against range and probability hierarchy constraints.
    /// </summary>
    /// <param name="predictions">The five predictions in the documented head order.</param>
    /// <param name="tolerance">The maximum permitted violation for a constraint category.</param>
    /// <returns>A report containing validity and detailed violation measurements.</returns>
    public static GameOutcomeConstraintReport Validate(
        IReadOnlyList<double> predictions,
        double tolerance = DefaultTolerance)
    {
        ValidateInput(predictions, tolerance);

        var win = predictions[0];
        var winGammon = predictions[1];
        var winBackgammon = predictions[2];
        var loseGammon = predictions[3];
        var loseBackgammon = predictions[4];
        var hasNonFiniteValue = predictions.Any(value => !double.IsFinite(value));

        // Each probability must be within [0, 1]; non-finite values are reported as violations.
        var rangeViolation = predictions
            .Select(GetRangeViolation)
            .DefaultIfEmpty(0d)
            .Max();

        // More specific outcomes are subsets of their broader outcomes.
        var winGammonHierarchyViolation = GetExcess(winGammon - win);
        var winBackgammonHierarchyViolation = GetExcess(winBackgammon - winGammon);
        var loseGammonComplementViolation = GetExcess(loseGammon - (1d - win));
        var loseBackgammonHierarchyViolation = GetExcess(loseBackgammon - loseGammon);

        var violations = new[]
        {
            rangeViolation,
            winGammonHierarchyViolation,
            winBackgammonHierarchyViolation,
            loseGammonComplementViolation,
            loseBackgammonHierarchyViolation
        };

        // Count categories above tolerance, while retaining exact magnitudes for diagnostics.
        var violationCount = violations.Count(value => value > tolerance);
        var maxViolation = violations.Max();
        var totalViolation = violations.Sum();

        return new GameOutcomeConstraintReport(
            violationCount == 0 && !hasNonFiniteValue,
            violationCount,
            hasNonFiniteValue,
            rangeViolation,
            winGammonHierarchyViolation,
            winBackgammonHierarchyViolation,
            loseGammonComplementViolation,
            loseBackgammonHierarchyViolation,
            maxViolation,
            totalViolation);
    }

    /// <summary>
    /// Validates the probability properties of a game-outcome model.
    /// </summary>
    /// <param name="outcome">The model whose five probability heads should be validated.</param>
    /// <param name="tolerance">The maximum permitted violation for a constraint category.</param>
    /// <returns>A report containing validity and detailed violation measurements.</returns>
    public static GameOutcomeConstraintReport Validate(GameOutcomeModel outcome, double tolerance = DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return Validate(ToPredictions(outcome), tolerance);
    }

    /// <summary>
    /// Projects single-precision predictions into the valid constrained probability space.
    /// </summary>
    /// <param name="predictions">The five predictions in the documented head order.</param>
    /// <returns>A constrained outcome and the magnitude of the applied correction.</returns>
    public static ProjectedGameOutcome Project(IReadOnlyList<float> predictions)
    {
        ArgumentNullException.ThrowIfNull(predictions);
        return Project(predictions.Select(value => (double)value).ToArray());
    }

    /// <summary>
    /// Projects double-precision predictions into the valid constrained probability space.
    /// </summary>
    /// <param name="predictions">The five predictions in the documented head order.</param>
    /// <returns>A constrained outcome and the magnitude of the applied correction.</returns>
    public static ProjectedGameOutcome Project(IReadOnlyList<double> predictions)
    {
        ValidateInput(predictions, DefaultTolerance);

        // We normalize invalid numeric values and clamp finite values before applying hierarchy rules.
        var rawWin = SanitizeProbability(predictions[0]);
        var rawWinGammon = SanitizeProbability(predictions[1]);
        var rawWinBackgammon = SanitizeProbability(predictions[2]);
        var rawLoseGammon = SanitizeProbability(predictions[3]);
        var rawLoseBackgammon = SanitizeProbability(predictions[4]);

        // We use successive upper bounds so every specific outcome remains a subset of its parent.
        var win = rawWin;
        var winGammon = Math.Min(rawWinGammon, win);
        var winBackgammon = Math.Min(rawWinBackgammon, winGammon);
        var loseGammon = Math.Min(rawLoseGammon, 1d - win);
        var loseBackgammon = Math.Min(rawLoseBackgammon, loseGammon);

        var correctionMagnitude = Distance(predictions[0], win)
            + Distance(predictions[1], winGammon)
            + Distance(predictions[2], winBackgammon)
            + Distance(predictions[3], loseGammon)
            + Distance(predictions[4], loseBackgammon);

        return new ProjectedGameOutcome(
            win,
            winGammon,
            winBackgammon,
            loseGammon,
            loseBackgammon,
            correctionMagnitude);
    }

    /// <summary>
    /// Projects the probability properties of a game-outcome model into the valid constrained space.
    /// </summary>
    /// <param name="outcome">The model whose five probability heads should be projected.</param>
    /// <returns>A constrained outcome and the magnitude of the applied correction.</returns>
    public static ProjectedGameOutcome Project(GameOutcomeModel outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return Project(ToPredictions(outcome));
    }

    private static double[] ToPredictions(GameOutcomeModel outcome)
        =>
        [
            outcome.WinP,
            outcome.WinGammonP,
            outcome.WinBackgammonP,
            outcome.LoseGammonP,
            outcome.LoseBackgammonP
        ];

    private static void ValidateInput(IReadOnlyList<double> predictions, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(predictions);
        if (predictions.Count != FullHeadCount)
        {
            throw new ArgumentException(
                $"Expected {FullHeadCount} outcome heads, but received {predictions.Count}.",
                nameof(predictions));
        }

        if (!double.IsFinite(tolerance) || tolerance < 0d)
            throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "The tolerance must be finite and non-negative.");
    }

    private static double GetRangeViolation(double value)
    {
        if (!double.IsFinite(value))
            return 1d;

        return Math.Max(0d, Math.Max(-value, value - 1d));
    }

    private static double GetExcess(double value) => double.IsFinite(value) ? Math.Max(0d, value) : 1d;

    private static double SanitizeProbability(double value)
    {
        if (double.IsNaN(value))
            return 0.5d;
        if (double.IsPositiveInfinity(value))
            return 1d;
        if (double.IsNegativeInfinity(value))
            return 0d;
        return Math.Clamp(value, 0d, 1d);
    }

    private static double Distance(double raw, double projected)
        => double.IsFinite(raw) ? Math.Abs(raw - projected) : 1d;
}
