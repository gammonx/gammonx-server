using System.Diagnostics.Metrics;

using GammonX.Models.Enums;
using GammonX.Server.Services;

namespace GammonX.Server.Diagnostics;

/// <summary>
/// Hosts observable counts so a listener attaching after startup sees current state.
/// This meter is disposed with the host and cannot retain repositories after host shutdown.
/// </summary>
internal sealed class ServerStateMetrics : IHostedService, IDisposable
{
    private readonly Meter _meter = new(ServerMetrics.MeterName);

    public ServerStateMetrics(MatchSessionRepository matches, ICancellationTokenService timers, IServiceProvider services)
    {
        _meter.CreateObservableGauge("gammonx.server.matches.active", () => matches.Count, "{match}");
        var registry = (CancellationTokenServiceImpl)timers;
        _meter.CreateObservableGauge("gammonx.server.deadlines.registered", () => registry.Count, "{deadline}");
        _meter.CreateObservableGauge("gammonx.server.timer.tasks", () => ServerMetrics.TimerTasks, "{task}");
        _meter.CreateObservableGauge("gammonx.server.timer.callbacks.active", () => ServerMetrics.TimerCallbacks, "{callback}");

        var normal = (MatchmakingServiceBaseImpl)services.GetRequiredKeyedService<IMatchmakingService>(MatchModus.Normal);
        var ranked = (MatchmakingServiceBaseImpl)services.GetRequiredKeyedService<IMatchmakingService>(MatchModus.Ranked);
        var bot = (MatchmakingServiceBaseImpl)services.GetRequiredKeyedService<IMatchmakingService>(MatchModus.Bot);
        _meter.CreateObservableGauge("gammonx.server.lobbies.pending", () => normal.LobbyCount + ranked.LobbyCount + bot.LobbyCount, "{lobby}");
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public void Dispose() => _meter.Dispose();
}
