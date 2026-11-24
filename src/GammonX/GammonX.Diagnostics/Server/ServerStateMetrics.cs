using System.Diagnostics.Metrics;

using Microsoft.Extensions.Hosting;

namespace GammonX.Diagnostics.Server;

/// <summary>
/// Hosts observable counts so a listener attaching after startup sees current state.
/// This meter is disposed with the host and cannot retain repositories after host shutdown.
/// </summary>
internal sealed class ServerStateMetrics : IHostedService, IDisposable
{
    private readonly Meter _meter = new(ServerMetrics.MeterName);

    public ServerStateMetrics(Func<int> countMatches, Func<int> countDeadlines, Func<int> countLobbies)
    {
        _meter.CreateObservableGauge("gammonx.server.matches.active", countMatches, "{match}");
        _meter.CreateObservableGauge("gammonx.server.deadlines.registered", countDeadlines, "{deadline}");
        _meter.CreateObservableGauge("gammonx.server.timer.tasks", () => ServerMetrics.TimerTasks, "{task}");
        _meter.CreateObservableGauge("gammonx.server.timer.callbacks.active", () => ServerMetrics.TimerCallbacks, "{callback}");

        _meter.CreateObservableGauge("gammonx.server.lobbies.pending", countLobbies, "{lobby}");
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public void Dispose() => _meter.Dispose();
}
