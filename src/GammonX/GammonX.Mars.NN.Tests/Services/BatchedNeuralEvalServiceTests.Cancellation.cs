using System.Diagnostics.Metrics;

using GammonX.Engine.Diagnostics;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Services;

public partial class BatchedNeuralEvalServiceTests
{
    [Fact]
    public async Task AlreadyCancelledCallsDoNotExtractFeaturesOrQueueWork()
    {
        using var service = CreateService(_ => throw new InvalidOperationException("Inference must not run."));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.PredictBatchAsync([[1]], cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.PredictAsync(default, null!, true, cancellation.Token));
    }

    [Fact]
    public async Task QueuedCancellationSkipsInferenceAndPreservesOtherCallers()
    {
        long queuedRows = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == EvaluationMetrics.MeterName && instrument.Name == EvaluationMetrics.QueuedRowsName)
                meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, count, _, _) => Interlocked.Add(ref queuedRows, count));
        listener.Start();

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var inputs = new List<float>();
        using var service = CreateService(input =>
        {
            var values = input.data<float>().ToArray();
            inputs.AddRange(values);
            if (values.Contains(1f))
            {
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Inference was not released.");
            }
            return input.clone();
        });
        await service.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var active = service.PredictBatchAsync([[1], [2], [3], [4]], TestContext.Current.CancellationToken);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var cancelled = service.PredictBatchAsync([[5]], cancellation.Token);
            var other = service.PredictBatchAsync([[9]], TestContext.Current.CancellationToken);
            cancellation.Cancel();
            Assert.False(cancelled.IsCompleted); // The worker must acknowledge ownership release.
            release.Set();

            await active.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
            Assert.Equal(cancellation.Token, error.CancellationToken);
            AssertRow((await other)[0], 9, 1);
            AssertRow((await service.PredictBatchAsync([[10]], TestContext.Current.CancellationToken))[0], 10, 1);
            Assert.DoesNotContain(5f, inputs);
            Assert.Equal(0, Interlocked.Read(ref queuedRows));
        }
        finally
        {
            release.Set();
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationDuringForwardWaitsForCompletionBeforeBuffersCanBeReused(bool failForward)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var calls = 0;
        using var service = CreateService(input =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Inference was not released.");
                Assert.Equal(7f, input.data<float>()[0]);
                if (failForward) throw new InvalidDataException("Expected inference failure.");
            }
            return input.clone();
        });
        await service.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            float[][] features = [[7]];
            var active = service.PredictBatchAsync(features, cancellation.Token);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            cancellation.Cancel();
            Assert.False(active.IsCompleted);
            release.Set();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => active);

            features[0][0] = 11;
            AssertRow((await service.PredictBatchAsync(features, TestContext.Current.CancellationToken))[0], 11, 1);
        }
        finally
        {
            release.Set();
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task CancellationInASharedForwardPreservesTheOtherRequestsRowOffsets()
    {
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sharedEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseFirst = new ManualResetEventSlim();
        using var releaseShared = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var calls = 0;
        using var service = CreateService(input =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                firstEntered.TrySetResult();
                if (!releaseFirst.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("First forward was not released.");
            }
            else
            {
                Assert.Equal(new[] { 5f, 6f, 9f }, input.data<float>().ToArray());
                sharedEntered.TrySetResult();
                if (!releaseShared.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Shared forward was not released.");
            }
            return input.clone();
        });
        await service.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var first = service.PredictBatchAsync([[1], [2], [3], [4]], TestContext.Current.CancellationToken);
            await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var cancelled = service.PredictBatchAsync([[5], [6]], cancellation.Token);
            var other = service.PredictBatchAsync([[9]], TestContext.Current.CancellationToken);
            releaseFirst.Set();
            await sharedEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            cancellation.Cancel();
            releaseShared.Set();

            await first;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
            AssertRow((await other)[0], 9, 1);
        }
        finally
        {
            releaseFirst.Set();
            releaseShared.Set();
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task CancellationWhileWaitingForCapacityDoesNotWaitForTheActiveForward()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var queuedCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var service = CreateService(input =>
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Inference was not released.");
            return input.clone();
        });
        await service.StartAsync(TestContext.Current.CancellationToken);
        var queued = new List<Task<float[][]>>();
        Task<float[][]>? active = null;
        try
        {
            active = service.PredictBatchAsync([[1], [2], [3], [4]], TestContext.Current.CancellationToken);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            for (var index = 0; index < service.MaxBatchSize * 4; index++)
                queued.Add(service.PredictBatchAsync([[5]], queuedCancellation.Token));
            var waiting = service.PredictBatchAsync([[99]], cancellation.Token);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.False(active.IsCompleted);
        }
        finally
        {
            queuedCancellation.Cancel();
            release.Set();
            if (active != null) await active;
            foreach (var pending in queued)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            await service.StopAsync(CancellationToken.None);
        }
    }
}
