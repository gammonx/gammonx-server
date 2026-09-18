using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GammonX.Server.Bot
{
    // <inheritdoc />
    public sealed class MarsHealthCheck : IHealthCheck
    {
        private readonly IServiceProvider _services;

        public MarsHealthCheck(IServiceProvider services)
        {
            _services = services;
        }

        // <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var marsBotService = _services.GetKeyedService<IBotService>(WellKnownBotServices.Mars);
            if (marsBotService == null)
            {
                return HealthCheckResult.Unhealthy("Mars bot service is not registered.");
            }

            return await marsBotService.IsHealthyAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Mars bot service is unavailable.");
        }
    }
}
