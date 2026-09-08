using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Nets;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Nets;

public sealed class DefaultNetTests
{
    [Fact]
    public void MonotonicOutputModeProducesAValidFiveHeadHierarchy()
    {
        using var model = new DefaultNet(CPU, GameOutcomeOutputMode.MonotonicCumulative, NetArchitecture.A);
        using var input = tensor(new float[3 * 216], [3, 216], device: CPU);
        using var noGradScope = no_grad();
        using var output = model.Forward(input);

        Assert.Equal([3L, 5L], output.shape);

        var values = output.data<float>().ToArray();
        for (var row = 0; row < 3; row++)
        {
            var predictions = values[(row * GameOutcomeConstraintValidator.FullHeadCount)..((row + 1) * GameOutcomeConstraintValidator.FullHeadCount)];
            var report = GameOutcomeConstraintValidator.Validate(predictions);
            Assert.True(report.IsValid, report.ToString());
        }
    }
}
