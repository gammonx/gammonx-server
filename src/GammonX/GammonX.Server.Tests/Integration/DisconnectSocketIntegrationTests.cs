using GammonX.Server.Contracts;
using GammonX.Server.Models;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

using Newtonsoft.Json;

namespace GammonX.Server.Tests.Integration
{
    public class DisconnectSocketIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public DisconnectSocketIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                // nothing to modify
            });
        }

        [Fact]
        public async Task PlayerDisconnectsAndTriesToReconnect()
        {
            var setup = await TestHelper.SetupIntegrationTest(_factory);

            var player1Connected = false;
            var player1Disconnected = false;
            var player1CanStartMatch = false;

            setup.Player2Connection.On<object>(ServerEventTypes.PlayerDisconnectedEvent, response =>
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

            setup.Player1Connection.On<object>(ServerEventTypes.PlayerConnectedEvent, response =>
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
                }
                ;
            });

            setup.Player1Connection.On<object>(ServerEventTypes.MatchStateEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchStatePayload payload)
                {
                    Assert.Contains(ServerCommands.StartMatchCommand, payload.AllowedCommands);
                    player1CanStartMatch = true;
                }
            });

            await setup.Player1Connection.StartAsync();
            await setup.Player2Connection.StartAsync();

            await setup.Player1Connection.InvokeAsync(ServerCommands.JoinMatchCommand, setup.MatchId, setup.Player1.PlayerId.ToString());
            await setup.Player2Connection.InvokeAsync(ServerCommands.JoinMatchCommand, setup.MatchId, setup.Player2.PlayerId.ToString());

            await Task.Delay(2500);

            await setup.Player1Connection.StopAsync();

            while (!player1Disconnected)
            {
                await Task.Delay(250);
            }

            await setup.Player1Connection.StartAsync();

            while (!player1Connected)
            {
                await Task.Delay(250);
            }

            await setup.Player1Connection.InvokeAsync(ServerCommands.MatchStateCommand, setup.MatchId);

            while (!player1CanStartMatch)
            {
                await Task.Delay(250);
            }

            Assert.True(player1CanStartMatch);
            Assert.True(player1Connected);
            Assert.False(player1Disconnected);
        }

        [Fact]
        public async Task BothPlayersDisconnect()
        {
            var setup = await TestHelper.SetupIntegrationTest(_factory);

            var player1Disconnected = false;
            var player2Disconnected = false;

            // player 2 receives the disconnected event from player 1
            setup.Player2Connection.On<object>(ServerEventTypes.PlayerDisconnectedEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventDisconnectedPayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventDisconnectedPayload payload)
                {
                    Assert.Equal(TimeSpan.FromSeconds(30), payload.GracePeriod);
                    Assert.True(payload.Expiration > DateTime.UtcNow);
                    player1Disconnected = true;
                }
            });

            await setup.Player1Connection.StartAsync();
            await setup.Player2Connection.StartAsync();

            await setup.Player1Connection.InvokeAsync(ServerCommands.JoinMatchCommand, setup.MatchId, setup.Player1.PlayerId.ToString());
            await setup.Player2Connection.InvokeAsync(ServerCommands.JoinMatchCommand, setup.MatchId, setup.Player2.PlayerId.ToString());

            await Task.Delay(2500);

            await setup.Player1Connection.StopAsync();

            while (!player1Disconnected)
            {
                await Task.Delay(250);
            }

            await setup.Player2Connection.StopAsync();
            // we cannot receive events after connection is gone
            player2Disconnected = true;

            await Task.Delay(1000);

            Assert.True(player1Disconnected);
            Assert.True(player2Disconnected);
        }
    }
}
