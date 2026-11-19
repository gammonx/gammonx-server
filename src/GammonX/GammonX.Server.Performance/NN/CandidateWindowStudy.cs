using System.Globalization;
using System.Text.Json;

namespace GammonX.Server.Performance.NN;

internal sealed class CandidateWindowStudy(StudyProcesses processes, int threads)
{
    private static readonly int[] Boards = [1, 2, 3];
    private static readonly StudyThresholds Thresholds = new();
    private readonly List<WindowRun> _runs = [];

    internal async Task RunAsync(string command, string optimization, bool quick)
    {
        var windows = command == "candidate-windows";
        Configuration[] configurations = windows
            ? [new("1", 1), new("2", 2), new("4", 4)]
            : optimization switch
            {
                "roll-batching" => [new("off", CrossRoll: false), new("on")],
                "reply-board" => [new("off"), new("on", ReplyBoard: true)],
                "feature-buffers" => [new("off", ReplyBoard: true), new("on", ReplyBoard: true, FeatureBuffers: true)],
                "per-roll-reuse" => [new("baseline", CrossRoll: false), new("board", CrossRoll: false, ReplyBoard: true),
                    new("both", CrossRoll: false, ReplyBoard: true, FeatureBuffers: true)],
                _ => throw new ArgumentException($"Unknown optimization: {optimization}")
            };

        var rounds = quick ? 1 : Thresholds.Rounds;
        var orders = Enumerable.Range(1, rounds).Select(round =>
        {
            if (!windows && optimization == "per-roll-reuse")
            {
                var offset = (4 - round) % 3;
                return configurations.Skip(offset).Concat(configurations.Take(offset)).ToArray();
            }
            return round % 2 == 0 ? configurations.Reverse().ToArray() : configurations;
        }).ToArray();
        processes.SaveJson("thresholds.json", Thresholds);
        processes.SaveJson("protocol.json", new { Command = command, Optimization = windows ? null : optimization, Quick = quick, Threads = threads, RoundOrder = orders });

        for (var round = 1; round <= rounds; round++)
        {
            foreach (var board in Boards)
            {
                foreach (var configuration in orders[round - 1])
                {
                    await RunCaseAsync(round, board, "Timing", configuration, windows);
                }
            }
        }
        foreach (var board in quick ? [1] : Boards)
        {
            foreach (var configuration in orders[0])
            {
                await RunCaseAsync(1, board, "Diagnostic", configuration, windows);
            }
        }
        for (var round = 1; round <= rounds; round++)
        {
            foreach (var configuration in orders[round - 1])
            {
                await RunCaseAsync(round, 1, "Mixed", configuration, windows);
            }
        }

        var scores = CheckScores();
        var comparisons = new List<Comparison>();
        if (!quick)
        {
            comparisons.AddRange(configurations.Skip(1).Select(candidate => Compare(configurations[0], candidate)));
            if (!windows && optimization == "per-roll-reuse")
            {
                comparisons.Add(Compare(configurations[1], configurations[2]));
            }
        }
        int? chosen = windows
            ? int.Parse(comparisons.FirstOrDefault(result => result.Accepted)?.Candidate ?? "1", CultureInfo.InvariantCulture)
            : null;
        processes.SaveJson("results.json", new
        {
            RecordedAt = DateTimeOffset.UtcNow, Command = command, Optimization = windows ? null : optimization,
            Quick = quick, ChosenWindow = chosen, Thresholds, processes.AssemblyVersion, processes.Fingerprints, Comparisons = comparisons,
            ScoreChecks = scores, Runs = _runs,
            ScreeningNote = quick ? "Quick runs screen configurations. Acceptance requires the full study." : null
        });
        if (chosen != null)
        {
            Console.WriteLine($"Chosen window: {chosen}");
        }
        Console.WriteLine($"Reports: {processes.Output}");
    }

    private async Task RunCaseAsync(int round, int board, string mode, Configuration configuration, bool windows)
    {
        var name = $"{mode}-r{round}-b{board}-{configuration.Name}";
        var settings = new List<(string Key, string Value)>
        {
            ("BOARD", board.ToString(CultureInfo.InvariantCulture)),
            ("WINDOW", configuration.Window.ToString(CultureInfo.InvariantCulture)),
            ("WINDOW_MODE", mode)
        };
        if (!windows)
        {
            settings.Add(("CROSS_ROLL_BATCHING", configuration.CrossRoll ? "1" : "0"));
            settings.Add(("REUSE_REPLY_BOARD", configuration.ReplyBoard ? "1" : "0"));
            settings.Add(("REUSE_FEATURE_BUFFERS", configuration.FeatureBuffers ? "1" : "0"));
        }
        var lines = await processes.RunAsync(name, "window-case", threads, settings.ToArray());
        var run = JsonSerializer.Deserialize<WindowRun>(StudyProcesses.SingleLine(lines, "WINDOW_JSON,"))
            ?? throw new InvalidDataException($"Missing benchmark data for {name}.");
        var expectedSamples = mode switch { "Timing" => Thresholds.SamplesPerProcess, "Mixed" => 1, _ => 0 };
        var actual = (run.CrossRollBatching, run.ReuseReplyBoard, run.ReuseFeatureBuffers);
        var expected = (configuration.CrossRoll, configuration.ReplyBoard, configuration.FeatureBuffers);
        if (windows)
        {
            // The window-only study inherits the workload switches, as the original runner did.
            var first = _runs.FirstOrDefault() ?? run;
            expected = (first.CrossRollBatching, first.ReuseReplyBoard, first.ReuseFeatureBuffers);
        }
        if (run.Threads != threads || run.Board != board || run.Window != configuration.Window || run.Mode != mode
            || actual != expected || run.Samples.Length != expectedSamples
            || run.Samples.Any(sample => !double.IsFinite(sample.WallMs) || sample.WallMs <= 0)
            || (_runs.Count > 0 && run.InteropThreads != _runs[0].InteropThreads))
        {
            throw new InvalidDataException($"Unexpected configuration or timing data for {name}.");
        }
        if (mode == "Mixed" && (run.Probes.Length == 0 || (!windows && run.Probes.Length != 100)
            || run.Probes.Any(value => !double.IsFinite(value) || value < 0)))
        {
            throw new InvalidDataException($"Invalid interactive probe data for {name}.");
        }
        _runs.Add(run with { Round = round, ProcessName = name, Configuration = configuration.Name });
        processes.SaveJson("runs.json", _runs);
    }

    private List<ScoreCheck> CheckScores()
    {
        var checks = new List<ScoreCheck>();
        foreach (var group in _runs.GroupBy(run => run.Board))
        {
            var reference = group.First();
            var expected = reference.Scores.ToDictionary(row => row.Key, row => row.Score);
            foreach (var run in group)
            {
                var actual = run.Scores.ToDictionary(row => row.Key, row => row.Score);
                if (run.Selected != reference.Selected || run.Rows != reference.Rows || run.Candidates != reference.Candidates
                    || actual.Count == 0 || actual.Count != run.Candidates || actual.Count != expected.Count
                    || !actual.ContainsKey(run.Selected) || expected.Keys.Any(key => !actual.ContainsKey(key)))
                {
                    throw new InvalidDataException($"Coverage or selected move changed in {run.ProcessName}.");
                }
                var differences = expected.Select(row => Math.Abs(row.Value - actual[row.Key])).ToArray();
                if (differences.Any(delta => !double.IsFinite(delta) || delta > 1e-5))
                {
                    throw new InvalidDataException($"Score tolerance exceeded in {run.ProcessName}.");
                }
                checks.Add(new ScoreCheck(run.ProcessName, differences.Max()));
            }
        }
        return checks;
    }

    private Comparison Compare(Configuration baseline, Configuration candidate)
    {
        var reasons = new List<string>();
        var boards = new List<BoardComparison>();
        foreach (var board in Boards)
        {
            var before = _runs.Where(run => run.Board == board && run.Mode == "Timing" && run.Configuration == baseline.Name).ToArray();
            var after = _runs.Where(run => run.Board == board && run.Mode == "Timing" && run.Configuration == candidate.Name).ToArray();
            var beforeMs = MedianWall(before);
            var afterMs = MedianWall(after);
            var wins = Enumerable.Range(1, Thresholds.Rounds).Count(round =>
                MedianWall(after.Where(run => run.Round == round)) <= MedianWall(before.Where(run => run.Round == round)) * (1 - Thresholds.MinimumGain));
            var allocation = MedianAllocation(after) / MedianAllocation(before);
            if (afterMs > beforeMs * (1 - Thresholds.MinimumGain) || wins < Thresholds.MinimumWinningRounds)
            {
                reasons.Add($"Board {board} lacks a repeatable 5% gain.");
            }
            if (allocation > Thresholds.MaximumAllocationRatio)
            {
                reasons.Add($"Board {board} allocation exceeds gate.");
            }
            if (StudyProcesses.Median(after.Select(run => (double)run.PrivateBytes)) >
                StudyProcesses.Median(before.Select(run => (double)run.PrivateBytes)) * Thresholds.MaximumPrivateRatio + Thresholds.PrivateAllowanceBytes)
            {
                reasons.Add($"Board {board} private memory exceeds gate.");
            }
            boards.Add(new BoardComparison(board, beforeMs, afterMs, 1 - afterMs / beforeMs, wins, allocation,
                StudyProcesses.Percentile(Walls(before), 0.95), StudyProcesses.Percentile(Walls(after), 0.95)));
        }

        var mixedBefore = _runs.Where(run => run.Mode == "Mixed" && run.Configuration == baseline.Name).ToArray();
        var mixedAfter = _runs.Where(run => run.Mode == "Mixed" && run.Configuration == candidate.Name).ToArray();
        var beforeProbes = mixedBefore.SelectMany(run => run.Probes).ToArray();
        var afterProbes = mixedAfter.SelectMany(run => run.Probes).ToArray();
        var p95Before = StudyProcesses.Percentile(beforeProbes, 0.95);
        var p95After = StudyProcesses.Percentile(afterProbes, 0.95);
        var worstBefore = beforeProbes.Max();
        var worstAfter = afterProbes.Max();
        var throughput = MedianWall(mixedBefore) / MedianWall(mixedAfter);
        if (p95After > p95Before + Math.Max(Thresholds.P95AllowanceMs, p95Before * (Thresholds.MaximumP95Ratio - 1)))
        {
            reasons.Add("Interactive p95 exceeds fairness gate.");
        }
        if (worstAfter > worstBefore + Math.Max(Thresholds.WorstAllowanceMs, worstBefore * (Thresholds.MaximumWorstRatio - 1)))
        {
            reasons.Add("Worst interactive probe exceeds fairness gate.");
        }
        if (throughput < Thresholds.MinimumMixedThroughputRatio)
        {
            reasons.Add("Mixed throughput exceeds regression gate.");
        }
        return new Comparison(baseline.Name, candidate.Name, reasons.Count == 0, reasons, boards, p95Before, p95After, worstBefore, worstAfter, throughput);
    }

    private static IEnumerable<double> Walls(IEnumerable<WindowRun> runs) => runs.SelectMany(run => run.Samples.Select(sample => sample.WallMs));
    private static double MedianWall(IEnumerable<WindowRun> runs) => StudyProcesses.Median(Walls(runs));
    private static double MedianAllocation(IEnumerable<WindowRun> runs) => StudyProcesses.Median(runs.SelectMany(run => run.Samples.Select(sample => (double)sample.AllocatedBytes)));

    private sealed record Configuration(string Name, int Window = 1, bool CrossRoll = true, bool ReplyBoard = false, bool FeatureBuffers = false);
    private sealed record StudyThresholds(double MinimumGain = 0.05, int MinimumWinningRounds = 2, double MaximumAllocationRatio = 1.10,
        double MaximumPrivateRatio = 1.20, long PrivateAllowanceBytes = 32 * 1024 * 1024, double MaximumP95Ratio = 1.10,
        double P95AllowanceMs = 0.10, double MaximumWorstRatio = 1.25, double WorstAllowanceMs = 0.50,
        double MinimumMixedThroughputRatio = 0.97, int Rounds = 3, int SamplesPerProcess = 3);
    private sealed record WindowRun(int Board, int Window, string Mode, bool CrossRollBatching, bool ReuseReplyBoard, bool ReuseFeatureBuffers,
        int Threads, int InteropThreads, long Rows, int Candidates, Sample[] Samples, double[] Probes, JsonElement Profile,
        long PrivateBytes, long WorkingSet, long PeakWorkingSet, long ManagedLive, CandidateScore[] Scores, string Selected, double MaximumScoreDifference)
    {
        public int Round { get; init; }
        public string ProcessName { get; init; } = "";
        public string Configuration { get; init; } = "";
    }
    private sealed record Sample(double WallMs, double CpuMs, long AllocatedBytes, int Gen0, int Gen1, int Gen2, long Predictions);
    private sealed record CandidateScore(string Key, double Score);
    private sealed record ScoreCheck(string Process, double MaximumScoreDifference);
    private sealed record BoardComparison(int Board, double BeforeMs, double AfterMs, double Gain, int WinningRounds, double AllocationRatio,
        double BeforeP95Ms, double AfterP95Ms);
    private sealed record Comparison(string Baseline, string Candidate, bool Accepted, List<string> Reasons, List<BoardComparison> Boards,
        double BeforeProbeP95Ms, double AfterProbeP95Ms, double BeforeWorstProbeMs, double AfterWorstProbeMs, double MixedThroughputRatio);
}
