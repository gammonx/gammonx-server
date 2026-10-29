using GammonX.Engine.Diagnostics;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

namespace GammonX.Mars.NN.Tests.Performance;

[Collection(nameof(PerformanceTestCollection))]
public class EvaluationMetricsTests
{
    [Fact]
    public void UniqueGenerationClonesOnlyFirstTerminalPositions()
    {
        var service = BoardServiceFactory.Create(GameModus.Backgammon);
        var board = service.CreateBoard();
        for (var turn = 0; turn < 100; turn++)
            service.AddRollEventToHistory(board, turn % 2 == 0, [2, 3]);
        var original = board.DeepClone();
        var history = board.History.Events.ToArray();
        using var collector = new EvaluationMetricsCollector();

        var sequences = service.GetUniqueLegalMoveSequences(board, false, 2, 2, 2, 2);

        var profile = collector.Snapshot();
        Assert.NotEmpty(sequences);
        Assert.Equal(sequences.Length, profile.UniqueSequences);
        Assert.Equal(profile.UniqueSequences, profile.GetStage(EvaluationMetrics.GenerationClone).Calls);
        Assert.True(profile.RawSequences > profile.UniqueSequences);
        Assert.True(board.Equals(original));
        Assert.Equal(history, board.History.Events.ToArray());
    }

    [Fact]
    public void DoublesTraversalReusesStatesOnlyWithinUniqueGeneration()
    {
        var service = BoardServiceFactory.Create(GameModus.Backgammon);
        var board = service.CreateBoard();

        using (var collector = new EvaluationMetricsCollector())
        {
            Assert.NotEmpty(service.GetUniqueLegalMoveSequences(board, false, 2, 2, 2, 2));
            var profile = collector.Snapshot();
            Assert.True(profile.ReusedStates > 0);
            Assert.True(profile.GetStage(EvaluationMetrics.TraversalSnapshot).Calls > 0);
        }
        using (var collector = new EvaluationMetricsCollector())
        {
            Assert.NotEmpty(service.GetUniqueLegalMoveSequences(board, false, 3, 2));
            Assert.NotEmpty(service.GetLegalMoveSequences(board, false, 2, 2, 2, 2));
            Assert.Equal(0, collector.Snapshot().ReusedStates);
            Assert.DoesNotContain(collector.Snapshot().Stages, stage => stage.Stage == EvaluationMetrics.TraversalSnapshot);
        }
    }

    [Fact]
    public void DisabledScopeDoesNotPublishWhenAListenerIsAddedLater()
    {
        var scope = EvaluationMetrics.Measure("Disabled");
        using var collector = new EvaluationMetricsCollector();

        scope.Dispose();

        Assert.Empty(collector.Snapshot().Stages);
    }

    [Fact]
    public void NestedSynchronousStagesReportInclusiveTimeAndThreadAllocations()
    {
        using var collector = new EvaluationMetricsCollector();

        using (EvaluationMetrics.Measure("Outer"))
        {
            using (EvaluationMetrics.Measure("Inner"))
            {
                GC.KeepAlive(new byte[4096]);
            }
        }

        var profile = collector.Snapshot();
        var outer = profile.GetStage("Outer");
        var inner = profile.GetStage("Inner");
        Assert.Equal(1, inner.Calls);
        Assert.Equal(1, outer.Calls);
        Assert.Equal(1, inner.AllocationSamples);
        Assert.Equal(1, outer.AllocationSamples);
        Assert.True(inner.AllocatedBytes >= 4096);
        Assert.True(outer.AllocatedBytes >= inner.AllocatedBytes);
        Assert.True(outer.TotalMs >= inner.TotalMs);
    }

    [Fact]
    public void DisposingAStageOnFailurePreservesTheExceptionAndRecordsTheStage()
    {
        using var collector = new EvaluationMetricsCollector();

        void Fail()
        {
            using var measurement = EvaluationMetrics.Measure("Failure");
            throw new InvalidOperationException("Expected test failure.");
        }

        Assert.Throws<InvalidOperationException>(Fail);

        Assert.Equal(1, collector.Snapshot().GetStage("Failure").Calls);
    }

    [Fact]
    public async Task CollectsWorkerStagesQueueWaitsAndBatchDistribution()
    {
        using var collector = new EvaluationMetricsCollector();
        var queued = EvaluationMetrics.GetQueueTimestamp();
        EvaluationMetrics.RecordSequences(5, 2);
        EvaluationMetrics.RecordReusedState();

        await Task.Run(() =>
        {
            var batchStarted = EvaluationMetrics.GetQueueTimestamp();
            EvaluationMetrics.RecordQueueWait(queued, batchStarted);
            EvaluationMetrics.RecordQueueWait(queued, batchStarted);
            EvaluationMetrics.RecordBatchSize(2);
            using var measurement = EvaluationMetrics.Measure("Worker");
            GC.KeepAlive(new byte[2048]);
        }, TestContext.Current.CancellationToken);

        var profile = collector.Snapshot();
        Assert.Equal(5, profile.RawSequences);
        Assert.Equal(2, profile.UniqueSequences);
        Assert.Equal(1, profile.RawMeasurements);
        Assert.Equal(1, profile.UniqueMeasurements);
        Assert.Equal(1, profile.ReusedStates);
        var batch = Assert.Single(profile.BatchSizes);
        Assert.Equal(2, batch.Key);
        Assert.Equal(1, batch.Value);
        var wait = profile.GetStage(EvaluationMetrics.QueueWait);
        Assert.Equal(2, wait.Calls);
        Assert.True(wait.TotalMs >= 0);
        Assert.Null(wait.AllocatedBytes);
        Assert.True(profile.GetStage("Worker").AllocatedBytes >= 2048);
    }
}
