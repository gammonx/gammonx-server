using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GammonX.Engine.Diagnostics
{
    /// <summary>
    /// Opt-in evaluation metrics, enabled by subscribing to <see cref="MeterName"/>.
    /// Stage durations and allocations are inclusive; nested stages must not be added together.
    /// </summary>
    public static class EvaluationMetrics
    {
        public const string MeterName = "GammonX.Evaluation";
        public const string DurationName = "gammonx.evaluation.stage.duration";
        public const string AllocationName = "gammonx.evaluation.stage.allocated";
        public const string SequenceCountName = "gammonx.evaluation.sequences";
        public const string BatchSizeName = "gammonx.evaluation.batch.size";
        public const string ReusedStateCountName = "gammonx.evaluation.traversal.reused";
        public const string InferenceRequestCountName = "gammonx.evaluation.inference.requests";

        public const string MoveGeneration = "MoveGeneration";
        public const string GenerationClone = "GenerationClone";
        public const string TraversalSnapshot = "TraversalSnapshot";
        public const string ReplyClone = "ReplyClone";
        public const string Features = "Features";
        public const string FeatureVector = "FeatureVector";
        public const string BatchPreparation = "BatchPreparation";
        public const string Forward = "Forward";
        public const string QueueWait = "QueueWait";

        private static readonly Meter Meter = new(MeterName);
        private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(DurationName, "ms");
        private static readonly Histogram<long> Allocation = Meter.CreateHistogram<long>(AllocationName, "By");
        private static readonly Histogram<long> SequenceCount = Meter.CreateHistogram<long>(SequenceCountName);
        private static readonly Histogram<long> BatchSize = Meter.CreateHistogram<long>(BatchSizeName);
        private static readonly Counter<long> ReusedStates = Meter.CreateCounter<long>(ReusedStateCountName);
        private static readonly Counter<long> InferenceRequests = Meter.CreateCounter<long>(InferenceRequestCountName);

        /// <summary>
        /// Measures synchronous work on one thread. Do not hold this scope across an await.
        /// With no subscribers, no timestamps or allocation counters are read.
        /// </summary>
        public static StageScope Measure(string stage) =>
            Duration.Enabled || Allocation.Enabled ? new StageScope(stage) : default;

        public static void RecordSequences(int raw, int unique)
        {
            SequenceCount.Record(raw, new KeyValuePair<string, object?>("kind", "raw"));
            SequenceCount.Record(unique, new KeyValuePair<string, object?>("kind", "unique"));
        }

        public static void RecordBatchSize(int count) => BatchSize.Record(count);

        public static void RecordReusedState() => ReusedStates.Add(1);

        public static void RecordInferenceRequest() => InferenceRequests.Add(1);

        public static long GetQueueTimestamp() => Duration.Enabled ? Stopwatch.GetTimestamp() : 0;

        /// <summary>
        /// Records enqueue-to-batch latency, including bounded-channel back pressure.
        /// Waits overlap between requests and must not be summed as wall-clock time.
        /// </summary>
        public static void RecordQueueWait(long queued, long batchStarted)
        {
            if (queued != 0)
            {
                Duration.Record(
                    (batchStarted - queued) * 1000d / Stopwatch.Frequency,
                    new KeyValuePair<string, object?>("stage", QueueWait));
            }
        }

        public readonly struct StageScope : IDisposable
        {
            private readonly string? _stage;
            private readonly long _start;
            private readonly long _allocatedBefore;
            private readonly bool _recordDuration;
            private readonly bool _recordAllocation;

            internal StageScope(string stage)
            {
                _stage = stage;
                _recordDuration = Duration.Enabled;
                _recordAllocation = Allocation.Enabled;
                _allocatedBefore = _recordAllocation ? GC.GetAllocatedBytesForCurrentThread() : 0;
                _start = _recordDuration ? Stopwatch.GetTimestamp() : 0;
            }

            public void Dispose()
            {
                if (_stage == null)
                    return;

                var elapsedMs = _recordDuration ? Stopwatch.GetElapsedTime(_start).TotalMilliseconds : 0;
                var allocated = _recordAllocation ? GC.GetAllocatedBytesForCurrentThread() - _allocatedBefore : 0;
                var tag = new KeyValuePair<string, object?>("stage", _stage);
                if (_recordDuration)
                    Duration.Record(elapsedMs, tag);
                if (_recordAllocation)
                    Allocation.Record(allocated, tag);
            }
        }
    }
}
