using GammonX.Engine.Services;
using GammonX.Mars.NN.Services;
using GammonX.Mars.NN.Tests.Data;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;
using Newtonsoft.Json;
using System.Diagnostics;
using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Performance;

public partial class PerformanceTests
{
    [Fact]
    public async Task MeasureSharedWorkerInference()
    {
        var maxBatchSize = ReadTuningInt("GAMMONX_PERF_BATCH_SIZE", 32);
        var modelPath = Path.Combine("Data", "NeuralNets", nameof(GameModus.Backgammon), "training_net.dat");

        using var neural = Assert.IsType<BatchedNeuralEvalService>(BatchedNeuralEvalService.Load(GameModus.Backgammon, modelPath, CPU, maxBatchSize));
        ConfigureInputStaging(neural);
        await neural.StartAsync(TestContext.Current.CancellationToken);

        try
        {
            var service = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(FeatureEvalServiceFactory.Create(GameModus.Backgammon, neural));
            var contract = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PerformanceBoard1);
            Assert.NotNull(contract);

            var board = BoardServiceFactory.Create(GameModus.Backgammon).CreateBoard(contract);
            var features = service.EvalPositionForTraining(contract, false);
            var expected = await neural.PredictAsync(features, board, false);
            await service.EvalMoveSequencesAsync(Request(), EvalWeights.DefaultContactWeights);

            WriteProcessMemory("AfterWarmup");
            var singleLatencies = new List<double>();
            using var process = Process.GetCurrentProcess();
            var cpuStart = process.TotalProcessorTime;
            var start = Stopwatch.GetTimestamp();

            var first = service.EvalMoveSequencesAsync(Request(), EvalWeights.DefaultContactWeights);
            var second = service.EvalMoveSequencesAsync(Request(), EvalWeights.DefaultContactWeights);

            for (var index = 0; index < 100; index++)
            {
                if (first.IsCompleted && second.IsCompleted)
                    break;

                var singleStart = Stopwatch.GetTimestamp();
                var actual = await neural.PredictAsync(features, board, false);
                singleLatencies.Add(Stopwatch.GetElapsedTime(singleStart).TotalMilliseconds);

                for (var head = 0; head < expected.Length; head++)
                {
                    Assert.InRange(Math.Abs(actual[head] - expected[head]), 0f, 1e-5f);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(5), TestContext.Current.CancellationToken);
            }

            var moves = await Task.WhenAll(first, second);
            var wall = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            var cpu = (process.TotalProcessorTime - cpuStart).TotalMilliseconds;
            WriteProcessMemory("AfterMeasurements");
            Assert.True(moves[0].Equals(moves[1]));
            Assert.NotEmpty(moves[0].Moves);
            Assert.NotEmpty(singleLatencies);

            var ordered = singleLatencies.Order().ToArray();
            output.WriteLine($"MixedMaxBatchSize={maxBatchSize}");
            output.WriteLine("MIXED columns: evaluations,single_predictions,wall_ms,cpu_ms,single_median_ms,single_p95_ms,single_max_ms");
            output.WriteLine(FormattableString.Invariant(
                $"MIXED,2,{ordered.Length},{wall:F6},{cpu:F6},{(ordered[(ordered.Length - 1) / 2] + ordered[ordered.Length / 2]) / 2:F6},{ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1]:F6},{ordered[^1]:F6}"));

            foreach (var latency in singleLatencies)
            {
                output.WriteLine(FormattableString.Invariant($"MIXED_SINGLE,{latency:F6}"));
            }

            EvalMoveRequestContract Request()
            {
                var position = JsonConvert.DeserializeObject<BoardModelContract>(MockBoards.PerformanceBoard1);
                Assert.NotNull(position);
                return new EvalMoveRequestContract
                {
                    Board = position,
                    BotLevel = BotLevel.Expert,
                    IsWhite = false,
                    Modus = GameModus.Backgammon,
                    Rolls = [2, 2, 2, 2]
                };
            }
        }
        finally
        {
            await neural.StopAsync(CancellationToken.None);
            WriteProcessMemory("AfterWorkerStop");
        }
    }
}
