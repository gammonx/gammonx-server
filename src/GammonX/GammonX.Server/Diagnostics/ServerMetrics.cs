using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GammonX.Server.Diagnostics;

/// <summary>
/// Low-cardinality game-server measurements. Subscribe to the meter to enable histograms.
/// Durations are inclusive elapsed time, not CPU time; overlapping stages must not be summed.
/// </summary>
internal static class ServerMetrics
{
    internal const string MeterName = "GammonX.Server";

    private static readonly Meter Meter = new(MeterName);
    internal static readonly Histogram<double> CommandDuration = Meter.CreateHistogram<double>("gammonx.server.command.duration", "ms");
    internal static readonly Histogram<double> MoveGenerationDuration = Meter.CreateHistogram<double>("gammonx.server.moves.duration", "ms");
    internal static readonly Histogram<long> SequenceCount = Meter.CreateHistogram<long>("gammonx.server.moves.sequences", "{sequence}");
    internal static readonly Histogram<double> LeaseWait = Meter.CreateHistogram<double>("gammonx.server.lease.wait", "ms");
    internal static readonly Histogram<double> LeaseHold = Meter.CreateHistogram<double>("gammonx.server.lease.hold", "ms");
    internal static readonly Histogram<double> ExternalDuration = Meter.CreateHistogram<double>("gammonx.server.external.duration", "ms");
    internal static readonly Histogram<long> MessageBytes = Meter.CreateHistogram<long>("gammonx.server.message.serialized_size", "By");
    internal static readonly Counter<long> BotFallbacks = Meter.CreateCounter<long>("gammonx.server.bot.fallbacks", "{fallback}");
    internal static readonly Counter<long> BotRecoveries = Meter.CreateCounter<long>("gammonx.server.bot.recoveries", "{attempt}");

    private static long _timerTasks;
    private static long _timerCallbacks;
    internal static long TimerTasks => Interlocked.Read(ref _timerTasks);
    internal static long TimerCallbacks => Interlocked.Read(ref _timerCallbacks);
    internal static void TimerTaskStarted() => Interlocked.Increment(ref _timerTasks);
    internal static void TimerTaskFinished() => Interlocked.Decrement(ref _timerTasks);

    internal readonly struct CallbackScope : IDisposable
    {
        public CallbackScope() => Interlocked.Increment(ref _timerCallbacks);
        public void Dispose() => Interlocked.Decrement(ref _timerCallbacks);
    }

    internal static DurationScope Measure(Histogram<double> histogram, string? operation = null) => new(histogram, operation);

    internal readonly struct DurationScope : IDisposable
    {
        private readonly Histogram<double>? _histogram;
        private readonly string? _operation;
        private readonly long _start;

        internal DurationScope(Histogram<double> histogram, string? operation)
        {
            _histogram = histogram.Enabled ? histogram : null;
            _operation = operation;
            _start = _histogram != null ? Stopwatch.GetTimestamp() : 0;
        }

        public void Dispose()
        {
            if (_histogram == null)
            {
                return;
            }

            var elapsed = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
            if (_operation == null)
            {
                _histogram.Record(elapsed);
            }
            else
            {
                _histogram.Record(elapsed, new KeyValuePair<string, object?>("operation", _operation));
            }
        }
    }
}
