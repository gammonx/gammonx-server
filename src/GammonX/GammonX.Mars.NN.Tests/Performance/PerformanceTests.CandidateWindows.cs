using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;

using GammonX.Engine.Diagnostics;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Performance;

public partial class PerformanceTests
{
    [Fact]
    public async Task MeasureCandidateWindows()
    {
        var window = ReadTuningInt("GAMMONX_PERF_WINDOW", 1);
        Assert.Contains(window, new[] { 1, 2, 4 });
        var boardNumber = ReadTuningInt("GAMMONX_PERF_BOARD", 1);
        var mode = Environment.GetEnvironmentVariable("GAMMONX_PERF_WINDOW_MODE") ?? "Timing";
        Assert.Contains(mode, new[] { "Timing", "Diagnostic", "Mixed" });

        var json = boardNumber switch
        {
            1 => MockBoards.PerformanceBoard1,
            2 => MockBoards.PerformanceBoard2,
            3 => MockBoards.PerformanceBoard3,
            _ => throw new ArgumentOutOfRangeException(nameof(boardNumber))
        };

        var contract = JsonConvert.DeserializeObject<BoardModelContract>(json);
        Assert.NotNull(contract);

        var modelPath = Path.Combine("Data", "NeuralNets", "Backgammon", "training_net.dat");
        using var neural = Assert.IsType<BatchedNeuralEvalService>(BatchedNeuralEvalService.Load(GameModus.Backgammon, modelPath, CPU, 32));

        await neural.StartAsync(TestContext.Current.CancellationToken);
        
        try
        {
            WriteEnvironment(boardNumber == 2 ? [3, 2] : [2, 2, 2, 2]);
            output.WriteLine($"Window={window}; WindowMode={mode}; TorchThreads={get_num_threads()}; TorchInteropThreads={get_num_interop_threads()}; ReuseCpuInput={neural.ReuseCPUTensor}");
            output.WriteLine($"ModelSHA256={Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(modelPath, TestContext.Current.CancellationToken)))}");
            var counting = new CountingNeuralEvalService(neural);
            var service = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(FeatureEvalServiceFactory.Create(GameModus.Backgammon, counting));
            // Every fresh process validates against sequential scores, not just its selected move.
            var maximumScoreDifference = 0d;
            long before;
            long expectedRows;
            FinalEvalResultModels reference;
            FinalEvalResultModels warmup;
            using (var workerThreads = new WorkerThreadVerifier(ReadTuningInt("GAMMONX_PERF_THREADS", get_num_threads())))
            {
                var defaultBatchAcrossRolls = service.BatchAcrossRolls;
                var defaultReuseReplyBoard = service.ReuseReplyBoard;
                service.BatchAcrossRolls = false;
                service.ReuseReplyBoard = false;
                before = counting.PredictionCount;
                reference = await Evaluate();
                expectedRows = counting.PredictionCount - before;
                service.BatchAcrossRolls = defaultBatchAcrossRolls;
                service.ReuseReplyBoard = defaultReuseReplyBoard;
                ConfigureRollBatching(service);
                service.ParallelCandidateCount = window;
                warmup = await Evaluate();
                workerThreads.Verify();
            }

            Validate(warmup);
            WriteProcessMemory("AfterWarmup");

            var samples = new List<Measurement>();
            double[] probes = [];
            WindowProfile? profile = null;

            if (mode == "Mixed")
            {
                var board = BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard(contract);
                var model = service.EvalPositionForTraining(contract, false);
                var expected = await neural.PredictAsync(model, board, false, TestContext.Current.CancellationToken);
                var latencies = new List<double>();
                using var process = Process.GetCurrentProcess();
                var cpu = process.TotalProcessorTime;
                var allocated = GC.GetTotalAllocatedBytes(true);
                var collections = Enumerable.Range(0, 3).Select(GC.CollectionCount).ToArray();
                before = counting.PredictionCount;
                var started = Stopwatch.GetTimestamp();
                var first = Evaluate();
                var second = Evaluate();

                // Observe both searches even if a probe or validation fails.
                try
                {
                    for (var index = 0; index < 100; index++)
                    {
                        Assert.False(first.IsCompleted && second.IsCompleted, "Probes must overlap search.");
                        var probeStart = Stopwatch.GetTimestamp();
                        var actual = await neural.PredictAsync(model, board, false, TestContext.Current.CancellationToken);
                        latencies.Add(Stopwatch.GetElapsedTime(probeStart).TotalMilliseconds);
                        for (var head = 0; head < expected.Length; head++)
                            Assert.InRange(Math.Abs(actual[head] - expected[head]), 0, 1e-5);
                        await Task.Delay(5, TestContext.Current.CancellationToken);
                    }
                }
                finally
                {
                    var results = await Task.WhenAll(first, second);
                    foreach (var result in results)
                    {
                        Validate(result);
                    }
                }
                samples.Add(new Measurement(
                    Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                    (process.TotalProcessorTime - cpu).TotalMilliseconds,
                    GC.GetTotalAllocatedBytes(true) - allocated,
                    GC.CollectionCount(0) - collections[0], GC.CollectionCount(1) - collections[1],
                    GC.CollectionCount(2) - collections[2], counting.PredictionCount - before));
                
                probes = latencies.ToArray();
                Assert.Equal(2 * expectedRows, samples[0].Predictions);
            }
            else if (mode == "Diagnostic")
            {
                // Subscribe only here: timing and mixed-load runs have no metrics listeners.
                using var collector = new WindowMetricsCollector();
                before = counting.PredictionCount;
                Validate(await Evaluate());
                profile = collector.Snapshot();
                Assert.Equal(expectedRows, counting.PredictionCount - before);
                Assert.Equal(expectedRows, profile.BatchSizes.Sum(pair => pair.Key * pair.Value));
                Assert.InRange(profile.PeakCandidates, 1, window);
                Assert.InRange(profile.PeakReplyBoards, 1, window * (service.ReuseReplyBoard ? 1 : 32));
                Assert.Equal(service.ReuseReplyBoard ? reference.Count : expectedRows, profile.ReplyClones);
                Assert.InRange(profile.PeakFeatures, 1, window * 32);
                Assert.InRange(profile.PeakQueuedRows, 1, window * 32);
                Assert.All(profile.BatchSizes, pair => Assert.InRange(pair.Key, 1, 32));
                collector.AssertDrained();
            }
            else
            {
                samples.AddRange(await MeasureAsync(
                    $"Board{boardNumber}", $"Window{window}", reference.Count,
                    () => new ValueTask<FinalEvalResultModels>(Evaluate()), Validate, counting));
                
                Assert.All(samples, sample => Assert.Equal(expectedRows, sample.Predictions));
            }
            WriteProcessMemory("AfterMeasurements");
            using var memory = Process.GetCurrentProcess();

            output.WriteLine("WINDOW_JSON," + JsonConvert.SerializeObject(new
            {
                Board = boardNumber, Window = window, Mode = mode, CrossRollBatching = service.BatchAcrossRolls,
                service.ReuseReplyBoard,
                service.ReuseFeatureBuffers, Threads = get_num_threads(),
                InteropThreads = get_num_interop_threads(), Rows = expectedRows, Candidates = reference.Count,
                Samples = samples, Probes = probes, Profile = profile,
                PrivateBytes = memory.PrivateMemorySize64, WorkingSet = memory.WorkingSet64,
                PeakWorkingSet = memory.PeakWorkingSet64, ManagedLive = GC.GetTotalMemory(false),
                Scores = warmup.Select(row => new { Key = MoveKey(row.MoveSequence), row.Score }).ToArray(),
                Selected = MoveKey(warmup.Select(BotLevel.Expert)),
                MaximumScoreDifference = maximumScoreDifference
            }));

            Task<FinalEvalResultModels> Evaluate()
            {
                // Board construction owns the contract's array, so simultaneous requests need distinct contracts.
                var input = JsonConvert.DeserializeObject<BoardModelContract>(json);
                Assert.NotNull(input);
                return service.EvalMoveSequencesForTrainingAsync(new EvalMoveRequestContract
                {
                    Board = input, IsWhite = false, Modus = GameModus.Backgammon,
                    BotLevel = BotLevel.Expert, Rolls = boardNumber == 2 ? [3, 2] : [2, 2, 2, 2]
                }, EvalWeights.DefaultContactWeights, TestContext.Current.CancellationToken);
            }

            void Validate(FinalEvalResultModels actual)
            {
                Assert.Equal(reference.Count, actual.Count);
                Assert.True(reference.Select(BotLevel.Expert).Equals(actual.Select(BotLevel.Expert)));
                var scores = actual.ToDictionary(row => MoveKey(row.MoveSequence), row => row.Score);
                foreach (var row in reference)
                {
                    var difference = Math.Abs(row.Score - scores[MoveKey(row.MoveSequence)]);
                    maximumScoreDifference = Math.Max(maximumScoreDifference, difference);
                    Assert.InRange(difference, 0, 1e-5);
                }
            }
        }
        finally
        {
            await neural.StopAsync(CancellationToken.None);
        }
    }

    private sealed class WindowMetricsCollector : IDisposable
    {
        private readonly Lock _gate = new();
        private readonly MeterListener _listener = new();
        private readonly Dictionary<string, long> _current = new();
        private readonly Dictionary<string, long> _peak = new();
        private readonly Dictionary<long, long> _batches = new();
        private long _replyClones;

        public WindowMetricsCollector()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == EvaluationMetrics.MeterName &&
                    instrument.Name is EvaluationMetrics.ActiveCandidatesName or EvaluationMetrics.ReplyBoardsName or
                        EvaluationMetrics.FeatureRowsName or EvaluationMetrics.QueuedRowsName or
                        EvaluationMetrics.BatchSizeName or EvaluationMetrics.DurationName)
                    listener.EnableMeasurementEvents(instrument);
            };
            _listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            {
                lock (_gate)
                {
                    if (instrument.Name == EvaluationMetrics.BatchSizeName)
                        _batches[value] = _batches.GetValueOrDefault(value) + 1;
                    else
                    {
                        var current = _current.GetValueOrDefault(instrument.Name) + value;
                        _current[instrument.Name] = current;
                        _peak[instrument.Name] = Math.Max(_peak.GetValueOrDefault(instrument.Name), current);
                    }
                }
            });
            _listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
            {
                if (instrument.Name != EvaluationMetrics.DurationName)
                    return;
                foreach (var tag in tags)
                    if (tag.Key == "stage" && tag.Value is EvaluationMetrics.ReplyClone)
                        Interlocked.Increment(ref _replyClones);
            });
            _listener.Start();
        }

        public WindowProfile Snapshot()
        {
            lock (_gate)
                return new(
                    _peak.GetValueOrDefault(EvaluationMetrics.ActiveCandidatesName),
                    _peak.GetValueOrDefault(EvaluationMetrics.ReplyBoardsName),
                    _peak.GetValueOrDefault(EvaluationMetrics.FeatureRowsName),
                    _peak.GetValueOrDefault(EvaluationMetrics.QueuedRowsName),
                    Interlocked.Read(ref _replyClones),
                    _batches.OrderBy(pair => pair.Key).ToDictionary());
        }

        public void AssertDrained()
        {
            lock (_gate)
                Assert.All(_current.Values, value => Assert.Equal(0, value));
        }
        public void Dispose() => _listener.Dispose();
    }

    private sealed record WindowProfile(
        long PeakCandidates, 
        long PeakReplyBoards, 
        long PeakFeatures, 
        long PeakQueuedRows,
        long ReplyClones,
        Dictionary<long, long> BatchSizes);
}
