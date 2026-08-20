using GammonX.Mars.Training.Validation;

using static TorchSharp.torch;

namespace GammonX.Mars.Training.Tests;

public sealed class ConstraintMetricsAccumulatorTests
{
    [Fact]
    public void AddRowCountsInvalidRowsAndRuleSeverity()
    {
        var accumulator = new ConstraintMetricsAccumulator();
        accumulator.AddRow([0.7f, 0.2f, 0.05f, 0.1f, 0.02f]);
        accumulator.AddRow([0.4f, 0.6f, 0.8f, 0.9f, 0.95f]);

        var result = accumulator.Complete();

                Assert.Equal(2, result.RowCount);
                Assert.Equal(1, result.InvalidRowCount);
                Assert.Equal(4, result.RuleViolationCount);
                Assert.Equal(1, result.WinGammonHierarchyViolationRows);
                Assert.Equal(1, result.WinBackgammonHierarchyViolationRows);
                Assert.Equal(1, result.LoseGammonComplementViolationRows);
                Assert.Equal(1, result.LoseBackgammonHierarchyViolationRows);
                Assert.Equal(0.5, result.InvalidRate, 5);
                Assert.Equal(0.3, result.MaxSeverity, 5);
        }

        [Fact]
        public void AddTensorAuditsEveryBatchRow()
        {
                using var predictions = tensor(
                        [
                                0.7f, 0.2f, 0.05f, 0.1f, 0.02f,
                                0.5f, 0.7f, 0.2f, 0.6f, 0.1f
                        ],
                        [2, 5],
                        device: CPU);
                var accumulator = new ConstraintMetricsAccumulator();

                accumulator.Add(predictions);

                var result = accumulator.Complete();

                Assert.Equal(2, result.RowCount);
                Assert.Equal(1, result.InvalidRowCount);
                Assert.Equal(0.5, result.InvalidRate, precision: 5);
        }
}
