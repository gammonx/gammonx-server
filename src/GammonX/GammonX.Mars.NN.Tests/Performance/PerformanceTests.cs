using GammonX.Engine.Diagnostics;
using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Mars.NN.Models;
using GammonX.Mars.NN.Services;
using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Performance;

[CollectionDefinition(nameof(PerformanceTestCollection), DisableParallelization = true)]
public sealed class PerformanceTestCollection
{
    // pass
}

[Collection(nameof(PerformanceTestCollection))]
public partial class PerformanceTests(ITestOutputHelper output)
{
    private const int MeasurementIterations = 3;

    [Theory]
    [InlineData(2, 2, BotLevel.Hard)]
    [InlineData(2, 2, BotLevel.Expert)]
    public Task MeasureEvalPerformanceBoard1(int roll1, int roll2, BotLevel botLevel) =>
        MeasureEvalPerformanceAsync("Board1", MockBoards.PerformanceBoard1, roll1, roll2, botLevel);

    [Theory]
    [InlineData(2, 2)]
    public Task MeasureEnginePerformanceBoard1(int roll1, int roll2) =>
        MeasureEnginePerformanceAsync("Board1", MockBoards.PerformanceBoard1, roll1, roll2);

    [Theory]
    [InlineData(3, 2, BotLevel.Hard)]
    [InlineData(3, 2, BotLevel.Expert)]
    public Task MeasureEvalPerformanceBoard2(int roll1, int roll2, BotLevel botLevel) =>
        MeasureEvalPerformanceAsync("Board2", MockBoards.PerformanceBoard2, roll1, roll2, botLevel);

    [Theory]
    [InlineData(3, 2)]
    public Task MeasureEnginePerformanceBoard2(int roll1, int roll2) =>
        MeasureEnginePerformanceAsync("Board2", MockBoards.PerformanceBoard2, roll1, roll2);

    [Theory]
    [InlineData(2, 2, BotLevel.Hard)]
    [InlineData(2, 2, BotLevel.Expert)]
    public Task MeasureEvalPerformanceBoard3(int roll1, int roll2, BotLevel botLevel) =>
        MeasureEvalPerformanceAsync("Board3", MockBoards.PerformanceBoard3, roll1, roll2, botLevel);

    [Theory]
    [InlineData(2, 2)]
    public Task MeasureEnginePerformanceBoard3(int roll1, int roll2) =>
        MeasureEnginePerformanceAsync("Board3", MockBoards.PerformanceBoard3, roll1, roll2);

    [Theory]
    [InlineData(1, 2, 2)]
    [InlineData(2, 3, 2)]
    [InlineData(3, 2, 2)]
    public Task ProfileTwoPlyPerformance(int boardNumber, int roll1, int roll2)
    {
        var boardJson = boardNumber switch
        {
            1 => MockBoards.PerformanceBoard1,
            2 => MockBoards.PerformanceBoard2,
            3 => MockBoards.PerformanceBoard3,
            _ => throw new ArgumentOutOfRangeException(nameof(boardNumber))
        };
        return MeasureEvalPerformanceAsync($"Board{boardNumber}", boardJson, roll1, roll2, BotLevel.Expert, collectDiagnostics: true);
    }

    private async Task MeasureEnginePerformanceAsync(string boardName, string boardJson, int roll1, int roll2)
    {
        var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(boardJson);
        Assert.NotNull(boardContract);
        
        var boardService = BoardServiceFactory.Create(GameModus.Backgammon);
        var board = boardService.CreateBoard(boardContract);
        var originalBoard = boardService.CreateBoard(boardContract);
        var rolls = CreateRolls(roll1, roll2);

        WriteEnvironment(rolls);

        var warmup = boardService.GetUniqueLegalMoveSequences(board, false, rolls);
        Assert.NotEmpty(warmup);

        await MeasureAsync(
            boardName,
            "Engine",
            warmup.Length,
            () => ValueTask.FromResult(boardService.GetUniqueLegalMoveSequences(board, false, rolls)),
            result =>
            {
                Assert.Equal(warmup.Length, result.Length);
                Assert.True(board.Equals(originalBoard), "Move generation changed the input board.");
            });
    }

    private async Task<EvaluationMeasurement> MeasureEvalPerformanceAsync(
        string boardName,
        string boardJson,
        int roll1,
        int roll2,
        BotLevel botLevel,
        bool collectDiagnostics = false,
        CpuTuningConfiguration? tuning = null)
    {
        var boardContract = JsonConvert.DeserializeObject<BoardModelContract>(boardJson);
        Assert.NotNull(boardContract);

        var boardService = BoardServiceFactory.Create(GameModus.Backgammon);
        var board = boardService.CreateBoard(boardContract);
        var rolls = CreateRolls(roll1, roll2);

        var legalMoves = boardService.GetUniqueLegalMoveSequences(board, false, rolls);
        Assert.NotEmpty(legalMoves);

        WriteEnvironment(rolls);

        var modelPath = Path.Combine("Data", "NeuralNets", nameof(GameModus.Backgammon), "training_net.dat");
        var startup = Stopwatch.StartNew();
        using var neuralService = Assert.IsType<BatchedNeuralEvalService>(
            BatchedNeuralEvalService.Load(GameModus.Backgammon, modelPath, CPU, tuning?.MaxBatchSize ?? 32));
        ConfigureInputStaging(neuralService);

        try
        {
            await neuralService.StartAsync(CancellationToken.None);
            startup.Stop();
            output.WriteLine(FormattableString.Invariant($"ModelStartupMs={startup.Elapsed.TotalMilliseconds:F3}"));
            output.WriteLine($"TorchThreads={get_num_threads()}; TorchInteropThreads={get_num_interop_threads()}");
            output.WriteLine($"ModelSHA256={Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(modelPath)))}");

            var countingService = new CountingNeuralEvalService(neuralService);
            var evalService = FeatureEvalServiceFactory.Create(GameModus.Backgammon, countingService);
            var features = evalService.EvalPositionForTraining(boardContract, false);
            var request = new EvalMoveRequestContract
            {
                Board = boardContract,
                BotLevel = botLevel,
                IsWhite = false,
                Modus = GameModus.Backgammon,
                Rolls = rolls
            };

            void Validate(MoveSequenceModel result)
            {
                Assert.NotEmpty(result.Moves);
                Assert.Contains(legalMoves, candidate => candidate.Equals(result));
            }

            FinalEvalResultModels? candidateScores = null;
            MoveSequenceModel warmup;
            using (var workerProbe = tuning == null ? null : new MeterListener())
            {
                var workerThreads = 0;
                var mismatchedThreads = -1;
                var workerBatches = 0;
                var expectedThreads = tuning?.Threads ?? 0;
                if (workerProbe != null)
                {
                    workerProbe.InstrumentPublished = (instrument, listener) =>
                    {
                        if (instrument.Meter.Name == EvaluationMetrics.MeterName && instrument.Name == EvaluationMetrics.BatchSizeName)
                            listener.EnableMeasurementEvents(instrument);
                    };
                    workerProbe.SetMeasurementEventCallback<long>((_, _, _, _) =>
                    {
                        var observed = get_num_threads();
                        Interlocked.Exchange(ref workerThreads, observed);
                        Interlocked.Increment(ref workerBatches);
                        if (observed != expectedThreads)
                            Interlocked.Exchange(ref mismatchedThreads, observed);
                    });
                    workerProbe.Start();
                }

                var firstInference = Stopwatch.StartNew();
                var prediction = await countingService.PredictAsync(features, board, false);
                firstInference.Stop();
                Assert.NotEmpty(prediction);
                Assert.All(prediction, value => Assert.True(float.IsFinite(value)));
                output.WriteLine(FormattableString.Invariant($"FirstInferenceMs={firstInference.Elapsed.TotalMilliseconds:F3}"));
                var warmupStart = Stopwatch.GetTimestamp();
                if (tuning != null)
                {
                    candidateScores = await evalService.EvalMoveSequencesForTrainingAsync(request, EvalWeights.DefaultContactWeights);
                    Assert.Equal(legalMoves.Length, candidateScores.Count);
                    Assert.All(candidateScores, result => Assert.True(double.IsFinite(result.Score)));
                    warmup = candidateScores.Select(botLevel);
                    Assert.True(workerBatches > 0);
                    Assert.True(mismatchedThreads == -1, $"Expected {tuning.Threads} Torch threads on every worker batch; observed {mismatchedThreads}.");
                    Assert.Equal(tuning.Threads, workerThreads);
                    output.WriteLine($"TuningPhase={tuning.Phase}; WorkerTorchThreads={workerThreads}; WorkerBatchesVerified={workerBatches}; MaxBatchSize={tuning.MaxBatchSize}");
                }
                else
                {
                    warmup = await evalService.EvalMoveSequencesAsync(request, EvalWeights.DefaultContactWeights);
                }
                output.WriteLine(FormattableString.Invariant(
                    $"FullEvaluationWarmupMs={Stopwatch.GetElapsedTime(warmupStart).TotalMilliseconds:F3}"));
                Validate(warmup);
            }

            WriteProcessMemory("AfterWarmup");
            var samples = await MeasureAsync(
                boardName,
                botLevel == BotLevel.Expert ? "TwoPly" : "OnePly",
                legalMoves.Length,
                () => new ValueTask<MoveSequenceModel>(
                    evalService.EvalMoveSequencesAsync(request, EvalWeights.DefaultContactWeights)),
                result =>
                {
                    Validate(result);
                    if (collectDiagnostics || tuning != null)
                        Assert.True(warmup.Equals(result), "Measured evaluation selected a different move than the warm-up.");
                },
                countingService,
                collectDiagnostics,
                tuning);
            WriteProcessMemory("AfterMeasurements");
            return new EvaluationMeasurement(warmup, legalMoves.Length, candidateScores, samples);
        }
        finally
        {
            await neuralService.StopAsync(CancellationToken.None);
            WriteProcessMemory("AfterWorkerStop");
        }
    }

    private async Task<Measurement[]> MeasureAsync<T>(
        string boardName,
        string workload,
        int rootCandidates,
        Func<ValueTask<T>> operation,
        Action<T> validate,
        CountingNeuralEvalService? neuralService = null,
        bool collectDiagnostics = false,
        CpuTuningConfiguration? tuning = null)
    {
        using var process = Process.GetCurrentProcess();
        var samples = new Measurement[MeasurementIterations];

        for (var iteration = 0; iteration < samples.Length; iteration++)
        {
            using var diagnostics = collectDiagnostics ? new EvaluationMetricsCollector() : null;
            var predictionsBefore = neuralService?.PredictionCount ?? 0;
            var cpuBefore = process.TotalProcessorTime;
            // Process-wide counters include allocations made by the inference worker after an await.
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            var gen0Before = GC.CollectionCount(0);
            var gen1Before = GC.CollectionCount(1);
            var gen2Before = GC.CollectionCount(2);
            var start = Stopwatch.GetTimestamp();

            var result = await operation();

            var wallMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            var allocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
            samples[iteration] = new Measurement(
                wallMs,
                (process.TotalProcessorTime - cpuBefore).TotalMilliseconds,
                allocatedBytes,
                GC.CollectionCount(0) - gen0Before,
                GC.CollectionCount(1) - gen1Before,
                GC.CollectionCount(2) - gen2Before,
                (neuralService?.PredictionCount ?? 0) - predictionsBefore);

            validate(result);
            if (diagnostics != null)
            {
                var profile = diagnostics.Snapshot();
                ValidateProfile(profile, rootCandidates, samples[iteration].Predictions);
                WriteProfile(boardName, workload, iteration + 1, profile);
            }
        }

        Assert.All(samples, sample => Assert.Equal(samples[0].Predictions, sample.Predictions));
        var samplePrefix = tuning != null ? "TUNING_SAMPLE" : collectDiagnostics ? "PROFILE_SAMPLE" : "SAMPLE";
        var summaryPrefix = tuning != null ? "TUNING_SUMMARY" : collectDiagnostics ? "PROFILE_SUMMARY" : "BASELINE";
        var axes = tuning == null
            ? $"{boardName},{workload}"
            : $"{tuning.Phase},{boardName},{workload},{tuning.Threads},{tuning.MaxBatchSize}";
        var axisColumns = tuning == null ? "board,workload" : "phase,board,workload,threads,max_batch_size";
        output.WriteLine($"{samplePrefix} columns: {axisColumns},iteration,root_candidates,wall_ms,cpu_ms,allocated_bytes,gen0,gen1,gen2,predictions");

        for (var iteration = 0; iteration < samples.Length; iteration++)
        {
            var sample = samples[iteration];
            output.WriteLine(FormattableString.Invariant($"{samplePrefix},{axes},{iteration + 1},{rootCandidates},{sample.WallMs:F3},{sample.CpuMs:F3},{sample.AllocatedBytes},{sample.Gen0},{sample.Gen1},{sample.Gen2},{sample.Predictions}"));
        }

        output.WriteLine($"{summaryPrefix} columns: {axisColumns},root_candidates,min_ms,median_ms,max_ms,median_cpu_ms,median_allocated_bytes,gen0_total,gen1_total,gen2_total,predictions_per_iteration");
        output.WriteLine(FormattableString.Invariant( $"{summaryPrefix},{axes},{rootCandidates},{samples.Min(s => s.WallMs):F3},{samples.OrderBy(s => s.WallMs).ElementAt(MeasurementIterations / 2).WallMs:F3},{samples.Max(s => s.WallMs):F3},{samples.OrderBy(s => s.CpuMs).ElementAt(MeasurementIterations / 2).CpuMs:F3},{samples.OrderBy(s => s.AllocatedBytes).ElementAt(MeasurementIterations / 2).AllocatedBytes},{samples.Sum(s => s.Gen0)},{samples.Sum(s => s.Gen1)},{samples.Sum(s => s.Gen2)},{samples[0].Predictions}"));
        return samples;
    }

    private static void ValidateProfile(EvaluationProfile profile, int rootCandidates, long predictions)
    {
        var generation = profile.GetStage(EvaluationMetrics.MoveGeneration);
        Assert.Equal(1 + 21L * rootCandidates, generation.Calls);
        Assert.Equal(generation.Calls, profile.RawMeasurements);
        Assert.Equal(generation.Calls, profile.UniqueMeasurements);
        Assert.Equal(profile.UniqueSequences, profile.GetStage(EvaluationMetrics.GenerationClone).Calls);
        Assert.True(profile.GetStage(EvaluationMetrics.GenerationClone).Calls < profile.RawSequences);
        Assert.True(profile.ReusedStates > 0);
        Assert.True(profile.GetStage(EvaluationMetrics.TraversalSnapshot).Calls > 0);
        Assert.Equal(profile.UniqueSequences - rootCandidates, profile.GetStage(EvaluationMetrics.ReplyClone).Calls);
        Assert.True(profile.GetStage(EvaluationMetrics.ReplyClone).Calls <= predictions);
        Assert.Equal(rootCandidates + predictions, profile.GetStage(EvaluationMetrics.Features).Calls);
        Assert.Equal(predictions, profile.GetStage(EvaluationMetrics.FeatureVector).Calls);
        Assert.Equal(predictions, profile.GetStage(EvaluationMetrics.QueueWait).Calls);
        Assert.Equal(predictions, profile.BatchSizes.Sum(batch => batch.Key * batch.Value));
        Assert.InRange(profile.InferenceRequests, 1, predictions);
        Assert.All(profile.BatchSizes, batch => Assert.InRange(batch.Key, 1, 32));
        var batches = profile.BatchSizes.Sum(batch => batch.Value);
        Assert.Equal(batches, profile.GetStage(EvaluationMetrics.Forward).Calls);
        Assert.Equal(batches, profile.GetStage(EvaluationMetrics.BatchPreparation).Calls);
        Assert.All(profile.Stages, stage =>
        {
            Assert.True(stage.TotalMs >= 0);
            if (stage.Stage == EvaluationMetrics.QueueWait)
            {
                Assert.Null(stage.AllocatedBytes);
            }
            else
            {
                Assert.Equal(stage.Calls, stage.AllocationSamples);
                Assert.True(stage.AllocatedBytes >= 0);
            }
        });
    }

    private void WriteProfile(string boardName, string workload, int iteration, EvaluationProfile profile)
    {
        output.WriteLine("Stage times are cumulative elapsed work, not a wall-time partition. GenerationClone and TraversalSnapshot are included in MoveGeneration; queue waits overlap. Allocation bytes are thread-local synchronous stage deltas.");
        output.WriteLine("PROFILE_STAGE columns: board,workload,iteration,stage,calls,total_ms,allocated_bytes");
        foreach (var stage in profile.Stages)
        {
            output.WriteLine(FormattableString.Invariant(
                $"PROFILE_STAGE,{boardName},{workload},{iteration},{stage.Stage},{stage.Calls},{stage.TotalMs:F6},{stage.AllocatedBytes}"));
        }
        output.WriteLine("PROFILE_BATCH columns: board,workload,iteration,batch_size,batch_count");
        foreach (var batch in profile.BatchSizes)
        {
            output.WriteLine(FormattableString.Invariant(
                $"PROFILE_BATCH,{boardName},{workload},{iteration},{batch.Key},{batch.Value}"));
        }
        output.WriteLine("PROFILE_SEARCH columns: board,workload,iteration,generation_calls,raw_sequences,unique_sequences,reused_states");
        output.WriteLine(FormattableString.Invariant(
            $"PROFILE_SEARCH,{boardName},{workload},{iteration},{profile.RawMeasurements},{profile.RawSequences},{profile.UniqueSequences},{profile.ReusedStates}"));
        var forwards = profile.BatchSizes.Sum(batch => batch.Value);
        var predictions = profile.BatchSizes.Sum(batch => batch.Key * batch.Value);
        output.WriteLine("PROFILE_INFERENCE columns: board,workload,iteration,queue_requests,predictions,forward_passes,average_batch_size");
        output.WriteLine(FormattableString.Invariant(
            $"PROFILE_INFERENCE,{boardName},{workload},{iteration},{profile.InferenceRequests},{predictions},{forwards},{(double)predictions / forwards:F6}"));
    }

    private void ConfigureInputStaging(BatchedNeuralEvalService service)
    {
        service.ReuseCPUTensor = Environment.GetEnvironmentVariable("GAMMONX_PERF_REUSE_INPUT") switch
        {
            null or "1" => true,
            "0" => false,
            _ => throw new ArgumentException("GAMMONX_PERF_REUSE_INPUT must be 0 or 1.")
        };
        output.WriteLine($"ReuseCpuInput={service.ReuseCPUTensor}");
    }

    private void WriteProcessMemory(string phase)
    {
        using var process = Process.GetCurrentProcess();
        output.WriteLine("MEMORY columns: phase,private_bytes,working_set_bytes,peak_working_set_bytes,managed_live_bytes,live_tensor_handles");
        output.WriteLine(FormattableString.Invariant(
            $"MEMORY,{phase},{process.PrivateMemorySize64},{process.WorkingSet64},{process.PeakWorkingSet64},{GC.GetTotalMemory(forceFullCollection: false)},{Tensor.TotalCount}"));
    }

    private void WriteEnvironment(int[] rolls)
    {
        output.WriteLine($"Runtime={RuntimeInformation.FrameworkDescription}; OS={RuntimeInformation.OSDescription}; Architecture={RuntimeInformation.ProcessArchitecture}; LogicalProcessors={Environment.ProcessorCount}");
        output.WriteLine($"Device=CPU; IsWhite=false; Rolls={string.Join(',', rolls)}; WarmupIterations=1; MeasurementIterations={MeasurementIterations}");
        output.WriteLine("CPU, allocation and GC measurements are process-wide; allocation bytes exclude native Torch memory. No forced GC.");
    }

    private static int[] CreateRolls(int roll1, int roll2) =>
        roll1 == roll2 ? [roll1, roll1, roll1, roll1] : [roll1, roll2];

    private sealed record Measurement(
        double WallMs,
        double CpuMs,
        long AllocatedBytes,
        int Gen0,
        int Gen1,
        int Gen2,
        long Predictions);

    private sealed record EvaluationMeasurement(
        MoveSequenceModel SelectedMove,
        // ReSharper disable once NotAccessedPositionalProperty.Local
        int RootCandidates,
        FinalEvalResultModels? CandidateScores,
        Measurement[] Samples)
    {
        // ReSharper disable once UnusedMember.Local
        public double MedianMs => Samples.OrderBy(sample => sample.WallMs).ElementAt(MeasurementIterations / 2).WallMs;
    }

    private sealed record CpuTuningConfiguration(string Phase, int Threads, int MaxBatchSize);

    private sealed class CountingNeuralEvalService(BatchedNeuralEvalService inner) : INeuralEvalService
    {
        private long _predictionCount;
        private readonly INeuralEvalService _batch = inner;

        public long PredictionCount => Interlocked.Read(ref _predictionCount);

        public int MaxBatchSize => _batch.MaxBatchSize;

        public float[] ExtractFeatures(NormalizedEvalResultModel model, IBoardModel board, bool isWhite) =>
            _batch.ExtractFeatures(model, board, isWhite);

        public Task<float[][]> PredictBatchAsync(float[][] features)
        {
            Interlocked.Add(ref _predictionCount, features.Length);
            return _batch.PredictBatchAsync(features);
        }

        public Task<float[]> PredictAsync(NormalizedEvalResultModel model, IBoardModel board, bool isWhite)
        {
            Interlocked.Increment(ref _predictionCount);
            return inner.PredictAsync(model, board, isWhite);
        }
    }
}