using System.Net;

using GammonX.Server.Tests.Http;

using Microsoft.AspNetCore.Http;

namespace GammonX.Server.Tests.Bot.Mars
{
    public class MarsClientTests
    {
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
