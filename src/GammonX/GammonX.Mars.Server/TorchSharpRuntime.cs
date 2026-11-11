using static TorchSharp.torch;

namespace GammonX.Mars.Server;

internal static class TorchSharpRuntime
{
    public static (int IntraOp, int InterOp) Initialize() => ThreadCounts.Value;

    private static readonly Lazy<(int IntraOp, int InterOp)> ThreadCounts = new(() =>
    {
        // inter-op configuration may only be set once, even when tests start multiple hosts.
        // we match LibTorch's precedence: MKL_NUM_THREADS overrides OMP_NUM_THREADS.
        var intraOp = ReadThreadCount("MKL_NUM_THREADS") ?? ReadThreadCount("OMP_NUM_THREADS") ?? 1;
        var interOp = ReadThreadCount("TORCH_INTEROP_THREADS") ?? 1;

        set_num_threads(intraOp);
        set_num_interop_threads(interOp);

        return (get_num_threads(), get_num_interop_threads());
    });

    private static int? ReadThreadCount(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (value is null)
        {
            return null;
        }

        if (!int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var count) || count <= 0)
        {
            throw new InvalidOperationException($"{name} must be a positive integer.");
        }

        return count;
    }
}
