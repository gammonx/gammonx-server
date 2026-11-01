using GammonX.Mars.NN.Services;
using GammonX.Engine.Diagnostics;
using System.Diagnostics.Metrics;
using static TorchSharp.torch;

namespace GammonX.Mars.NN.Tests.Performance;

public partial class PerformanceTests
{
    private void ConfigureRollBatching(IFeatureEvalService service)
    {
        var configured = Assert.IsAssignableFrom<BaseFeatureEvalServiceImpl>(service);
        var setting = Environment.GetEnvironmentVariable("GAMMONX_PERF_CROSS_ROLL_BATCHING");
        if (setting != null)
        {
            Assert.Contains(setting, new[] { "0", "1" });
            configured.BatchAcrossRolls = setting == "1";
        }
        output.WriteLine($"CrossRollBatching={configured.BatchAcrossRolls}");
    }

    private sealed class WorkerThreadVerifier : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly int _expected;
        private long _batches;
        private int _mismatch = -1;

        public WorkerThreadVerifier(int expected)
        {
            _expected = expected;
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == EvaluationMetrics.MeterName && instrument.Name == EvaluationMetrics.BatchSizeName)
                    listener.EnableMeasurementEvents(instrument);
            };
            _listener.SetMeasurementEventCallback<long>((_, _, _, _) =>
            {
                var observed = get_num_threads();
                if (observed != _expected)
                    Interlocked.Exchange(ref _mismatch, observed);
                Interlocked.Increment(ref _batches);
            });
            _listener.Start();
        }

        public void Verify()
        {
            Assert.True(Interlocked.Read(ref _batches) > 0);
            Assert.True(_mismatch == -1, $"Expected {_expected} Torch threads on every warm-up worker batch; observed {_mismatch}.");
        }

        public void Dispose() => _listener.Dispose();
    }
}
