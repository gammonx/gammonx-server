using static TorchSharp.torch;
using static TorchSharp.torch.nn;

namespace GammonX.Mars.Training.Tests;

public sealed class EpochMetricsAccumulatorTests
{
    [Fact]
    public void Complete_WeightsPartialBatchesByRowCount()
    {
        using var firstBatch = tensor([0.2f, 0.4f, 0.6f, 0.8f, 1.0f], device: CPU);
        using var finalBatch = tensor([0.3f, 0.6f, 0.9f, 1.2f, 1.5f], device: CPU);
        using var accumulator = new EpochMetricsAccumulator(outputCount: 5);

        accumulator.Add(firstBatch, rowCount: 2);
        accumulator.Add(finalBatch, rowCount: 1);

        var result = accumulator.Complete();

        Assert.Equal(3, result.RowCount);
        Assert.Equal(1f / 6f, result.PerOutputLosses[0], precision: 5);
        Assert.Equal(1f / 3f, result.PerOutputLosses[1], precision: 5);
        Assert.Equal(0.5f, result.PerOutputLosses[2], precision: 5);
        Assert.Equal(2f / 3f, result.PerOutputLosses[3], precision: 5);
        Assert.Equal(5f / 6f, result.PerOutputLosses[4], precision: 5);
        Assert.Equal(0.5f, result.Loss, precision: 5);
    }

    [Fact]
    public void Complete_ReportsSingleOutputMetric()
    {
        using var batch = tensor([0.75f], device: CPU);
        using var accumulator = new EpochMetricsAccumulator(outputCount: 1);

        accumulator.Add(batch, rowCount: 3);

        var result = accumulator.Complete();

        Assert.Single(result.PerOutputLosses);
        Assert.Equal(0.25f, result.PerOutputLosses[0], precision: 5);
        Assert.Equal(0.25f, result.Loss, precision: 5);
    }

    [Fact]
    public void Complete_MatchesUnreducedBcePerOutputMeans()
    {
        using var predictions = tensor(
            [
                0.8f, 0.2f, 0.7f, 0.1f, 0.6f,
                0.4f, 0.9f, 0.3f, 0.5f, 0.2f
            ],
            [2, 5],
            device: CPU);
        using var labels = tensor(
            [
                1f, 0f, 1f, 0f, 0f,
                0f, 1f, 0f, 1f, 1f
            ],
            [2, 5],
            device: CPU);
        using var bce = BCELoss(reduction: Reduction.None);
        using var elementLosses = bce.forward(predictions, labels);
        Assert.Equal(2, elementLosses.shape.Length);
        Assert.Equal(2, elementLosses.shape[0]);
        Assert.Equal(5, elementLosses.shape[1]);
        using var headLosses = elementLosses.sum(dim: 0);
        Assert.Single(headLosses.shape);
        Assert.Equal(5, headLosses.shape[0]);
        using var accumulator = new EpochMetricsAccumulator(outputCount: 5);

        accumulator.Add(headLosses, rowCount: 2);

        var result = accumulator.Complete();
        var expected = headLosses.data<float>().ToArray().Select(value => value / 2f).ToArray();

        Assert.Equal(expected, result.PerOutputLosses);
        Assert.Equal(expected.Average(), result.Loss);
    }
}