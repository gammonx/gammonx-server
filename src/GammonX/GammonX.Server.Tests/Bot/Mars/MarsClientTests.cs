using System.Net;

using GammonX.Server.Tests.Http;

using Microsoft.AspNetCore.Http;

namespace GammonX.Server.Tests.Bot.Mars
{
    public class MarsClientTests
    {
        [Fact]
        public async Task HealthProbeUsesConfiguredMarsHealthEndpoint()
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromResult(MarsStubs.JsonResponse(HttpStatusCode.OK, "healthy")));
            using var client = MarsStubs.CreateClient(handler, 1);
            client.BaseAddress = new Uri("http://localhost:8083/bot/mars/");

            var result = await client.IsHealthyAsync(CancellationToken.None);

            Assert.True(result);
            Assert.Equal("http://localhost:8083/bot/mars/health", handler.RequestUris.Single()?.ToString());
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task HealthProbeReturnsFalseForUnsuccessfulResponses(HttpStatusCode statusCode)
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromResult(MarsStubs.JsonResponse(statusCode, "unavailable")));
            using var client = MarsStubs.CreateClient(handler, 1);

            var result = await client.IsHealthyAsync(CancellationToken.None);

            Assert.False(result);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task HealthProbeReturnsFalseForTransportFailures()
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromException<HttpResponseMessage>(new HttpRequestException("connection refused")));
            using var client = MarsStubs.CreateClient(handler, 1);

            var result = await client.IsHealthyAsync(CancellationToken.None);

            Assert.False(result);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task HealthProbePropagatesCallerCancellation()
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromResult(MarsStubs.JsonResponse(HttpStatusCode.OK, "healthy")));
            using var client = MarsStubs.CreateClient(handler, 1);
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                client.IsHealthyAsync(cancellationTokenSource.Token));

            Assert.Equal(0, handler.RequestCount);
        }

        [Theory]
        [InlineData(408)]
        [InlineData(429)]
        [InlineData(503)]
        public async Task RetriesRetryableStatusCodes(int statusCode)
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromResult(MarsStubs.JsonResponse((HttpStatusCode)statusCode, "temporary")),
                _ => Task.FromResult(MarsStubs.JsonResponse(HttpStatusCode.OK, MarsStubs.MoveResponse)));
            using var client = MarsStubs.CreateClient(handler, 2);

            var result = await client.GetMoveEvalAsync(MarsStubs.CreateMoveRequest(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(2, handler.RequestCount);
        }

        [Fact]
        public async Task RetriesTransportFailures()
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromException<HttpResponseMessage>(new HttpRequestException("connection reset")),
                _ => Task.FromResult(MarsStubs.JsonResponse(HttpStatusCode.OK, MarsStubs.MoveResponse)));
            using var client = MarsStubs.CreateClient(handler, 2);

            var result = await client.GetMoveEvalAsync(MarsStubs.CreateMoveRequest(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(2, handler.RequestCount);
        }

        [Theory]
        [InlineData(400)]
        [InlineData(404)]
        [InlineData(422)]
        public async Task DoesNotRetryNonRetryableClientErrors(int statusCode)
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromResult(MarsStubs.JsonResponse((HttpStatusCode)statusCode, "invalid request")));
            using var client = MarsStubs.CreateClient(handler, 3);

            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
                client.GetMoveEvalAsync(MarsStubs.CreateMoveRequest(), CancellationToken.None));

            Assert.Contains("invalid request", exception.Message);
            Assert.Equal(1, handler.RequestCount);
        }

        [Fact]
        public async Task ReturnsFinalRetryableErrorBodyAfterAttemptsAreExhausted()
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromResult(MarsStubs.JsonResponse(HttpStatusCode.ServiceUnavailable, "temporary")),
                _ => Task.FromResult(MarsStubs.JsonResponse(HttpStatusCode.ServiceUnavailable, "final failure")));
            using var client = MarsStubs.CreateClient(handler, 2);

            var exception = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
                client.GetMoveEvalAsync(MarsStubs.CreateMoveRequest(), CancellationToken.None));

            Assert.Contains("final failure", exception.Message);
            Assert.Equal(2, handler.RequestCount);
        }

        [Fact]
        public async Task DoesNotRetryMalformedSuccessfulPayload()
        {
            var handler = new ScriptedHttpMessageHandler(
                _ => Task.FromResult(MarsStubs.JsonResponse(HttpStatusCode.OK, "null")));
            using var client = MarsStubs.CreateClient(handler, 3);

            await Assert.ThrowsAsync<BadHttpRequestException>(() =>
                client.GetMoveEvalAsync(MarsStubs.CreateMoveRequest(), CancellationToken.None));

            Assert.Equal(1, handler.RequestCount);
        }
    }
}
