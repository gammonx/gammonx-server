using GammonX.Engine.Models;

using GammonX.Mars.NN.Tests.Data;

using GammonX.Models.Enums;

using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Performance;

public partial class PerformanceTests
{
    /// <summary>
    /// Measures one CPU configuration per fresh test process. Set OMP_NUM_THREADS and
    /// MKL_NUM_THREADS before launch; GAMMONX_PERF_THREADS verifies the requested value.
    /// Optional GAMMONX_PERF_BOARD, GAMMONX_PERF_BATCH_SIZE and GAMMONX_PERF_PHASE select the workload.
    /// </summary>
    [Fact]
    public async Task MeasureCpuThreadAndBatchTuning()
    {
        var threads = ReadTuningInt("GAMMONX_PERF_THREADS", get_num_threads());
        var batchSize = ReadTuningInt("GAMMONX_PERF_BATCH_SIZE", 32);
        var boardNumber = ReadTuningInt("GAMMONX_PERF_BOARD", 1);
        var phase = Environment.GetEnvironmentVariable("GAMMONX_PERF_PHASE") ?? "Configured";

        Assert.Contains(phase, new[] { "Configured", "ThreadScreen", "BatchScreen", "ConfirmDefault", "ConfirmTuned" });

        if (Environment.GetEnvironmentVariable("GAMMONX_PERF_THREADS") != null)
        {
            var expected = threads.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(expected, Environment.GetEnvironmentVariable("OMP_NUM_THREADS"));
            Assert.Equal(expected, Environment.GetEnvironmentVariable("MKL_NUM_THREADS"));
        }

        Assert.Equal(threads, get_num_threads());
        var boardJson = boardNumber switch
        {
            1 => MockBoards.PerformanceBoard1,
            2 => MockBoards.PerformanceBoard2,
            3 => MockBoards.PerformanceBoard3,
            _ => throw new ArgumentOutOfRangeException(nameof(boardNumber))
        };

        var configuration = new CpuTuningConfiguration(phase, threads, batchSize);
        output.WriteLine("CPU settings are applied before process startup, not mutated at runtime. Timed iterations have no metrics collector.");

        var result = await MeasureEvalPerformanceAsync($"Board{boardNumber}", boardJson, boardNumber == 2 ? 3 : 2, 2, BotLevel.Expert, tuning: configuration);

        var scores = result.CandidateScores;
        Assert.NotNull(scores);

        output.WriteLine("TUNING_CANDIDATE columns: phase,board,threads,max_batch_size,move_key,score");

        foreach (var candidate in scores)
        {
            output.WriteLine(FormattableString.Invariant( $"TUNING_CANDIDATE,{phase},Board{boardNumber},{threads},{batchSize},{MoveKey(candidate.MoveSequence)},{candidate.Score:R}"));
        }

        output.WriteLine($"TUNING_SELECTION,{phase},Board{boardNumber},{threads},{batchSize},{MoveKey(result.SelectedMove)}");
    }

    private static string MoveKey(MoveSequenceModel sequence) => string.Join('|', sequence.Moves.Select(move => FormattableString.Invariant($"{move.From}>{move.To}")));

    private static int ReadTuningInt(string variable, int defaultValue)
    {
        var text = Environment.GetEnvironmentVariable(variable);

        if (text == null)
        {
            return defaultValue;
        }

        if (!int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            throw new ArgumentException($"{variable} must be a positive integer.", variable);
        }

        return value;
    }
}
