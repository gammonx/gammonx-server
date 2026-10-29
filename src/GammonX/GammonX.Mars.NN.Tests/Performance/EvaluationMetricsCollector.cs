using GammonX.Engine.Diagnostics;

using System.Diagnostics.Metrics;

namespace GammonX.Mars.NN.Tests.Performance;

internal sealed class EvaluationMetricsCollector : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, StageTotals> _stages = new(StringComparer.Ordinal);
    private readonly Dictionary<long, long> _batchSizes = new();
    private readonly MeterListener _listener = new();
    private long _rawSequences;
    private long _uniqueSequences;
    private long _rawMeasurements;
    private long _uniqueMeasurements;
    private long _reusedStates;
    private long _inferenceRequests;

    public EvaluationMetricsCollector()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == EvaluationMetrics.MeterName)
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<double>((_, value, tags, _) =>
        {
            lock (_gate)
            {
                var stage = GetStage(GetTag(tags, "stage"));
                stage.Calls++;
                stage.TotalMs += value;
            }
        });
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            lock (_gate)
            {
                switch (instrument.Name)
                {
                    case EvaluationMetrics.AllocationName:
                        var stage = GetStage(GetTag(tags, "stage"));
                        stage.AllocatedBytes += value;
                        stage.AllocationSamples++;
                        break;
                    case EvaluationMetrics.BatchSizeName:
                        _batchSizes[value] = _batchSizes.GetValueOrDefault(value) + 1;
                        break;
                    case EvaluationMetrics.ReusedStateCountName:
                        _reusedStates += value;
                        break;
                    case EvaluationMetrics.InferenceRequestCountName:
                        _inferenceRequests += value;
                        break;
                    case EvaluationMetrics.SequenceCountName:
                        switch (GetTag(tags, "kind"))
                        {
                            case "raw":
                                _rawSequences += value;
                                _rawMeasurements++;
                                break;
                            case "unique":
                                _uniqueSequences += value;
                                _uniqueMeasurements++;
                                break;
                            default:
                                throw new InvalidOperationException("Unknown sequence metric kind.");
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown integer evaluation metric: {instrument.Name}");
                }
            }
        });
        _listener.Start();
    }

    public EvaluationProfile Snapshot()
    {
        lock (_gate)
        {
            return new EvaluationProfile(
                _stages.OrderBy(pair => pair.Key).Select(pair => new StageMeasurement(
                    pair.Key,
                    pair.Value.Calls,
                    pair.Value.TotalMs,
                    pair.Value.AllocationSamples == 0 ? null : pair.Value.AllocatedBytes,
                    pair.Value.AllocationSamples)).ToArray(),
                _batchSizes.OrderBy(pair => pair.Key).ToArray(),
                _rawSequences,
                _uniqueSequences,
                _rawMeasurements,
                _uniqueMeasurements,
                _reusedStates,
                _inferenceRequests);
        }
    }

    public void Dispose() => _listener.Dispose();

    private StageTotals GetStage(string name)
    {
        if (!_stages.TryGetValue(name, out var totals))
        {
            totals = new StageTotals();
            _stages.Add(name, totals);
        }
        return totals;
    }

    private static string GetTag(ReadOnlySpan<KeyValuePair<string, object?>> tags, string name)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == name && tag.Value is string value)
                return value;
        }
        throw new InvalidOperationException($"Missing evaluation metric tag: {name}");
    }

    private sealed class StageTotals
    {
        public long Calls;
        public double TotalMs;
        public long AllocatedBytes;
        public long AllocationSamples;
    }
}

internal sealed record StageMeasurement(string Stage, long Calls, double TotalMs, long? AllocatedBytes, long AllocationSamples);

internal sealed record EvaluationProfile(
    StageMeasurement[] Stages,
    KeyValuePair<long, long>[] BatchSizes,
    long RawSequences,
    long UniqueSequences,
    long RawMeasurements,
    long UniqueMeasurements,
    long ReusedStates,
    long InferenceRequests)
{
    public StageMeasurement GetStage(string stage) => Stages.Single(measurement => measurement.Stage == stage);
}
