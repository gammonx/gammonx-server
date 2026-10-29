using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Nets;
using GammonX.Mars.NN.Services;
using GammonX.Mars.NN.Tests.Performance;

using GammonX.Models.Enums;

using Moq;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Services;

[Collection(nameof(PerformanceTestCollection))]
public class BatchedNeuralEvalServiceTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(5, false)]
    public async Task MapsMixedSinglesAndGroupsWithoutSplittingOrReorderingRows(int width, bool flatOutput)
    {
        var seen = new List<int>();
        using var service = CreateService(input =>
        {
            var markers = input.data<float>().ToArray();
            seen.Add(markers.Length);
            var values = markers.SelectMany(marker => Enumerable.Range(0, width).Select(column => marker + column)).ToArray();
            return flatOutput ? tensor(values) : tensor(values, [markers.Length, width]);
        });

        await service.StartAsync(TestContext.Current.CancellationToken);

        try
        {
            INeuralEvalService batch = service;
            var board = BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard();

            var tasks = Enumerable.Range(0, 20).Select(async index =>
            {
                var marker = index * 10;
                if (index % 2 == 0)
                {
                    var result = await service.PredictAsync(new NormalizedEvalResultModel { BlotCount = marker }, board, true);
                    AssertRow(result, marker, width);
                }
                else
                {
                    var result = await batch.PredictBatchAsync([[marker], [marker + 1], [marker + 2]]);
                    Assert.Equal(3, result.Length);
                    for (var row = 0; row < result.Length; row++)
                    {
                        AssertRow(result[row], marker + row, width);
                    }
                }
            });
            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.All(seen, count => Assert.InRange(count, 1, 4));
            Assert.Equal(40, seen.Sum());
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task RejectsInvalidGroupsBeforeQueueing()
    {
        using var service = CreateService(input => input.clone());
        await service.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            INeuralEvalService batch = service;
            await Assert.ThrowsAsync<ArgumentNullException>(() => batch.PredictBatchAsync(null!));
            await Assert.ThrowsAsync<ArgumentException>(() => batch.PredictBatchAsync([]));
            await Assert.ThrowsAsync<ArgumentException>(() => batch.PredictBatchAsync([[1], [2], [3], [4], [5]]));
            await Assert.ThrowsAsync<ArgumentException>(() => batch.PredictBatchAsync([[1, 2]]));
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task RejectsAnExtractorWidthMismatchRatherThanPaddingWithZeros()
    {
        var net = new Mock<INetModel>();
        var extractor = new Mock<IFeatureVectorExtractor>();
        extractor.SetupGet(value => value.FeatureCount).Returns(2);
        extractor.Setup(value => value.Extract(It.IsAny<NormalizedEvalResultModel>(), It.IsAny<IBoardModel>(), It.IsAny<bool>()))
            .Returns([1f]);
        using var service = new BatchedNeuralEvalService(net.Object, extractor.Object, 4, CPU);
        await service.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var board = BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard();
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                service.PredictAsync(new NormalizedEvalResultModel(), board, true));
            net.Verify(model => model.Forward(It.IsAny<Tensor>()), Times.Never);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task ForwardFailureFaultsTheGroupAndWorkerRemainsUsable()
    {
        var calls = 0;
        using var service = CreateService(input =>
        {
            if (Interlocked.Increment(ref calls) == 1)
                throw new InvalidDataException("Model failed.");
            return input.clone();
        });
        await service.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            INeuralEvalService batch = service;
            var error = await Assert.ThrowsAsync<InvalidDataException>(() => batch.PredictBatchAsync([[1], [2]]));
            Assert.Equal("Model failed.", error.Message);
            var results = await batch.PredictBatchAsync([[3], [4]]);
            AssertRow(results[0], 3, 1);
            AssertRow(results[1], 4, 1);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task SinglePredictionPreservesTheOriginalForwardException()
    {
        var failure = new InvalidDataException("Expected model failure.");
        using var service = CreateService(_ => throw failure);
        await service.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var board = BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard();
            var actual = await Assert.ThrowsAsync<InvalidDataException>(() =>
                service.PredictAsync(new NormalizedEvalResultModel(), board, true));
            Assert.Same(failure, actual);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task InvalidOutputShapeFaultsAllRowsInsteadOfReturningPartialSuccess()
    {
        using var service = CreateService(_ => tensor(new float[6], [2, 3]));
        await service.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await Assert.ThrowsAsync<InvalidDataException>(() => service.PredictBatchAsync([[1], [2]]));
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task AQueuedSingleIsServedBeforeTheNextChunkOfALargeCaller()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var calls = new List<float[]>();
        using var service = CreateService(input =>
        {
            var markers = input.data<float>().ToArray();
            calls.Add(markers);
            if (markers.Contains(1f))
            {
                entered.TrySetResult();
                if (!release.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException("Test did not release the first forward pass.");
            }
            return input.clone();
        });
        await service.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            INeuralEvalService batch = service;
            var board = BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard();
            var bulk = SendChunks();
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var single = service.PredictAsync(new NormalizedEvalResultModel { BlotCount = 99 }, board, true);
            release.Set();
            AssertRow(await single.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken), 99, 1);
            await bulk.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.True(calls.FindIndex(rows => rows.Contains(99f)) < calls.FindIndex(rows => rows.Contains(5f)));

            async Task SendChunks()
            {
                await batch.PredictBatchAsync([[1], [2], [3], [4]]);
                await batch.PredictBatchAsync([[5], [6], [7], [8]]);
            }
        }
        finally
        {
            release.Set();
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task StopCompletesQueuedGroupsAndRejectsNewWork()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var service = CreateService(input =>
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Test did not release inference.");
            return input.clone();
        });
        await service.StartAsync(TestContext.Current.CancellationToken);
        INeuralEvalService batch = service;
        try
        {
            var active = batch.PredictBatchAsync([[1], [2], [3], [4]]);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var queued = batch.PredictBatchAsync([[5]]);
            var stopping = service.StopAsync(CancellationToken.None);
            release.Set();
            await active.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await stopping.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
            await Assert.ThrowsAsync<InvalidOperationException>(() => batch.PredictBatchAsync([[6]]));
        }
        finally
        {
            release.Set();
            await service.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    public async Task CancellingWorkerCompletesQueuedAndBackpressuredCallers()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var service = CreateService(input =>
        {
            entered.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(10)))
                throw new TimeoutException("Test did not release inference.");
            return input.clone();
        });
        await service.StartAsync(cancellation.Token);
        try
        {
            INeuralEvalService batch = service;
            var board = BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard();
            var active = batch.PredictBatchAsync([[1], [2], [3], [4]]);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var singles = Enumerable.Range(0, 40)
                .Select(_ => service.PredictAsync(new NormalizedEvalResultModel(), board, true)).ToArray();
            var groups = Enumerable.Range(0, 40).Select(_ => batch.PredictBatchAsync([[5]])).ToArray();
            await cancellation.CancelAsync();
            release.Set();
            await active.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            foreach (var single in singles)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => single);
            foreach (var group in groups)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => group);
        }
        finally
        {
            release.Set();
            await service.StopAsync(CancellationToken.None);
        }
    }

    private static BatchedNeuralEvalService CreateService(Func<Tensor, Tensor> forward)
    {
        var net = new Mock<INetModel>();
        net.Setup(model => model.Forward(It.IsAny<Tensor>())).Returns(forward);
        var extractor = new Mock<IFeatureVectorExtractor>();
        extractor.SetupGet(value => value.FeatureCount).Returns(1);
        extractor.Setup(value => value.Extract(It.IsAny<NormalizedEvalResultModel>(), It.IsAny<IBoardModel>(), It.IsAny<bool>()))
            .Returns((NormalizedEvalResultModel model, IBoardModel _, bool _) => new[] { (float)model.BlotCount });
        return new BatchedNeuralEvalService(net.Object, extractor.Object, 4, CPU);
    }

    private static void AssertRow(float[] result, float marker, int width)
    {
        Assert.Equal(5, result.Length);
        for (var column = 0; column < result.Length; column++)
            Assert.Equal(column < width ? marker + column : 0f, result[column]);
    }
}
