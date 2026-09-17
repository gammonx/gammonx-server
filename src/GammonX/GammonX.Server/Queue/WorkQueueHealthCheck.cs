using GammonX.Models.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GammonX.Server.Queue
{
    // <inheritdoc />
    public sealed class WorkQueueHealthCheck : IHealthCheck
    {
        private static readonly WorkQueueType[] QueueTypes = Enum.GetValues<WorkQueueType>();

        private readonly IServiceProvider _services;

        public WorkQueueHealthCheck(IServiceProvider services)
        {
            _services = services;
        }

        // <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            // we get all queues which are required in order to run the game server
            var keyedQueues = QueueTypes
                .Select(queueType =>
                    (Name: queueType.ToString(), Queue: _services.GetKeyedService<IWorkQueue>(queueType)))
                .ToArray();

            if (keyedQueues.All(item => item.Queue == null))
            {
                var fallbackQueue = _services.GetService<IWorkQueue>();
                if (fallbackQueue == null)
                {
                    return HealthCheckResult.Unhealthy("No work queue is registered.");
                }

                return await CheckQueueAsync("default", fallbackQueue, cancellationToken);
            }

            var missingQueues = keyedQueues
                .Where(item => item.Queue == null)
                .Select(item => item.Name)
                .ToArray();
            
            if (missingQueues.Length > 0)
            {
                // We report each queue that is missing from the service provider
                return HealthCheckResult.Unhealthy(
                    $"Work queue registrations are missing: {string.Join(", ", missingQueues)}.");
            }

            var unavailableQueues = new List<string>();

            foreach (var (name, queue) in keyedQueues)
            {
                // We check the health of each available queue
                if (!await queue!.IsHealthyAsync(cancellationToken))
                {
                    unavailableQueues.Add(name);
                }
            }

            return unavailableQueues.Count == 0
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy(
                    $"Work queues are unavailable: {string.Join(", ", unavailableQueues)}.");
        }

        private static async Task<HealthCheckResult> CheckQueueAsync(
            string name,
            IWorkQueue queue,
            CancellationToken cancellationToken)
        {
            return await queue.IsHealthyAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Work queue '{name}' is unavailable.");
        }
    }
}