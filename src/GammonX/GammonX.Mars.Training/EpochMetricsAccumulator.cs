using TorchSharp;

using static TorchSharp.torch;

namespace GammonX.Mars.Training;

internal sealed record EpochMetricsResult(
    float Loss,
    float[] PerOutputLosses,
    long RowCount);

/// <summary>
/// Keeps per-output BCE sums on the training device so partial batches are row-weighted
/// and the final metrics require only one device-to-host read.
/// </summary>
internal sealed class EpochMetricsAccumulator : IDisposable
{
    private readonly int _outputCount;
    private Tensor? _lossSums;
    private long _rowCount;

    public EpochMetricsAccumulator(int outputCount)
    {
        if (outputCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(outputCount), outputCount, "The output count must be greater than zero.");

        _outputCount = outputCount;
    }

    public void Add(Tensor batchLossSums, long rowCount)
    {
        ArgumentNullException.ThrowIfNull(batchLossSums);

        if (rowCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(rowCount), rowCount, "The row count must be greater than zero.");

        if (batchLossSums.shape.Length != 1 || batchLossSums.shape[0] != _outputCount)
        {
            throw new ArgumentException(
                $"Expected one loss sum per output ({_outputCount}), but received shape [{string.Join(", ", batchLossSums.shape)}].",
                nameof(batchLossSums));
        }

        if (_lossSums is null)
            _lossSums = batchLossSums.detach().clone();
        else
            _lossSums.add_(batchLossSums);

        _rowCount = checked(_rowCount + rowCount);
    }

    public EpochMetricsResult Complete()
    {
        if (_lossSums is null || _rowCount == 0)
            throw new InvalidOperationException("Cannot complete metrics without any batches.");

        using var hostLossSums = _lossSums.to(CPU);
        var lossSums = hostLossSums.data<float>().ToArray();
        var perOutputLosses = lossSums
            .Select(sum => sum / _rowCount)
            .ToArray();

        return new EpochMetricsResult(perOutputLosses.Average(), perOutputLosses, _rowCount);
    }

    public void Dispose()
    {
        _lossSums?.Dispose();
        _lossSums = null;
    }
}