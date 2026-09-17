using GammonX.Server.Http;

namespace GammonX.Server.Tests.Http
{
    public class ResilientExecutorTests
    {
        [Fact]
        public async Task RetriesTransportFailuresAndIncludesInitialAttempt()
        {
            var executor = new ResilientExecutor(3, 0, null);
            var attempts = 0;

            var result = await executor.ExecuteWithRetryAsync(async () =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new HttpRequestException("temporary transport failure");
                }

                await Task.CompletedTask;
                return 42;
            }, CancellationToken.None);

            Assert.Equal(42, result);
            Assert.Equal(3, attempts);
        }

        [Fact]
        public async Task RetriesTimeoutCancellationWhenCallerTokenIsNotCanceled()
        {
            var executor = new ResilientExecutor(2, 0, null);
            var attempts = 0;

            var result = await executor.ExecuteWithRetryAsync(async () =>
            {
                attempts++;
                if (attempts == 1)
                {
                    throw new TaskCanceledException("operation timeout");
                }

                await Task.CompletedTask;
                return "complete";
            }, CancellationToken.None);

            Assert.Equal("complete", result);
            Assert.Equal(2, attempts);
        }

        [Fact]
        public async Task DoesNotRetryWhenCallerCancellationIsRequested()
        {
            var executor = new ResilientExecutor(3, 0, null);
            using var cancellationTokenSource = new CancellationTokenSource();
            var attempts = 0;

            var exception = await Assert.ThrowsAsync<TaskCanceledException>(() =>
                executor.ExecuteWithRetryAsync<int>(() =>
                {
                    attempts++;
                    cancellationTokenSource.Cancel();
                    return Task.FromException<int>(new TaskCanceledException("caller canceled"));
                }, cancellationTokenSource.Token));

            Assert.Equal("caller canceled", exception.Message);
            Assert.Equal(1, attempts);
        }

        [Fact]
        public void RejectsInvalidRetrySettings()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ResilientExecutor(0, 0, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ResilientExecutor(1, -1, null));
        }
    }
}
