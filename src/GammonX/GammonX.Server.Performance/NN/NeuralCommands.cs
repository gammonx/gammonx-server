using System.Globalization;
using System.Reflection;

using GammonX.Models.Enums;

namespace GammonX.Server.Performance.NN;

internal static class NeuralCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help")
        {
            Console.WriteLine("""
                nn engine|profile|input-reuse [--board 1|2|3]
                nn eval [--board 1|2|3] [--level Hard|Expert]
                nn inference|cpu-case|window-case
                nn cpu-tuning --baseline-threads N [--output-dir PATH]
                nn candidate-windows [--threads N] [--output-dir PATH]
                nn roll-batching [--threads N] [--optimization roll-batching|reply-board|feature-buffers|per-roll-reuse] [--quick] [--output-dir PATH]
                """);
            return 0;
        }

        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += handler;
        try
        {
            if (Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration != "Release")
            {
                throw new InvalidOperationException("Run neural benchmarks with -c Release.");
            }

            var command = args[0];
            var options = ParseOptions(args[1..], command);
            var benchmark = new NeuralBenchmarks(Console.Out, cancellation.Token);
            var board = Positive(options, "--board", 1);
            var roll1 = board == 2 ? 3 : 2;
            var level = options.GetValueOrDefault("--level", "Expert") switch
            {
                "Hard" => BotLevel.Hard,
                "Expert" => BotLevel.Expert,
                _ => throw new ArgumentException("--level must be Hard or Expert.")
            };
            await (command switch
            {
                "engine" or "eval" => benchmark.MeasureBoard(board, roll1, level, command == "engine"),
                "profile" => benchmark.ProfileTwoPlyPerformance(board, roll1, 2),
                "input-reuse" => benchmark.MeasureIdenticalInputs(board),
                "inference" => benchmark.MeasureSharedWorkerInference(),
                "cpu-case" => benchmark.MeasureCpuThreadAndBatchTuning(),
                "window-case" => benchmark.MeasureCandidateWindows(),
                "cpu-tuning" => new CpuTuningStudy(CreateStudy(command, options, cancellation.Token)).RunAsync(Positive(options, "--baseline-threads")),
                "candidate-windows" or "roll-batching" => new CandidateWindowStudy(CreateStudy(command, options, cancellation.Token),
                    Positive(options, "--threads", 16)).RunAsync(command, options.GetValueOrDefault("--optimization", "roll-batching"), options.ContainsKey("--quick")),
                _ => throw new ArgumentException($"Unknown neural benchmark: {command}")
            });
            return 0;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return 130;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    private static StudyProcesses CreateStudy(string command, Dictionary<string, string> options, CancellationToken token) =>
        new(options.GetValueOrDefault("--output-dir", Path.Combine("out", "performance", $"{command}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fffffff}")), token);

    private static int Positive(Dictionary<string, string> options, string key, int? fallback = null)
    {
        if (!options.TryGetValue(key, out var value) && fallback.HasValue)
        {
            return fallback.Value;
        }
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number <= 0)
        {
            throw new ArgumentException($"{key} requires a positive integer.");
        }
        return number;
    }

    private static Dictionary<string, string> ParseOptions(string[] args, string command)
    {
        string[] supported = command switch
        {
            "engine" or "profile" or "input-reuse" => ["--board"],
            "eval" => ["--board", "--level"],
            "cpu-tuning" => ["--baseline-threads", "--output-dir"],
            "candidate-windows" => ["--threads", "--output-dir"],
            "roll-batching" => ["--threads", "--output-dir", "--quick", "--optimization"],
            _ => []
        };
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            var option = args[i];
            if (!supported.Contains(option) || options.ContainsKey(option))
            {
                throw new ArgumentException($"Unexpected or repeated option: {option}");
            }
            if (option == "--quick")
            {
                options.Add(option, "true");
                continue;
            }
            if (++i == args.Length || args[i].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Missing value for {option}.");
            }
            options.Add(option, args[i]);
        }
        return options;
    }
}
