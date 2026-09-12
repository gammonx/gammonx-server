using System.Net;
using System.Text;

using GammonX.Server.Repository;

namespace GammonX.Server.Tests.Repository
{
    public class ApiGatewayClientTemporalTests
    {
        [Fact]
        public async Task GetPlayersGamesParsesCanonicalUtcAndDurationMilliseconds()
        {
            using var client = CreateClient("""
                {"Games":[{"StartedAt":"2026-09-10T16:56:33.2245744Z","EndedAt":"2026-09-10T16:56:34.5579077Z","DurationMilliseconds":1333}]}
                """);
            var repositoryClient = new ApiGatewayClient(client);

            var result = await repositoryClient.GetPlayersGames(Guid.NewGuid(), CancellationToken.None);

            Assert.NotNull(result);
            var game = Assert.Single(result.Games);
            Assert.Equal(DateTimeKind.Utc, game.StartedAt.Kind);
            Assert.Equal(DateTimeKind.Utc, game.EndedAt.Kind);
            Assert.Equal(1333, game.DurationMilliseconds);
        }

        [Fact]
        public async Task GetPlayersGamesRejectsTimezoneLessTimestamp()
        {
            using var client = CreateClient("""
                {"Games":[{"StartedAt":"2026-09-10T16:56:33.2245744","EndedAt":"2026-09-10T16:56:34.5579077Z","DurationMilliseconds":1333}]}
                """);
            var repositoryClient = new ApiGatewayClient(client);

            var result = await repositoryClient.GetPlayersGames(Guid.NewGuid(), CancellationToken.None);

            Assert.Null(result);
        }

        private static HttpClient CreateClient(string responseBody)
        {
            return new HttpClient(new StubHandler(responseBody))
            {
                BaseAddress = new Uri("https://repository.test/")
            };
        }

        private sealed class StubHandler(string responseBody) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
