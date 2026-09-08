using GammonX.Mars.NN.Models;

using static TorchSharp.torch;

namespace GammonX.Mars.Training.Validation;

/// <summary>
/// Contains aggregate constraint-validation metrics for a collection of prediction rows.
/// </summary>
/// <param name="RowCount">Gets the total number of prediction rows processed.</param>
/// <param name="InvalidRowCount">Gets the number of rows with at least one invalid constraint or non-finite value.</param>
/// <param name="RuleViolationCount">Gets the total number of violated rule categories across all rows.</param>
/// <param name="RangeViolationRows">Gets the number of rows with a probability outside the inclusive range [0, 1].</param>
/// <param name="WinGammonHierarchyViolationRows">Gets the number of rows where win-gammon probability exceeds win probability.</param>
/// <param name="WinBackgammonHierarchyViolationRows">Gets the number of rows where win-backgammon probability exceeds win-gammon probability.</param>
/// <param name="LoseGammonComplementViolationRows">Gets the number of rows where lose-gammon probability exceeds the probability of losing.</param>
/// <param name="LoseBackgammonHierarchyViolationRows">Gets the number of rows where lose-backgammon probability exceeds lose-gammon probability.</param>
/// <param name="TotalSeverity">Gets the sum of all constraint violation severities.</param>
/// <param name="MaxSeverity">Gets the largest individual constraint violation severity.</param>
public sealed record ConstraintMetricsResult(
    long RowCount,
    long InvalidRowCount,
    long RuleViolationCount,
    long RangeViolationRows,
    long WinGammonHierarchyViolationRows,
    long WinBackgammonHierarchyViolationRows,
    long LoseGammonComplementViolationRows,
    long LoseBackgammonHierarchyViolationRows,
    double TotalSeverity,
    double MaxSeverity)
{
    /// <summary>
    /// Gets the fraction of processed rows that were invalid, or zero when no rows were processed.
    /// </summary>
    public double InvalidRate => RowCount == 0 ? 0d : (double)InvalidRowCount / RowCount;

    /// <summary>
    /// Gets the average total violation severity per processed row, or zero when no rows were processed.
    /// </summary>
    public double AverageSeverity => RowCount == 0 ? 0d : TotalSeverity / RowCount;
}

/// <summary>
/// Accumulates game-outcome constraint metrics from individual rows or prediction tensors.
/// </summary>
/// <remarks>
/// Tensor inputs must have shape <c>[batch, 5]</c>, where the five heads are ordered as
/// win, win gammon, win backgammon, lose gammon, and lose backgammon. The accumulator
/// is mutable and is intended to be completed after all batches have been added.
/// </remarks>
internal sealed class ConstraintMetricsAccumulator
{
    private readonly double _tolerance;
    private long _rowCount;
    private long _invalidRowCount;
    private long _ruleViolationCount;
    private long _rangeViolationRows;
    private long _winGammonHierarchyViolationRows;
    private long _winBackgammonHierarchyViolationRows;
    private long _loseGammonComplementViolationRows;
    private long _loseBackgammonHierarchyViolationRows;
    private double _totalSeverity;
    private double _maxSeverity;

    /// <summary>
    /// Initializes an accumulator using the specified violation tolerance.
    /// </summary>
    /// <param name="tolerance">The minimum severity counted as a rule violation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the tolerance is negative or non-finite.</exception>
    public ConstraintMetricsAccumulator(double tolerance = GameOutcomeConstraintValidator.DefaultTolerance)
    {
        if (!double.IsFinite(tolerance) || tolerance < 0d)
            throw new ArgumentOutOfRangeException(nameof(tolerance), tolerance, "The tolerance must be finite and non-negative.");

        _tolerance = tolerance;
    }

    /// <summary>
    /// Adds every row in a CPU or device tensor to the aggregate metrics.
    /// </summary>
    /// <param name="predictions">A tensor with shape <c>[batch, 5]</c> containing model predictions.</param>
    /// <exception cref="ArgumentException">Thrown when the tensor does not have the required shape.</exception>
    public void Add(Tensor predictions)
    {
        ArgumentNullException.ThrowIfNull(predictions);
        if (predictions.shape.Length != 2 || predictions.shape[1] != GameOutcomeConstraintValidator.FullHeadCount)
        {
            throw new ArgumentException($"Expected prediction shape [batch, {GameOutcomeConstraintValidator.FullHeadCount}], but received [{string.Join(", ", predictions.shape)}].", nameof(predictions));
        }

        // Copy detached values to the CPU once, then validate each row independently.
        using var hostPredictions = predictions.detach().to(CPU);
        var values = hostPredictions.data<float>().ToArray();
        var batchSize = checked((int)predictions.shape[0]);
        for (var row = 0; row < batchSize; row++)
        {
            var rowValues = new float[GameOutcomeConstraintValidator.FullHeadCount];
            Array.Copy(values, row * rowValues.Length, rowValues, 0, rowValues.Length);
            AddRow(rowValues);
        }
    }

    /// <summary>
    /// Adds one five-head prediction row to the aggregate metrics.
    /// </summary>
    /// <param name="predictions">The predictions in the order expected by <see cref="GameOutcomeConstraintValidator"/>.</param>
    public void AddRow(IReadOnlyList<float> predictions)
    {
        var report = GameOutcomeConstraintValidator.Validate(predictions, _tolerance);
        _rowCount++;

        if (!report.IsValid)
        {
            _invalidRowCount++;
        }

        // We keep both the total number of violated categories and per-category row counts.
        _ruleViolationCount += report.ViolationCount;
        AddRuleViolation(report.RangeViolation, ref _rangeViolationRows);
        AddRuleViolation(report.WinGammonHierarchyViolation, ref _winGammonHierarchyViolationRows);
        AddRuleViolation(report.WinBackgammonHierarchyViolation, ref _winBackgammonHierarchyViolationRows);
        AddRuleViolation(report.LoseGammonComplementViolation, ref _loseGammonComplementViolationRows);
        AddRuleViolation(report.LoseBackgammonHierarchyViolation, ref _loseBackgammonHierarchyViolationRows);
        _totalSeverity += report.TotalViolation;
        _maxSeverity = Math.Max(_maxSeverity, report.MaxViolation);
    }

    /// <summary>
    /// Merges metrics produced by another accumulator or processing partition.
    /// </summary>
    /// <param name="metrics">The previously completed metrics to merge.</param>
    public void Merge(ConstraintMetricsResult metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        _rowCount += metrics.RowCount;
        _invalidRowCount += metrics.InvalidRowCount;
        _ruleViolationCount += metrics.RuleViolationCount;
        _rangeViolationRows += metrics.RangeViolationRows;
        _winGammonHierarchyViolationRows += metrics.WinGammonHierarchyViolationRows;
        _winBackgammonHierarchyViolationRows += metrics.WinBackgammonHierarchyViolationRows;
        _loseGammonComplementViolationRows += metrics.LoseGammonComplementViolationRows;
        _loseBackgammonHierarchyViolationRows += metrics.LoseBackgammonHierarchyViolationRows;
        _totalSeverity += metrics.TotalSeverity;
        _maxSeverity = Math.Max(_maxSeverity, metrics.MaxSeverity);
    }

    /// <summary>
    /// Creates a snapshot of all metrics accumulated so far.
    /// </summary>
    /// <returns>The aggregate metrics result.</returns>
    public ConstraintMetricsResult Complete()
        => new(
            _rowCount,
            _invalidRowCount,
            _ruleViolationCount,
            _rangeViolationRows,
            _winGammonHierarchyViolationRows,
            _winBackgammonHierarchyViolationRows,
            _loseGammonComplementViolationRows,
            _loseBackgammonHierarchyViolationRows,
            _totalSeverity,
            _maxSeverity);

    /// <summary>
    /// Increments a per-rule row count when severity exceeds the configured tolerance.
    /// </summary>
    private void AddRuleViolation(double severity, ref long rowCount)
    {
        if (severity > _tolerance)
            rowCount++;
    }
}
