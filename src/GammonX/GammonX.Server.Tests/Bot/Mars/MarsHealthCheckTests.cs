using GammonX.Server.Bot;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Moq;

namespace GammonX.Server.Tests.Bot.Mars
{
    public class MarsHealthCheckTests
    {
        [Fact]
        public async Task ReportsHealthyWhenMarsBotIsHealthy()
        {
            var botService = new Mock<IBotService>();
            botService
                .Setup(service => service.IsHealthyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var check = CreateHealthCheck(botService.Object);

            var result = await check.CheckHealthAsync(
                new HealthCheckContext(),
                TestContext.Current.CancellationToken);

            Assert.Equal(HealthStatus.Healthy, result.Status);
            botService.Verify(
                service => service.IsHealthyAsync(TestContext.Current.CancellationToken),
                Times.Once);
        }

        [Fact]
        public async Task ReportsUnhealthyWhenMarsBotIsUnavailable()
        {
            var botService = new Mock<IBotService>();
            botService
                .Setup(service => service.IsHealthyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            var check = CreateHealthCheck(botService.Object);

            var result = await check.CheckHealthAsync(
                new HealthCheckContext(),
                TestContext.Current.CancellationToken);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("unavailable", result.Description);
        }

        [Fact]
        public async Task ReportsUnhealthyWhenMarsBotIsNotRegistered()
        {
            var check = new MarsHealthCheck(new ServiceCollection().BuildServiceProvider());

            var result = await check.CheckHealthAsync(
                new HealthCheckContext(),
                TestContext.Current.CancellationToken);

            Assert.Equal(HealthStatus.Unhealthy, result.Status);
            Assert.Contains("not registered", result.Description);
        }

        [Fact]
        public async Task PropagatesCancellationFromMarsBot()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            var botService = new Mock<IBotService>();
            botService
                .Setup(service => service.IsHealthyAsync(cancellationTokenSource.Token))
                .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));
            var check = CreateHealthCheck(botService.Object);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                check.CheckHealthAsync(new HealthCheckContext(), cancellationTokenSource.Token));
        }

        private static MarsHealthCheck CreateHealthCheck(IBotService botService)
        {
            var services = new ServiceCollection();
            services.AddKeyedSingleton(WellKnownBotServices.Mars, botService);
            return new MarsHealthCheck(services.BuildServiceProvider());
        }
    }
}
