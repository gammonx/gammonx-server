using System.Globalization;
using System.Text;

namespace GammonX.Server.Performance.NN;

internal sealed class CpuTuningStudy(StudyProcesses processes)
{
    private const double ScoreTolerance = 1e-5;
    private const int DefaultBatchSize = 32;
    private static readonly string[] SampleFields = ["Phase", "Board", "Workload", "Threads", "MaxBatchSize", "Iteration",
        "RootCandidates", "WallMs", "CpuMs", "AllocatedBytes", "Gen0", "Gen1", "Gen2", "Predictions"];
    private static readonly string[] SummaryFields = ["Phase", "Board", "Workload", "Threads", "MaxBatchSize", "RootCandidates",
        "MinMs", "MedianMs", "MaxMs", "MedianCpuMs", "MedianAllocatedBytes", "Gen0Total", "Gen1Total", "Gen2Total", "PredictionsPerIteration"];
    private static readonly string[] CandidateFields = ["Phase", "Board", "Threads", "MaxBatchSize", "MoveKey", "Score"];
    private static readonly string[] SelectionFields = ["Phase", "Board", "Threads", "MaxBatchSize", "MoveKey"];
    private readonly List<CpuRun> _runs = [];

    internal async Task RunAsync(int baselineThreads)
    {
        var baseline = await RunCaseAsync("ThreadScreen", 1, baselineThreads, DefaultBatchSize);
        var threadRuns = new List<CpuRun> { baseline };
        foreach (var threads in new[] { 1, 8, 2, 4 }.Where(threads => threads != baselineThreads))
        {
            threadRuns.Add(await RunCaseAsync("ThreadScreen", 1, threads, DefaultBatchSize, baseline));
        }
        var bestThreads = int.Parse(threadRuns.MinBy(run => run.MedianMs)!.Summary["Threads"], CultureInfo.InvariantCulture);
        var batchRuns = new List<CpuRun>();
        foreach (var batch in new[] { 8, 32, 16, 64 })
        {
            batchRuns.Add(await RunCaseAsync("BatchScreen", 1, bestThreads, batch, baseline));
        }
        var bestBatch = int.Parse(batchRuns.MinBy(run => run.MedianMs)!.Summary["MaxBatchSize"], CultureInfo.InvariantCulture);
        Console.WriteLine($"Screening selected {bestThreads} threads and maximum batch {bestBatch}.");

        var confirmations = new Dictionary<int, (List<CpuRun> Defaults, List<CpuRun> Tuned)>();
        foreach (var board in new[] { 1, 2, 3 })
        {
            var defaults = new List<CpuRun>();
            var tuned = new List<CpuRun>();
            if (board == 2)
            {
                tuned.Add(await RunCaseAsync("ConfirmTuned", board, bestThreads, bestBatch));
                defaults.Add(await RunCaseAsync("ConfirmDefault", board, baselineThreads, DefaultBatchSize, tuned[0]));
            }
            else
            {
                defaults.Add(await RunCaseAsync("ConfirmDefault", board, baselineThreads, DefaultBatchSize));
                tuned.Add(await RunCaseAsync("ConfirmTuned", board, bestThreads, bestBatch, defaults[0]));
            }
            confirmations.Add(board, (defaults, tuned));
        }
        var (board2Defaults, board2Tuned) = confirmations[2];
        board2Defaults.Add(await RunCaseAsync("ConfirmDefault", 2, baselineThreads, DefaultBatchSize, board2Defaults[0], "-Repeat"));
        board2Tuned.Add(await RunCaseAsync("ConfirmTuned", 2, bestThreads, bestBatch, board2Defaults[0], "-Repeat"));
        var comparisons = confirmations.Values.Select(pair => CompareConfirmation(pair.Defaults, pair.Tuned)).ToArray();

        var fields = new[] { "Board", "DefaultThreads", "DefaultBatchSize", "TunedThreads", "TunedBatchSize", "DefaultMedianMs", "TunedMedianMs",
            "Speedup", "DefaultMedianCpuMs", "TunedMedianCpuMs", "MaxCandidateScoreDelta", "SelectedMoveUnchanged", "ConfirmationRunsPerConfiguration" };
        SaveCsv("confirmation-comparison.csv", fields, comparisons.Select(row => new object[]
        {
            row.Board, row.DefaultThreads, row.DefaultBatchSize, row.TunedThreads, row.TunedBatchSize,
            row.DefaultMedianMs, row.TunedMedianMs, row.Speedup, row.DefaultMedianCpuMs, row.TunedMedianCpuMs,
            row.MaxCandidateScoreDelta, row.SelectedMoveUnchanged, row.ConfirmationRunsPerConfiguration
        }.Select(value => Convert.ToString(value, CultureInfo.InvariantCulture)!)));
        processes.SaveJson("tuning-context.json", new
        {
            RecordedAt = DateTimeOffset.UtcNow, processes.AssemblyVersion, processes.Fingerprints,
            Choice = new { Threads = bestThreads, MaxBatchSize = bestBatch },
            Environment = baseline.Context, ScoreTolerance,
            PassingConfigurations = _runs.Count, Samples = _runs.Sum(run => run.Samples.Length),
            MaxCandidateScoreDelta = _runs.Max(run => run.ScoreDelta),
            WorkerVerification = _runs.Select(run => run.Worker), Comparisons = comparisons
        });
        Console.WriteLine($"Reports: {processes.Output}");
    }

    private async Task<CpuRun> RunCaseAsync(string phase, int board, int threads, int batch, CpuRun? reference = null, string suffix = "")
    {
        var name = $"{phase}-Board{board}-T{threads}-B{batch}{suffix}";
        var lines = await processes.RunAsync(name, "cpu-case", threads, ("PHASE", phase),
            ("BOARD", board.ToString(CultureInfo.InvariantCulture)), ("BATCH_SIZE", batch.ToString(CultureInfo.InvariantCulture)));
        var summary = ReadRows(lines, "TUNING_SUMMARY", SummaryFields).Single();
        var samples = ReadRows(lines, "TUNING_SAMPLE", SampleFields);
        var candidates = ReadRows(lines, "TUNING_CANDIDATE", CandidateFields);
        var selection = ReadRows(lines, "TUNING_SELECTION", SelectionFields).Single();
        var context = new Dictionary<string, string>
        {
            ["ModelSHA256"] = StudyProcesses.SingleLine(lines, "ModelSHA256="),
            ["Runtime"] = StudyProcesses.SingleLine(lines, "Runtime="),
            ["TorchThreads"] = StudyProcesses.SingleLine(lines, "TorchThreads=")
        };
        var worker = StudyProcesses.SingleLine(lines, "TuningPhase=");
        var workerParts = worker.Split("; ");
        var torchParts = context["TorchThreads"].Split("; ");
        if (workerParts.Length != 4 || workerParts[0] != phase || workerParts[1] != $"WorkerTorchThreads={threads}"
            || !workerParts[2].StartsWith("WorkerBatchesVerified=", StringComparison.Ordinal)
            || !int.TryParse(workerParts[2]["WorkerBatchesVerified=".Length..], out var batches) || batches <= 0
            || workerParts[3] != $"MaxBatchSize={batch}" || torchParts.Length != 2 || torchParts[0] != threads.ToString(CultureInfo.InvariantCulture)
            || !torchParts[1].StartsWith("TorchInteropThreads=", StringComparison.Ordinal)
            || !int.TryParse(torchParts[1]["TorchInteropThreads=".Length..], out var interopThreads) || interopThreads <= 0)
        {
            throw new InvalidDataException($"Unexpected Torch/worker settings for {name}.");
        }
        context["TorchInteropThreads"] = interopThreads.ToString(CultureInfo.InvariantCulture);
        foreach (var row in samples.Concat(candidates).Append(summary).Append(selection))
        {
            if (row["Phase"] != phase || row["Board"] != $"Board{board}" || row["Threads"] != threads.ToString(CultureInfo.InvariantCulture)
                || row["MaxBatchSize"] != batch.ToString(CultureInfo.InvariantCulture))
            {
                throw new InvalidDataException($"Configuration mismatch for {name}.");
            }
        }
        var scores = candidates.ToDictionary(row => row["MoveKey"], row => Number(row, "Score"));
        var times = samples.Select(row => Number(row, "WallMs")).ToArray();
        if (!samples.Select(row => row["Iteration"]).Order().SequenceEqual(["1", "2", "3"])
            || samples.Any(row => row["Workload"] != "TwoPly" || row["Predictions"] != summary["PredictionsPerIteration"]
                || row["RootCandidates"] != summary["RootCandidates"])
            || scores.Count == 0 || scores.Count != Number(summary, "RootCandidates") || !scores.ContainsKey(selection["MoveKey"])
            || times.Any(time => time <= 0) || Math.Abs(times.Min() - Number(summary, "MinMs")) > 0.001
            || Math.Abs(StudyProcesses.Median(times) - Number(summary, "MedianMs")) > 0.001
            || Math.Abs(times.Max() - Number(summary, "MaxMs")) > 0.001)
        {
            throw new InvalidDataException($"Unexpected benchmark data for {name}.");
        }
        var result = new CpuRun(name, summary, samples, candidates, scores, selection["MoveKey"], context, "TuningPhase=" + worker);
        if (reference != null)
        {
            result = result with { ScoreDelta = CompareScores(reference, result) };
        }
        _runs.Add(result);
        SaveCsv("tuning-summary.csv", ["Run", .. SummaryFields], _runs.Select(run => RawRow(run.Name, run.Summary, SummaryFields)));
        SaveCsv("tuning-samples.csv", ["Run", .. SampleFields], _runs.SelectMany(run => run.Samples.Select(row => RawRow(run.Name, row, SampleFields))));
        SaveCsv("candidate-scores.csv", ["Run", .. CandidateFields], _runs.SelectMany(run => run.Candidates.Select(row => RawRow(run.Name, row, CandidateFields))));
        Console.WriteLine(FormattableString.Invariant($"{name}: median={result.MedianMs:F3} ms, max_score_delta={result.ScoreDelta:G3}"));
        return result;
    }

    private static double CompareScores(CpuRun reference, CpuRun candidate)
    {
        if (reference.Selection != candidate.Selection || reference.Scores.Count != candidate.Scores.Count
            || reference.Scores.Keys.Any(key => !candidate.Scores.ContainsKey(key))
            || new[] { "RootCandidates", "PredictionsPerIteration" }.Any(key => reference.Summary[key] != candidate.Summary[key])
            || new[] { "ModelSHA256", "Runtime", "TorchInteropThreads" }.Any(key => reference.Context[key] != candidate.Context[key]))
        {
            throw new InvalidDataException($"Search coverage, selected move, or runtime changed in {candidate.Name}.");
        }
        var delta = reference.Scores.Max(row => Math.Abs(row.Value - candidate.Scores[row.Key]));
        if (delta > ScoreTolerance)
        {
            throw new InvalidDataException($"Score tolerance exceeded in {candidate.Name}: {delta}.");
        }
        return delta;
    }

    private static Confirmation CompareConfirmation(List<CpuRun> defaults, List<CpuRun> tuned)
    {
        var reference = defaults[0];
        var candidate = tuned[0];
        double Median(List<CpuRun> runs, string field) => StudyProcesses.Median(runs.SelectMany(run => run.Samples.Select(row => Number(row, field))));
        var before = Median(defaults, "WallMs");
        var after = Median(tuned, "WallMs");
        return new Confirmation(reference.Summary["Board"], Number(reference.Summary, "Threads"), Number(reference.Summary, "MaxBatchSize"),
            Number(candidate.Summary, "Threads"), Number(candidate.Summary, "MaxBatchSize"), before, after, before / after,
            Median(defaults, "CpuMs"), Median(tuned, "CpuMs"), defaults.Concat(tuned).Max(run => CompareScores(reference, run)), true, defaults.Count);
    }

    private static Dictionary<string, string>[] ReadRows(string[] lines, string prefix, string[] fields) =>
        lines.Where(line => line.StartsWith(prefix + ",", StringComparison.Ordinal)).Select(line =>
        {
            // The benchmark emits fixed fields. Move keys use vertical bars and never contain commas.
            var values = line[(prefix.Length + 1)..].Split(',');
            if (values.Length != fields.Length)
            {
                throw new InvalidDataException($"Unexpected columns in {prefix}.");
            }
            return fields.Zip(values).ToDictionary(pair => pair.First, pair => pair.Second);
        }).ToArray();

    private static double Number(Dictionary<string, string> row, string field)
    {
        if (!double.TryParse(row[field], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
        {
            throw new InvalidDataException($"Invalid benchmark value for {field}: {row[field]}");
        }
        return value;
    }

    private static IEnumerable<string> RawRow(string run, Dictionary<string, string> row, string[] fields) =>
        fields.Select(field => row[field]).Prepend(run);

    private void SaveCsv(string name, string[] fields, IEnumerable<IEnumerable<string>> rows)
    {
        using var writer = new StreamWriter(Path.Combine(processes.Output, name), false, new UTF8Encoding(true));
        writer.WriteLine(string.Join(',', fields));
        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(',', row.Select(value => "\"" + value.Replace("\"", "\"\"") + "\"")));
        }
    }

    private sealed record CpuRun(string Name, Dictionary<string, string> Summary, Dictionary<string, string>[] Samples,
        Dictionary<string, string>[] Candidates, Dictionary<string, double> Scores, string Selection, Dictionary<string, string> Context,
        string Worker, double ScoreDelta = 0)
    {
        internal double MedianMs => Number(Summary, "MedianMs");
    }

    private sealed record Confirmation(string Board, double DefaultThreads, double DefaultBatchSize, double TunedThreads, double TunedBatchSize,
        double DefaultMedianMs, double TunedMedianMs, double Speedup, double DefaultMedianCpuMs, double TunedMedianCpuMs,
        double MaxCandidateScoreDelta, bool SelectedMoveUnchanged, int ConfirmationRunsPerConfiguration);
}
