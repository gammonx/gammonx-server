using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace GammonX.Server.Performance.NN;

internal sealed class StudyProcesses
{
    private readonly CancellationToken _token;
    internal string Output { get; }
    internal IReadOnlyDictionary<string, string> Fingerprints { get; }
    internal string? AssemblyVersion => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    internal StudyProcesses(string output, CancellationToken token)
    {
        Output = Path.GetFullPath(output);
        if (Directory.Exists(Output) && Directory.EnumerateFileSystemEntries(Output).Any())
        {
            throw new IOException($"Use a new or empty results directory: {Output}");
        }
        _token = token;
        Fingerprints = Fingerprint();
        Directory.CreateDirectory(Output);
        Console.WriteLine($"Results directory: {Output}");
    }

    internal async Task<string[]> RunAsync(string name, string command, int threads, params (string Key, string Value)[] settings)
    {
        _token.ThrowIfCancellationRequested();
        VerifyArtifacts();
        var entry = Assembly.GetExecutingAssembly().Location;
        var host = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot resolve the benchmark executable.");
        var start = new ProcessStartInfo(host)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (Path.GetFileNameWithoutExtension(host).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            start.ArgumentList.Add(entry);
        }
        start.ArgumentList.Add("nn");
        start.ArgumentList.Add(command);
        var threadCount = threads.ToString(CultureInfo.InvariantCulture);
        start.Environment["OMP_NUM_THREADS"] = threadCount;
        start.Environment["MKL_NUM_THREADS"] = threadCount;
        start.Environment["GAMMONX_PERF_THREADS"] = threadCount;
        foreach (var (key, value) in settings)
        {
            start.Environment["GAMMONX_PERF_" + key] = value;
        }

        Console.WriteLine($"Running {name}");
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Cannot start {name}.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            await process.WaitForExitAsync(_token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
            await process.WaitForExitAsync();
            throw;
        }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(Output, name + ".log"), await stdout + await stderr);
        }
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{name} exited with code {process.ExitCode}. See its log in {Output}.");
        }
        VerifyArtifacts();
        return (await stdout).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    internal void SaveJson(string name, object value) =>
        File.WriteAllText(Path.Combine(Output, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }) + "\n");

    private void VerifyArtifacts()
    {
        if (!Fingerprint().OrderBy(entry => entry.Key).SequenceEqual(Fingerprints.OrderBy(entry => entry.Key)))
        {
            throw new InvalidOperationException("Benchmark binaries, dependency manifest or model changed during the study.");
        }
    }

    private static Dictionary<string, string> Fingerprint()
    {
        var directory = AppContext.BaseDirectory;
        var model = Path.Combine(directory, "Data", "NeuralNets", "Backgammon", "training_net.dat");
        var paths = Directory.EnumerateFiles(directory, "GammonX*.dll")
            .Concat([Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".deps.json"), model]);
        if (File.Exists(model + ".meta.json"))
        {
            paths = paths.Append(model + ".meta.json");
        }
        return paths.ToDictionary(path => Path.GetRelativePath(directory, path), path =>
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        });
    }

    internal static double Median(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        return (ordered[(ordered.Length - 1) / 2] + ordered[ordered.Length / 2]) / 2;
    }

    internal static double Percentile(IEnumerable<double> values, double quantile)
    {
        var ordered = values.Order().ToArray();
        return ordered[(int)Math.Ceiling(ordered.Length * quantile) - 1];
    }

    internal static string SingleLine(string[] lines, string prefix) =>
        lines.Single(line => line.StartsWith(prefix, StringComparison.Ordinal))[prefix.Length..];
}
