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
        /// <summary>
        /// Meter to subscribe to when profiling evaluation throughput and resource usage.
        /// </summary>
        public const string MeterName = "GammonX.Evaluation";

        /// <summary>
        /// Histogram of elapsed milliseconds, tagged by stage, to locate evaluation bottlenecks.
        /// Includes overlapping queue waits; nested stage durations must not be summed.
        /// </summary>
        public const string DurationName = "gammonx.evaluation.stage.duration";

        /// <summary>
        /// Histogram of managed bytes allocated on the current thread per synchronous stage,
        /// tagged by stage, to identify allocation pressure. Includes allocations in nested stages.
        /// </summary>
        public const string AllocationName = "gammonx.evaluation.stage.allocated";

        /// <summary>
        /// Histogram of move sequences per generation, tagged with kind=raw or kind=unique,
        /// to compare branching volume before and after deduplication.
        /// </summary>
        public const string SequenceCountName = "gammonx.evaluation.sequences";

        /// <summary>
        /// Histogram of feature rows per inference batch to assess batching efficiency.
        /// </summary>
        public const string BatchSizeName = "gammonx.evaluation.batch.size";

        /// <summary>
        /// Counter of reused traversal states to quantify avoided state creation.
        /// </summary>
        public const string ReusedStateCountName = "gammonx.evaluation.traversal.reused";

        /// <summary>
        /// Counter of successfully enqueued inference requests to measure submission volume.
        /// </summary>
        public const string InferenceRequestCountName = "gammonx.evaluation.inference.requests";

        /// <summary>
        /// Up/down counter of active evaluation candidates to track concurrent search work.
        /// </summary>
        public const string ActiveCandidatesName = "gammonx.evaluation.candidates.active";

        /// <summary>
        /// Up/down counter of retained reply boards to track search memory pressure.
        /// </summary>
        public const string ReplyBoardsName = "gammonx.evaluation.replies.retained";

        /// <summary>
        /// Up/down counter of retained feature rows to track feature storage pressure.
        /// </summary>
        public const string FeatureRowsName = "gammonx.evaluation.features.retained";

        /// <summary>
        /// Up/down counter of feature rows awaiting batch processing, including submissions waiting
        /// for channel capacity, to expose inference backlog and back pressure.
        /// </summary>
        public const string QueuedRowsName = "gammonx.evaluation.queue.rows";

        /// <summary>
        /// Histogram of milliseconds spent waiting for work in an empty inference queue,
        /// to identify worker under utilization.
        /// </summary>
        public const string WorkerIdleName = "gammonx.evaluation.worker.empty_wait";

        /// <summary>
        /// Stage covering move generation, including nested traversal and cloning work.
        /// </summary>
        public const string MoveGeneration = "MoveGeneration";

        /// <summary>
        /// Stage isolating board cloning when materializing generated moves.
        /// </summary>
        public const string GenerationClone = "GenerationClone";

        /// <summary>
        /// Stage isolating board snapshots during move traversal.
        /// </summary>
        public const string TraversalSnapshot = "TraversalSnapshot";

        /// <summary>
        /// Stage isolating board cloning for reply evaluation.
        /// </summary>
        public const string ReplyClone = "ReplyClone";

        /// <summary>
        /// Stage measuring feature extraction from a board for evaluation.
        /// </summary>
        public const string Features = "Features";

        /// <summary>
        /// Stage measuring conversion of extracted features into an inference input vector.
        /// </summary>
        public const string FeatureVector = "FeatureVector";

        /// <summary>
        /// Stage measuring input preparation for an inference batch.
        /// </summary>
        public const string BatchPreparation = "BatchPreparation";

        /// <summary>
        /// Stage measuring the neural network forward pass for a batch.
        /// </summary>
        public const string Forward = "Forward";

        /// <summary>
        /// Duration-only stage measuring request enqueue-to-batch latency, including channel back pressure.
        /// Request waits overlap and must not be summed as wall-clock time.
        /// </summary>
        public const string QueueWait = "QueueWait";

        private static readonly Meter Meter = new(MeterName);
        private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(DurationName, "ms");
        private static readonly Histogram<long> Allocation = Meter.CreateHistogram<long>(AllocationName, "By");
        private static readonly Histogram<long> SequenceCount = Meter.CreateHistogram<long>(SequenceCountName);
        private static readonly Histogram<long> BatchSize = Meter.CreateHistogram<long>(BatchSizeName);
        private static readonly Counter<long> ReusedStates = Meter.CreateCounter<long>(ReusedStateCountName);
        private static readonly Counter<long> InferenceRequests = Meter.CreateCounter<long>(InferenceRequestCountName);
        private static readonly UpDownCounter<long> ActiveCandidates = Meter.CreateUpDownCounter<long>(ActiveCandidatesName);
        private static readonly UpDownCounter<long> ReplyBoards = Meter.CreateUpDownCounter<long>(ReplyBoardsName);
        private static readonly UpDownCounter<long> FeatureRows = Meter.CreateUpDownCounter<long>(FeatureRowsName);
        private static readonly UpDownCounter<long> QueuedRows = Meter.CreateUpDownCounter<long>(QueuedRowsName);
        private static readonly Histogram<double> WorkerIdle = Meter.CreateHistogram<double>(WorkerIdleName, "ms");

        public static void RecordActiveCandidates(int delta) => ActiveCandidates.Add(delta);
        public static void RecordReplyBoards(int delta) => ReplyBoards.Add(delta);
        public static void RecordFeatureRows(int delta) => FeatureRows.Add(delta);
        public static void RecordQueuedRows(int delta) => QueuedRows.Add(delta);
        public static long GetWorkerWaitTimestamp() => WorkerIdle.Enabled ? Stopwatch.GetTimestamp() : 0;
        public static void RecordWorkerWait(long started)
        {
            if (started != 0)
                WorkerIdle.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }

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
