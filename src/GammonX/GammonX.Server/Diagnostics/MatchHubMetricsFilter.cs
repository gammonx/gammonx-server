using Microsoft.AspNetCore.SignalR;

namespace GammonX.Server.Diagnostics;

internal sealed class MatchHubMetricsFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        // Only resolved method names are used, never client-supplied arguments or IDs.
        using var measurement = ServerMetrics.Measure(ServerMetrics.CommandDuration, invocationContext.HubMethod.Name);
        return await next(invocationContext);
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        using var measurement = ServerMetrics.Measure(ServerMetrics.CommandDuration, "connect");
        await next(context);
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        using var measurement = ServerMetrics.Measure(ServerMetrics.CommandDuration, "disconnect");
        await next(context, exception);
    }
}
