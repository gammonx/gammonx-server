using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using GammonX.Server.Models;

using GammonX.Server.Tests.Stubs;
using GammonX.Server.Tests.Utils;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using System.Net.Http.Json;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests.Integration
{
    public class ForceRerollOnMatchStartIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Guid _player1Id = Guid.Parse("fdd907ca-794a-43f4-83e6-cadfabc57c45");
        private readonly Guid _player2Id = Guid.Parse("f6f9bb06-cbf6-4f42-80bf-5d62be34cff6");

        public ForceRerollOnMatchStartIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDiceServiceFactory));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    services.AddSingleton<IDiceServiceFactory>(new ForceRerollDiceFactoryStub());
                });
            });
        }

        [Theory]
        [InlineData(MatchVariant.Backgammon)]
        [InlineData(MatchVariant.Tavli)]
        [InlineData(MatchVariant.Tavla)]
        public async Task ForceRerollOnMatchStart(MatchVariant variant)
        {
            var client = _factory.CreateClient();
            var serverUri = client.BaseAddress!.ToString().TrimEnd('/');

            var player1 = new JoinRequest(_player1Id, variant, MatchModus.Normal, MatchType.CashGame);
            var response1 = await client.PostAsJsonAsync("/game/api/matches/join", player1);
            var resultJson1 = await response1.Content.ReadAsStringAsync();
            var joinResponse1 = JsonConvert.DeserializeObject<RequestResponseContract<RequestQueueEntryPayload>>(resultJson1);
            var joinPayload1 = joinResponse1?.Payload;
            Assert.NotNull(joinPayload1);
            Assert.Equal(QueueEntryStatus.WaitingForOpponent, joinPayload1.Status);
            var player1Connection = new HubConnectionBuilder()
                .WithUrl($"{serverUri}/matchhub", options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
                .Build();

            var player2 = new JoinRequest(_player2Id, variant, MatchModus.Normal, MatchType.CashGame);
            var response2 = await client.PostAsJsonAsync("/game/api/matches/join", player2);
            var resultJson2 = await response2.Content.ReadAsStringAsync();
            var joinResponse2 = JsonConvert.DeserializeObject<RequestResponseContract<RequestQueueEntryPayload>>(resultJson2);
            var joinPayload2 = joinResponse2?.Payload;
            Assert.NotNull(joinPayload2);
            Assert.Equal(QueueEntryStatus.WaitingForOpponent, joinPayload2.Status);
            var player2Connection = new HubConnectionBuilder()
                .WithUrl($"{serverUri}/matchhub", options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                })
                .Build();

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
            joinPayload1 = result1;
            joinPayload2 = result2;

            // ##################################################
            // ERROR
            // ##################################################

            player1Connection.On<object>(ServerEventTypes.ErrorEvent, response =>
            {
                Assert.Fail();
            });

            player2Connection.On<object>(ServerEventTypes.ErrorEvent, response =>
            {
                Assert.Fail();
            });

            // ##################################################
            // PLAYERS JOINING THE MATCH
            // ##################################################

            var player1MatchStarted = false;
            var player1MatchStartCount = 0;
            player1Connection.On<object>(ServerEventTypes.MatchWaitingForStartEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchStatePayload payload)
                {
                    Assert.Equal(joinPayload1.MatchId, payload.Id);
                    Assert.NotNull(payload.Player1);
                    Assert.NotNull(payload.Player2);
                    Assert.Null(payload.Player1.UserName);
                    Assert.Null(payload.Player2.UserName);
                    Assert.Equal(player1.PlayerId, payload.Player1.Id);
                    Assert.Equal(player2.PlayerId, payload.Player2.Id);
                    Assert.Equal(1, payload.GameRound);
                    Assert.NotNull(payload.Player2);
                    Assert.NotNull(payload.GameRounds);
                    Assert.Empty(payload.GameRounds);
                    Assert.Equal(3, payload.AllowedCommands.Length);
                    Assert.Contains(ServerCommands.StartMatchCommand, payload.AllowedCommands);
                    Assert.Contains(ServerCommands.ResignGameCommand, payload.AllowedCommands);
                    Assert.Contains(ServerCommands.ResignMatchCommand, payload.AllowedCommands);
                    Assert.Equal($"match_{matchId}", payload.GroupName);
                    Assert.Null(payload.Winner);
                    Assert.Null(payload.WinnerPoints);
                    Assert.Null(payload.Loser);
                    Assert.Null(payload.LoserPoints);
                    Assert.Equal(MatchModus.Normal, payload.Modus);
                    Assert.Equal(variant, payload.Variant);
                    Assert.Equal(MatchType.CashGame, payload.Type);
                    // we should not have opening dices before the match is started
                    if (player1MatchStartCount == 0)
                    {
                        Assert.Null(payload.Player1.StartDiceRoll);
                        Assert.Null(payload.Player2.StartDiceRoll);
                    }
                    else
                    {
                        Assert.NotNull(payload.Player1.StartDiceRoll);
                        Assert.NotNull(payload.Player2.StartDiceRoll);
                        Assert.Equal(5, payload.Player1.StartDiceRoll);
                        Assert.Equal(5, payload.Player2.StartDiceRoll);
                    }
                    player1MatchStarted = true;
                    player1MatchStartCount++;
                }
            });

            var player2MatchStarted = false;
            var player2MatchStartCount = 0;
            player2Connection.On<object>(ServerEventTypes.MatchWaitingForStartEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchStatePayload payload)
                {
                    Assert.Equal(joinPayload1.MatchId, payload.Id);
                    Assert.NotNull(payload.Player1);
                    Assert.NotNull(payload.Player2);
                    Assert.Null(payload.Player1.UserName);
                    Assert.Null(payload.Player2.UserName);
                    Assert.Equal(player1.PlayerId, payload.Player1.Id);
                    Assert.Equal(player2.PlayerId, payload.Player2.Id);
                    Assert.Equal(1, payload.GameRound);
                    Assert.NotNull(payload.Player2);
                    Assert.NotNull(payload.GameRounds);
                    Assert.Empty(payload.GameRounds);
                    Assert.Equal(3, payload.AllowedCommands.Length);
                    Assert.Contains(ServerCommands.StartMatchCommand, payload.AllowedCommands);
                    Assert.Contains(ServerCommands.ResignGameCommand, payload.AllowedCommands);
                    Assert.Contains(ServerCommands.ResignMatchCommand, payload.AllowedCommands);
                    Assert.Equal($"match_{matchId}", payload.GroupName);
                    Assert.Null(payload.Winner);
                    Assert.Null(payload.WinnerPoints);
                    Assert.Null(payload.Loser);
                    Assert.Null(payload.LoserPoints);
                    Assert.Equal(MatchModus.Normal, payload.Modus);
                    Assert.Equal(variant, payload.Variant);
                    Assert.Equal(MatchType.CashGame, payload.Type);
                    // we should not have opening dices before the match is started
                    if (player2MatchStartCount == 0)
                    {
                        Assert.Null(payload.Player1.StartDiceRoll);
                        Assert.Null(payload.Player2.StartDiceRoll);
                    }
                    else
                    {
                        Assert.NotNull(payload.Player1.StartDiceRoll);
                        Assert.NotNull(payload.Player2.StartDiceRoll);
                        Assert.Equal(5, payload.Player1.StartDiceRoll);
                        Assert.Equal(5, payload.Player2.StartDiceRoll);
                    }
                    player2MatchStarted = true;
                    player2MatchStartCount++;
                }
            });

            // ##################################################
            // PLAYERS STARTING THE MATCH
            // ##################################################
            var player1MatchWaiting = false;
            player1Connection.On<object>(ServerEventTypes.MatchWaitingEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchStatePayload payload)
                {
                    Assert.Equal(joinPayload1.MatchId, payload.Id);
                    Assert.NotNull(payload.Player1);
                    Assert.NotNull(payload.Player2);
                    Assert.Equal(player1.PlayerId, payload.Player1.Id);
                    Assert.Equal(2, payload.AllowedCommands.Length);
                    Assert.Contains(ServerCommands.ResignGameCommand, payload.AllowedCommands);
                    Assert.Contains(ServerCommands.ResignMatchCommand, payload.AllowedCommands);
                    Assert.NotNull(payload.GameRounds);
                    Assert.Empty(payload.GameRounds);
                    // player 1 has rolled his opening dice, player 2 not yet
                    Assert.NotNull(payload.Player1.StartDiceRoll);
                    Assert.Equal(5, payload.Player1.StartDiceRoll);
                    Assert.Null(payload.Player2.StartDiceRoll);
                    player1MatchWaiting = true;
                }
                else
                {
                    Assert.Fail("Expected EventResponseContract<EventMatchStatePayload> but got: " + response?.GetType().Name);
                }
            });

            player2Connection.On<object>(ServerEventTypes.MatchWaitingEvent, response =>
            {
                // there is no game waiting event for the first game
                Assert.Fail();
            });


            player1Connection.On<object>(ServerEventTypes.MatchStartedEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchStatePayload payload)
                {
                    Assert.Equal(joinPayload1.MatchId, payload.Id);
                    Assert.NotNull(payload.Player1);
                    Assert.NotNull(payload.Player2);
                    Assert.Equal(player1.PlayerId, payload.Player1.Id);
                    Assert.Equal(3, payload.AllowedCommands.Length);
                    Assert.Contains(ServerCommands.ResignGameCommand, payload.AllowedCommands);
                    Assert.Contains(ServerCommands.ResignMatchCommand, payload.AllowedCommands);
                    Assert.Contains(ServerCommands.MoveCommand, payload.AllowedCommands);
                    Assert.NotNull(payload.GameRounds);
                    Assert.Single(payload.GameRounds);
                    // both players have rolled their opening dice
                    Assert.NotNull(payload.Player1.StartDiceRoll);
                    Assert.NotNull(payload.Player2.StartDiceRoll);
                    // player 1 won
                    Assert.Equal(6, payload.Player1.StartDiceRoll);
                    Assert.Equal(1, payload.Player2.StartDiceRoll);
                }
                else
                {
                    Assert.Fail("Expected EventResponseContract<EventMatchStatePayload> but got: " + response?.GetType().Name);
                }
            });

            player2Connection.On<object>(ServerEventTypes.MatchStartedEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchStatePayload payload)
                {
                    Assert.Equal(joinPayload1.MatchId, payload.Id);
                    Assert.NotNull(payload.Player1);
                    Assert.NotNull(payload.Player2);
                    Assert.Equal(player2.PlayerId, payload.Player2.Id);
                    Assert.Equal(2, payload.AllowedCommands.Length);
                    Assert.Contains(ServerCommands.ResignGameCommand, payload.AllowedCommands);
                    Assert.Contains(ServerCommands.ResignMatchCommand, payload.AllowedCommands);
                    Assert.NotNull(payload.GameRounds);
                    Assert.Single(payload.GameRounds);
                    // both players have rolled their opening dice
                    Assert.NotNull(payload.Player1.StartDiceRoll);
                    Assert.NotNull(payload.Player2.StartDiceRoll);
                    // player 1 won
                    Assert.Equal(6, payload.Player1.StartDiceRoll);
                    Assert.Equal(1, payload.Player2.StartDiceRoll);
                }
                else
                {
                    Assert.Fail("Expected EventResponseContract<EventMatchStatePayload> but got: " + response?.GetType().Name);
                }
            });

            await player1Connection.StartAsync();
            await player2Connection.StartAsync();

            // join the match
            await player1Connection.InvokeAsync(ServerCommands.JoinMatchCommand, matchId, player1.PlayerId.ToString());
            await player2Connection.InvokeAsync(ServerCommands.JoinMatchCommand, matchId, player2.PlayerId.ToString());

            while (!player1MatchStarted || !player2MatchStarted)
            {
                await Task.Delay(100);
            }

            Assert.True(player1MatchStarted);
            Assert.True(player2MatchStarted);
            Assert.Equal(1, player1MatchStartCount);
            Assert.Equal(1, player2MatchStartCount);

            // start the match and roll their opening dice (should be equal)
            await player1Connection.InvokeAsync(ServerCommands.StartMatchCommand, matchId);
            await player2Connection.InvokeAsync(ServerCommands.StartMatchCommand, matchId);

            while (!player1MatchWaiting)
            {
                await Task.Delay(100);
            }

            Assert.True(player1MatchWaiting);

            while (player1MatchStartCount == 1 || player2MatchStartCount == 1)
            {
                await Task.Delay(100);
            }

            // start the match a second time
            await player1Connection.InvokeAsync(ServerCommands.StartMatchCommand, matchId);
            await player2Connection.InvokeAsync(ServerCommands.StartMatchCommand, matchId);

            // both player had to start the match twice due to forced re-roll
            Assert.Equal(2, player1MatchStartCount);
            Assert.Equal(2, player2MatchStartCount);
        }
    }
}
