using GammonX.Mars.NN.Nets;
using GammonX.Mars.NN.Services;
using GammonX.Mars.NN.Tests.Performance;

using Moq;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Services;

[Collection(nameof(PerformanceTestCollection))]
public class NeuralEvalServiceCancellationTests
{
    [Fact]
    public async Task CancellationInterruptsGateWaitAndFinishesActiveForwardWithoutBlockingOtherCallers()
    {
        using var activeCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var waitingCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var net = new Mock<INetModel>();
        net.SetupGet(model => model.Metadata).Returns(new NetModelMetadata(2, GammonX.Models.Enums.GameModus.Fevga,
            GameOutcomeOutputMode.LegacyIndependentSigmoid, NetArchitecture.A));
        var calls = 0;
        net.Setup(value => value.Forward(It.IsAny<Tensor>())).Returns((Tensor input) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Inference was not released.");
            }
            return input.clone();
        });
        var extractor = new Mock<IFeatureVectorExtractor>();
        extractor.SetupGet(value => value.FeatureCount).Returns(1);
        var first = new NeuralEvalService(net.Object, extractor.Object, CPU);
        var second = new NeuralEvalService(net.Object, extractor.Object, CPU);
        var active = Task.Run(() => first.PredictBatchAsync([[1]], activeCancellation.Token), TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var waiting = second.PredictBatchAsync([[2]], waitingCancellation.Token);
            waitingCancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.Equal(1, calls);
            activeCancellation.Cancel();
            Assert.False(active.IsCompleted);
        }
        finally
        {
            release.Set();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => active);
        }

        var result = await second.PredictBatchAsync([[3]], TestContext.Current.CancellationToken);
        Assert.Equal(3f, result[0][0]);
        Assert.Equal(2, calls);
    }
}
