using GammonX.Server.Bot;

namespace GammonX.Server.Tests.Bot
{
    public class BotHealthTests
    {
        [Fact]
        public async Task SimpleBotReportsHealthy()
        {
            var result = await new SimpleBotService().IsHealthyAsync(CancellationToken.None);

            Assert.True(result);
        }

        [Fact]
        public async Task WildBgBotReportsHealthy()
        {
            using var client = new HttpClient();
            var result = await new WildbgBotService(client).IsHealthyAsync(CancellationToken.None);

            Assert.True(result);
        }

        [Fact]
        public async Task SimpleBotPropagatesCancellation()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                new SimpleBotService().IsHealthyAsync(cancellationTokenSource.Token));
        }

        [Fact]
        public async Task WildBgBotPropagatesCancellation()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            using var client = new HttpClient();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                new WildbgBotService(client).IsHealthyAsync(cancellationTokenSource.Token));
        }
    }
}
