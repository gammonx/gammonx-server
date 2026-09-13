using GammonX.Models.Enums;
using GammonX.Server.Queue;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Moq;

namespace GammonX.Server.Tests.Queue
{
    public class WorkQueueHealthCheckTests
    {
        [Fact]
        public async Task LoggingQueueModeIsHealthy()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IWorkQueue, LogWorkQueue>();

            var check = new WorkQueueHealthCheck(services.BuildServiceProvider());

            var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task AllConfiguredQueuesMustBeHealthy()
        {
            var queues = Enum.GetValues<WorkQueueType>()
                .ToDictionary(queueType => queueType, _ => new Mock<IWorkQueue>());
            foreach (var queue in queues.Values)
            {
                queue.Setup(value => value.IsHealthyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            }

            queues[WorkQueueType.StatsUpdated]
                .Setup(value => value.IsHealthyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var services = new ServiceCollection();
            foreach (var (queueType, queue) in queues)
            {
                services.AddKeyedSingleton(queueType, queue.Object);
            }

            var check = new WorkQueueHealthCheck(services.BuildServiceProvider());

            var result = await check.CheckHealthAsync(
                new HealthCheckContext(),
                TestContext.Current.CancellationToken);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("StatsUpdated", result.Description);

            queues[WorkQueueType.GameCompleted].Verify(value => value.IsHealthyAsync(It.IsAny<CancellationToken>()), Times.Once);
            queues[WorkQueueType.StatsUpdated].Verify(value => value.IsHealthyAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MissingConfiguredQueueIsUnhealthy()
        {
            var services = new ServiceCollection();
            services.AddKeyedSingleton(WorkQueueType.GameCompleted, new Mock<IWorkQueue>().Object);

            var check = new WorkQueueHealthCheck(services.BuildServiceProvider());

            var result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("MatchCompleted", result.Description);
        }
    }
}