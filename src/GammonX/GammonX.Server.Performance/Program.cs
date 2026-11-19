using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Text.Json;

using GammonX.Engine.Services;
using GammonX.Models.Enums;
using GammonX.Diagnostics.Server;
using GammonX.Server.Performance;
using GammonX.Server.Performance.NN;

// This executable intentionally never starts the web host or reads application configuration.
if (args.FirstOrDefault() == "nn")
{
    return await NeuralCommands.RunAsync(args[1..]);
}

var supportedOptions = new HashSet<string>(["--output", "--iterations", "--warmup", "--telemetry", "--compare", "--cpu-limit", "--memory-limit-mb"]);
var options = new Dictionary<string, string>(StringComparer.Ordinal);
// we parse the cli arguments
for (var i = 0; i < args.Length; i += 2)
{
    if (i + 1 == args.Length || !supportedOptions.Contains(args[i]))
    {
        throw new ArgumentException("Use --output path --iterations 30 --warmup 5 --telemetry off|on --compare path (optional) --cpu-limit value --memory-limit-mb value.");
    }
    options.Add(args[i], args[i + 1]);
}
// we set the iteration count and warmup phase
var iterations = int.Parse(options.GetValueOrDefault("--iterations", "30"), CultureInfo.InvariantCulture);
var warmup = int.Parse(options.GetValueOrDefault("--warmup", "5"), CultureInfo.InvariantCulture);
if (iterations < 1 || warmup < 1)
{
    throw new ArgumentOutOfRangeException(nameof(args), "Iterations and warmup must be positive.");
}
// we turn the telemetry on/off
var telemetry = options.GetValueOrDefault("--telemetry", "off");
if (telemetry is not ("on" or "off"))
{
    throw new ArgumentException("Telemetry must be on or off.");
}
// we set the output path
var outputPath = Path.GetFullPath(options.GetValueOrDefault("--output", "baseline.json"));
if (options.TryGetValue("--compare", out var comparisonPath)
    && string.Equals(Path.GetFullPath(comparisonPath), outputPath, StringComparison.OrdinalIgnoreCase))
{
    throw new ArgumentException("The baseline and output paths must differ.");
}
// we enforce a release build
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
var configuration = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
if (configuration != "Release")
{
    throw new InvalidOperationException("Run this benchmark with -c Release.");
}
// we attach the meter listener
using var listener = new MeterListener();
long measurements = 0;
if (telemetry == "on")
{
    listener.InstrumentPublished = (instrument, meterListener) =>
    {
        if (instrument.Meter.Name == ServerMetrics.MeterName)
        {
            meterListener.EnableMeasurementEvents(instrument);
        }
    };
    listener.SetMeasurementEventCallback<double>((_, _, _, _) => Interlocked.Increment(ref measurements));
    listener.SetMeasurementEventCallback<long>((_, _, _, _) => Interlocked.Increment(ref measurements));
    listener.Start();
}
// we fetch the test fixtures
var fixtures = new List<RunSimulationModel>();
foreach (var variant in new[] { GameModus.Backgammon, GameModus.Portes, GameModus.Plakoto, GameModus.Fevga, GameModus.Tavla })
{
    fixtures.Add(new RunSimulationModel($"{variant}-opening-12", variant, 1, 2));
    fixtures.Add(new RunSimulationModel($"{variant}-opening-11", variant, 1, 1));
}
fixtures.Add(new RunSimulationModel("Backgammon-spread-11", GameModus.Backgammon, 1, 1, Spread: true));
fixtures.Add(new RunSimulationModel("Backgammon-history1000-12", GameModus.Backgammon, 1, 2, HistoryEvents: 1000));

// we initialize some metrics counter
var results = new List<Sample>();
using var process = Process.GetCurrentProcess();
var runStart = Stopwatch.GetTimestamp();
var cpuStart = process.TotalProcessorTime;
var allocationStart = GC.GetTotalAllocatedBytes(true);
var pauseStart = GC.GetTotalPauseDuration();
var collectionsStart = Enumerable.Range(0, 3).Select(GC.CollectionCount).ToArray();
long exceptions = 0;
long peakQueue = 0;
AppDomain.CurrentDomain.FirstChanceException += (_, _) => Interlocked.Increment(ref exceptions);
using var sampler = new Timer(_ =>
{
    var pending = ThreadPool.PendingWorkItemCount;
    Interlocked.Exchange(ref peakQueue, Math.Max(Interlocked.Read(ref peakQueue), pending));
}, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

// we run the different test fixtures
foreach (var fixture in fixtures)
{
    var boardService = BoardServiceFactory.Create(fixture.Variant);
    var unrolled = fixture.CreateGame(false);
    var game = fixture.CreateGame(true);
    var rolls = fixture.Rolls;
    var payload = game.ToPayload(RunSimulationModel.Player, [], false);
    var protocol = RunSimulationModel.CreateProtocol();
    var message = RunSimulationModel.Message(payload);
    var wireBytes = protocol.GetMessageBytes(message).Length;

    RunSimulation("generate", () => () => GC.KeepAlive(boardService.GetLegalMoveSequences(unrolled.BoardModel, true, rolls)));
    RunSimulation("clone", () => () => GC.KeepAlive(((ICloneable)game).Clone()));
    RunSimulation("payload", () => () => GC.KeepAlive(game.ToPayload(RunSimulationModel.Player, [], false)));
    RunSimulation("serialize", () =>
    {
        // Allocate a fresh buffer per operation; include buffer growth, never reuse its capacity.
        return () =>
        {
            var writer = new ArrayBufferWriter<byte>();
            protocol.WriteMessage(message, writer);
            GC.KeepAlive(writer);
        };
    });
    if (fixture.Variant == GameModus.Backgammon)
    {
        RunSimulation("repository-turn", fixture.PrepareTurn);
    }

    void RunSimulation(string stage, Func<Action> prepare)
    {
        for (var i = 0; i < warmup; i++)
        {
            prepare()();
        }

        var times = new double[iterations];
        long bytes = 0;

        for (var i = 0; i < iterations; i++)
        {
            var action = prepare(); // Fixture/reset costs are outside each timed/allocated sample.
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            var started = Stopwatch.GetTimestamp();
            action();
            times[i] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            bytes += GC.GetAllocatedBytesForCurrentThread() - allocated;
        }

        Array.Sort(times);
        var sample = new Sample(fixture.Name, fixture.Variant.ToString(), rolls, game.BoardModel.History.Events.Count,
            game.MoveSequences.Count, stage, iterations, warmup, times.Average(), times[(int)Math.Ceiling(iterations * 0.95) - 1],
            (double)bytes / iterations, wireBytes);
        results.Add(sample);
        
        Console.WriteLine(FormattableString.Invariant($"{sample.Fixture,-30} {stage,-16} {sample.MeanMs,10:F3} ms {sample.AllocatedBytes,14:F0} B/op; sequences={sample.Sequences}; wire={wireBytes}"));
    }
}

sampler.Change(Timeout.Infinite, Timeout.Infinite);
var elapsed = Stopwatch.GetElapsedTime(runStart).TotalSeconds;
var cpuSeconds = (process.TotalProcessorTime - cpuStart).TotalSeconds;
var allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocationStart;
var pauseMs = (GC.GetTotalPauseDuration() - pauseStart).TotalMilliseconds;

var runtime = new
{
    ElapsedSeconds = elapsed,
    CpuSeconds = cpuSeconds,
    AverageCpuCores = cpuSeconds / elapsed,
    AllocatedBytes = allocatedBytes,
    AllocationBytesPerSecond = allocatedBytes / elapsed,
    GcPauseMs = pauseMs,
    GcPausePercent = pauseMs / (elapsed * 1000) * 100,
    GcCollections = Enumerable.Range(0, 3).Select(i => GC.CollectionCount(i) - collectionsStart[i]).ToArray(),
    Exceptions = Interlocked.Read(ref exceptions),
    PeakThreadPoolQueueSampled = Interlocked.Read(ref peakQueue),
    MetricMeasurements = Interlocked.Read(ref measurements)
};

var metadata = new
{
    SchemaVersion = 1,
    FixtureVersion = 1,
    RecordedUtc = DateTime.UtcNow,
    Runtime = RuntimeInformation.FrameworkDescription,
    OS = RuntimeInformation.OSDescription,
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    LogicalProcessors = Environment.ProcessorCount,
    CpuLimit = options.GetValueOrDefault("--cpu-limit", "unspecified"),
    MemoryLimitMb = options.GetValueOrDefault("--memory-limit-mb", "unspecified"),
    Configuration = configuration,
    Telemetry = telemetry,
    ServerGc = GCSettings.IsServerGC,
    TieredCompilation = AppContext.GetData("System.Runtime.TieredCompilation")?.ToString(),
    ServerAssembly = typeof(GammonX.Server.MatchLobbyHub).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
    ServerModuleId = typeof(GammonX.Server.MatchLobbyHub).Module.ModuleVersionId,
    EngineModuleId = typeof(BoardServiceFactory).Module.ModuleVersionId,
    BenchmarkModuleId = Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId,
    Note = "Local synchronous component benchmark; no host, transport, AWS or Mars. Runtime totals include preparation/warmup/harness; stage samples exclude preparation."
};
// we prepare the sandbox
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllText(outputPath, JsonSerializer.Serialize(new { Metadata = metadata, Runtime = runtime, Samples = results }, jsonOptions));

Console.WriteLine($"Saved {outputPath}");
Console.WriteLine(JsonSerializer.Serialize(runtime, jsonOptions));

if (options.TryGetValue("--compare", out var baselinePath))
{
    using var baseline = JsonDocument.Parse(File.ReadAllText(baselinePath));
    var prior = baseline.RootElement.GetProperty("Samples").Deserialize<Sample[]>()!;
    var priorMetadata = baseline.RootElement.GetProperty("Metadata");
    if (priorMetadata.GetProperty("FixtureVersion").GetInt32() != metadata.FixtureVersion || prior.Length != results.Count)
    {
        throw new InvalidOperationException("Fixture sets differ; comparison is not valid.");
    }
    var comparisons = results.Select(current =>
    {
        var previous = prior.Single(sample => sample.Fixture == current.Fixture && sample.Stage == current.Stage);
        if (previous.Sequences != current.Sequences || previous.HistoryEvents != current.HistoryEvents
            || !previous.Dice.SequenceEqual(current.Dice) || previous.Variant != current.Variant
            || previous.Iterations != current.Iterations || previous.Warmup != current.Warmup)
        {
            throw new InvalidOperationException($"Fixture behavior changed: {current.Fixture}/{current.Stage}. Review before comparing.");
        }
        return new
        {
            current.Fixture, current.Stage,
            BaselineMs = previous.MeanMs, CurrentMs = current.MeanMs,
            TimeChangePercent = Percent(previous.MeanMs, current.MeanMs),
            BaselineBytes = previous.AllocatedBytes, CurrentBytes = current.AllocatedBytes,
            AllocationChangePercent = Percent(previous.AllocatedBytes, current.AllocatedBytes),
            BaselineWireBytes = previous.WireBytes, CurrentWireBytes = current.WireBytes,
            WireSizeChangePercent = Percent(previous.WireBytes, current.WireBytes)
        };
    }).ToArray();
    File.WriteAllText(outputPath + ".comparison.json", JsonSerializer.Serialize(new
    {
        BaselineMetadata = priorMetadata,
        CurrentMetadata = metadata,
        Note = "Compare metadata and iteration counts before interpreting timing; positive change is an increase.",
        Comparisons = comparisons
    }, jsonOptions));
}

return 0;

static double? Percent(double before, double after) => before == 0 ? null : (after / before - 1) * 100;

internal sealed record Sample(
    string Fixture, 
    string Variant, 
    int[] Dice, 
    int HistoryEvents, 
    int Sequences, 
    string Stage,
    int Iterations, 
    int Warmup, 
    double MeanMs, 
    double P95Ms, 
    double AllocatedBytes, 
    int WireBytes
);
