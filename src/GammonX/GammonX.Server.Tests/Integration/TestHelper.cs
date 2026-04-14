using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Tests.Utils;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.IdentityModel.Tokens;

using Newtonsoft.Json;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests.Integration
{
    internal static class TestHelper
    {
        private static readonly Guid _player1Id = Guid.Parse("fdd907ca-794a-43f4-83e6-cadfabc57c45");
        private static readonly Guid _player2Id = Guid.Parse("f6f9bb06-cbf6-4f42-80bf-5d62be34cff6");
        private const string JwtSecret = "super-secret-key-that-is-at-least-32-characters-long-for-hs256";

        internal static async Task<MatchHubIntegrationTest> SetupIntegrationTest(WebApplicationFactory<Program> factory)
        {
            var client = factory.CreateClient();
            var serverUri = client.BaseAddress!.ToString().TrimEnd('/');

            var player1 = new JoinRequest(_player1Id, MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame);
            var response1 = await client.PostAsJsonAsync("/game/api/matches/join", player1);
            var resultJson1 = await response1.Content.ReadAsStringAsync();
            var joinResponse1 = JsonConvert.DeserializeObject<ResponseContract<RequestQueueEntryPayload>>(resultJson1);
            var joinPayload1 = joinResponse1?.Payload;
            Assert.NotNull(joinPayload1);
            Assert.Equal(QueueEntryStatus.WaitingForOpponent, joinPayload1.Status);

            var player2 = new JoinRequest(_player2Id, MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame);
            var response2 = await client.PostAsJsonAsync("/game/api/matches/join", player2);
            var resultJson2 = await response2.Content.ReadAsStringAsync();
            var joinResponse2 = JsonConvert.DeserializeObject<ResponseContract<RequestQueueEntryPayload>>(resultJson2);
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
                    options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult(player1Token);
                })
                .Build();


            var player2Connection = new HubConnectionBuilder()
                .WithUrl($"{serverUri}/matchhub", options =>
                {
                    options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                    options.AccessTokenProvider = () => Task.FromResult(player2Token);
                })
                .Build();
            return new MatchHubIntegrationTest
            {
                Player1 = player1,
                Player2 = player2,
                MatchId = matchId,
                Player1Connection = player1Connection,
                Player2Connection = player2Connection
            };
        }

        internal readonly struct MatchHubIntegrationTest
        {
            public JoinRequest Player1 { get; init; }

            public JoinRequest Player2 { get; init; }

            public Guid? MatchId { get; init; }

            public HubConnection Player1Connection { get; init; }

            public HubConnection Player2Connection { get; init; }
        }

        internal static string? GenerateJwtToken(Guid playerId, Guid matchId)
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
