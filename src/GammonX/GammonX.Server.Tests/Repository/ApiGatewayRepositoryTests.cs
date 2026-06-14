using DotNetEnv;

using GammonX.Models.Enums;

using GammonX.Server.Repository;

namespace GammonX.Server.Tests.Repository
{
    public class ApiGatewayRepositoryTests
    {
        private readonly ApiGatewayClient _client;

        public ApiGatewayRepositoryTests()
        {
            var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            if (!isDocker)
            {
                var envLocal = Path.Combine(Directory.GetCurrentDirectory(), ".env.local");
                var env = Path.Combine(Directory.GetCurrentDirectory(), ".env");

                if (File.Exists(envLocal))
                {
                    Env.Load(envLocal);
                }
                else if (File.Exists(env))
                {
                    Env.Load(env);
                }
            }

            var baseUrl = Environment.GetEnvironmentVariable("REPOSITORY__BASEURL");
            Assert.NotNull(baseUrl);

            var httpClinet = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(20)
            };
            _client = new ApiGatewayClient(httpClinet);
            Assert.Equal(baseUrl, _client.BaseUrl);
        }

        [Fact(Skip = "AWS_STACK")]
        public async Task CanGetPlayersGamesFromRepository1()
        {
            var existingPlayer1Id = Guid.Parse("b08e895f-a397-4b44-89cc-2372e9b54657");
            var games = await _client.GetPlayersGames(existingPlayer1Id, CancellationToken.None);
            Assert.NotNull(games);
            Assert.NotNull(games.Games);
        }

        [Fact(Skip = "AWS_STACK")]
        public async Task ReturnsNullOnUnknownPlayer()
        {
            var unknownPlayerId = Guid.NewGuid();
            var games = await _client.GetPlayersGames(unknownPlayerId, CancellationToken.None);
            Assert.Null(games);
        }

        [Fact(Skip = "AWS_STACK")]
        public async Task CanGetRatingFromRepository1()
        {
            var existingPlayer1Id = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
            var rating = await _client.GetRatingAsync(existingPlayer1Id, MatchVariant.Tavli, CancellationToken.None);
            Assert.NotNull(rating);
            Assert.True(rating.Rating > 1200);
        }

        [Fact(Skip = "AWS_STACK")]
        public async Task CanGetRatingFromRepository2()
        {
            var existingPlayer1Id = Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8");
            var rating = await _client.GetRatingAsync(existingPlayer1Id, MatchVariant.Tavli, CancellationToken.None);
            Assert.NotNull(rating);
            Assert.True(rating.Rating < 1200);
        }

        [Fact(Skip = "AWS_STACK")]
        public async Task CannotGetRatingOnWrongPlayerId()
        {
            var existingPlayer1Id = Guid.Empty;
            var rating = await _client.GetRatingAsync(existingPlayer1Id, MatchVariant.Tavli, CancellationToken.None);
            Assert.NotNull(rating);
            // we expect a default value for the rating
            Assert.Equal(1200, rating.Rating);
        }

        [Fact(Skip = "AWS_STACK")]
        public async Task CannotGetRatingOnWrongVariant()
        {
            var existingPlayer1Id = Guid.Empty;
            var rating = await _client.GetRatingAsync(existingPlayer1Id, MatchVariant.Unknown, CancellationToken.None);
            Assert.NotNull(rating);
            // we expect a default value for the rating
            Assert.Equal(1200, rating.Rating);
        }
    }
}
