using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Stubs;
using GammonX.Server.Tests.Utils;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests.Integration
{
    public class DisconnectSocketIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Guid _player1Id = Guid.Parse("fdd907ca-794a-43f4-83e6-cadfabc57c45");
        private readonly Guid _player2Id = Guid.Parse("f6f9bb06-cbf6-4f42-80bf-5d62be34cff6");
        private const string JwtSecret = "super-secret-key-that-is-at-least-32-characters-long-for-hs256";

        public DisconnectSocketIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IGameSessionFactory));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    services.AddSingleton<IGameSessionFactory>(new TavliStartGameSessionFactoryStub());
                    descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDiceServiceFactory));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    services.AddSingleton<IDiceServiceFactory>(new StartDiceServiceFactoryStub());
                });
            });
        }

        [Fact]
        public async Task PlayerDisconnectsAndTriesToReconnect()
        {
            var client = _factory.CreateClient();
            var serverUri = client.BaseAddress!.ToString().TrimEnd('/');

            var player1 = new JoinRequest(_player1Id, MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame);
            var response1 = await client.PostAsJsonAsync("/game/api/matches/join", player1);
            var resultJson1 = await response1.Content.ReadAsStringAsync();
            var joinResponse1 = JsonConvert.DeserializeObject<RequestResponseContract<RequestQueueEntryPayload>>(resultJson1);
            var joinPayload1 = joinResponse1?.Payload;
            Assert.NotNull(joinPayload1);
            Assert.Equal(QueueEntryStatus.WaitingForOpponent, joinPayload1.Status);

            var player2 = new JoinRequest(_player2Id, MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame);
            var response2 = await client.PostAsJsonAsync("/game/api/matches/join", player2);
            var resultJson2 = await response2.Content.ReadAsStringAsync();
            var joinResponse2 = JsonConvert.DeserializeObject<RequestResponseContract<RequestQueueEntryPayload>>(resultJson2);
            var joinPayload2 = joinResponse2?.Payload;
            Assert.NotNull(joinPayload2);
            Assert.Equal(QueueEntryStatus.WaitingForOpponent, joinPayload2.Status);

            Assert.NotNull(joinPayload1.QueueId);
            Assert.NotNull(joinPayload2.QueueId);
            RequestQueueEntryPayload? result1 = null;
            RequestQueueEntryPayload? result2 = null;
            do
            {
                result1 = await client.PollAsync(player1.PlayerId, joinPayload1.QueueId.Value, MatchModus.Normal);
            }
            while (result1?.Status == QueueEntryStatus.WaitingForOpponent);

            do
            {
                result2 = await client.PollAsync(player2.PlayerId, joinPayload2.QueueId.Value, MatchModus.Normal);
            }
            while (result2?.Status == QueueEntryStatus.WaitingForOpponent);

            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.MatchId, result2.MatchId);
            var matchId = result1.MatchId;
            Assert.True(matchId.HasValue);

            var player1Token = GenerateJwtToken(_player1Id, matchId.Value);
            var player2Token = GenerateJwtToken(_player2Id, matchId.Value);

            var player1Connection = new HubConnectionBuilder()
                .WithUrl($"{serverUri}/matchhub", options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult(player1Token);
                })
                .Build();


            var player2Connection = new HubConnectionBuilder()
                .WithUrl($"{serverUri}/matchhub", options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult(player2Token);
                })
                .Build();

            var player1Connected = false;
            var player1Disconnected = false;
            var player1CanStartMatch = false;

            player2Connection.On<object>(ServerEventTypes.PlayerDisconnectedEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventDisconnectedPayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventDisconnectedPayload payload)
                {
                    Assert.Equal(TimeSpan.FromSeconds(30), payload.GracePeriod);
                    Assert.True(payload.Expiration > DateTime.UtcNow);
                    player1Disconnected = true;
                    player1Connected = false;
                }                
            });

            player1Connection.On<object>(ServerEventTypes.PlayerConnectedEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchLobbyPayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchLobbyPayload payload)
                {
                    if (payload.Player2 != null)
                    {
                        Assert.True(payload.MatchFound);
                        Assert.Contains(ServerCommands.MatchStateCommand, payload.AllowedCommands);
                        player1Connected = true;
                        player1Disconnected = false;
                    }
                    else
                    {
                        Assert.False(payload.MatchFound);
                        Assert.Contains(ServerCommands.JoinMatchCommand, payload.AllowedCommands);
                        player1Connected = true;
                        player1Disconnected = false;
                    }
                };
            });

            player1Connection.On<object>(ServerEventTypes.MatchStateEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchStatePayload payload)
                {
                    Assert.Contains(ServerCommands.StartMatchCommand, payload.AllowedCommands);
                    player1CanStartMatch = true;
                }
            });

            await player1Connection.StartAsync();
            await player2Connection.StartAsync();

            await player1Connection.InvokeAsync(ServerCommands.JoinMatchCommand, matchId, player1.PlayerId.ToString());
            await player2Connection.InvokeAsync(ServerCommands.JoinMatchCommand, matchId, player2.PlayerId.ToString());

            await Task.Delay(5000);

            await player1Connection.StopAsync();

            while (!player1Disconnected)
            {
                await Task.Delay(100);
            }

            await player1Connection.StartAsync();

            while (!player1Connected)
            {
                await Task.Delay(100);
            }

            await player1Connection.InvokeAsync(ServerCommands.MatchStateCommand, matchId);

            while (!player1CanStartMatch)
            {
                await Task.Delay(100);
            }

            Assert.True(player1CanStartMatch);
            Assert.True(player1Connected);
            Assert.False(player1Disconnected);
        }

        private static string? GenerateJwtToken(Guid playerId, Guid matchId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(JwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("playerId", playerId.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, playerId.ToString()),
                    new Claim("matchId", matchId.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

    }
}
