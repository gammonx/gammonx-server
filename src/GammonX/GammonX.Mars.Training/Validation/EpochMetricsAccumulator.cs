using GammonX.Mars.Training.Validation;

using static TorchSharp.torch;

namespace GammonX.Mars.Training;

/// <summary>
/// Contains the loss and optional prediction-constraint metrics collected for one epoch.
/// </summary>
/// <param name="Loss">Gets the average loss across all outputs and rows.</param>
/// <param name="PerOutputLosses">Gets the average loss for each output.</param>
/// <param name="RowCount">Gets the total number of rows included in the metrics.</param>
/// <param name="ConstraintMetrics">Gets the optional aggregate constraint metrics.</param>
internal sealed record EpochMetricsResult(
    float Loss,
    float[] PerOutputLosses,
    long RowCount,
    ConstraintMetricsResult? ConstraintMetrics = null);

/// <summary>
/// Keeps per-output BCE sums on the training device so partial batches are row-weighted
/// and the final metrics require only one device-to-host read.
/// </summary>
/// <remarks>
/// Add each batchs summed per-output losses together with its row count. For example,
/// two batches containing 32 and 16 rows are averaged using 48 total rows rather than
/// averaging the two batch means equally. When enabled, prediction constraint metrics
/// are accumulated alongside the loss metrics.
/// </remarks>
internal sealed class EpochMetricsAccumulator : IDisposable
{
    private readonly int _outputCount;
    private readonly ConstraintMetricsAccumulator? _constraintMetrics;
    private Tensor? _lossSums;
    private long _rowCount;

    /// <summary>
    /// Initializes an epoch metrics accumulator.
    /// </summary>
    /// <param name="outputCount">The number of output losses represented by each batch loss tensor.</param>
    /// <param name="collectConstraintMetrics">Whether to collect game-outcome constraint metrics for predictions.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="outputCount"/> is not positive.</exception>
    public EpochMetricsAccumulator(int outputCount, bool collectConstraintMetrics = false)
    {
        if (outputCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(outputCount), outputCount, "The output count must be greater than zero.");

        _outputCount = outputCount;
        _constraintMetrics = collectConstraintMetrics
            ? new ConstraintMetricsAccumulator()
            : null;
    }

    /// <summary>
    /// Adds one batch of loss sums and, optionally, model predictions to the epoch totals.
    /// </summary>
    /// <param name="batchLossSums">A one-dimensional tensor containing one summed loss per output.</param>
    /// <param name="rowCount">The number of rows represented by the batch.</param>
    /// <param name="predictions">Optional model predictions used for constraint metrics.</param>
    /// <exception cref="ArgumentException">Thrown when the loss tensor shape is not <c>[outputCount]</c>.</exception>
    /// <exception cref="ArgumentNullException">Thrown when required input is null or constraint metrics are enabled without predictions.</exception>
    public void Add(Tensor batchLossSums, long rowCount, Tensor? predictions = null)
    {
        ArgumentNullException.ThrowIfNull(batchLossSums);

        if (rowCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(rowCount), rowCount, "The row count must be greater than zero.");

        if (batchLossSums.shape.Length != 1 || batchLossSums.shape[0] != _outputCount)
        {
            throw new ArgumentException($"Expected one loss sum per output ({_outputCount}), but received shape [{string.Join(", ", batchLossSums.shape)}].", nameof(batchLossSums));
        }

        // We keep accumulation on the tensors device and defer the host transfer until completion.
        if (_lossSums is null)
        {
            _lossSums = batchLossSums.detach().clone();
        }
        else
        {
            _lossSums.add_(batchLossSums);
        }

        if (_constraintMetrics != null)
        {
            if (predictions is null)
                throw new ArgumentNullException(nameof(predictions), "Constraint metrics require model predictions.");

            _constraintMetrics.Add(predictions);
        }

        _rowCount = checked(_rowCount + rowCount);
    }

    /// <summary>
    /// Completes the aggregation and returns row-weighted epoch metrics.
    /// </summary>
    /// <returns>The average overall loss, per-output losses, row count, and optional constraint metrics.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no batches have been added.</exception>
    public EpochMetricsResult Complete()
    {
        if (_lossSums is null || _rowCount == 0)
            throw new InvalidOperationException("Cannot complete metrics without any batches.");

        // We perform the single device-to-host read needed to calculate the final averages.
        using var hostLossSums = _lossSums.to(CPU);
        var lossSums = hostLossSums.data<float>().ToArray();
        var perOutputLosses = lossSums.Select(sum => sum / _rowCount).ToArray();

        return new EpochMetricsResult(
            perOutputLosses.Average(),
            perOutputLosses,
            _rowCount,
            _constraintMetrics?.Complete());
    }

    /// <summary>
    /// Releases the accumulated loss tensor and makes the accumulator reusable for disposal only.
    /// </summary>
    public void Dispose()
    {
        _lossSums?.Dispose();
        _lossSums = null;
    }
}