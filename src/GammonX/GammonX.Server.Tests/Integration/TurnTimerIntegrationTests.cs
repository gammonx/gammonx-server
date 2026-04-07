using GammonX.Server.Contracts;
using GammonX.Server.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

using Newtonsoft.Json;

namespace GammonX.Server.Tests.Integration
{
    public class TurnTimerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public TurnTimerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                // nothing to modify
            });
        }

        [Fact(Skip = "takes to long (timeout dependent)")]
        public async Task TurnTimerTimesOut()
        {
            var setup = await TestHelper.SetupIntegrationTest(_factory);

            var timeout1Received = false;
            var timeout2Received = false;
            setup.Player1Connection.On<object>(ServerEventTypes.TurnTimerEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventTurnTimerPayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventTurnTimerPayload payload)
                {
                    timeout1Received = true;
                }
            });

            setup.Player2Connection.On<object>(ServerEventTypes.TurnTimerEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventTurnTimerPayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventTurnTimerPayload payload)
                {
                    timeout2Received = true;
                }
            });

            var onePlayerResigned = false;
            setup.Player1Connection.On<object>(ServerEventTypes.GameEndedEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchStatePayload payload)
                {
                    Assert.NotNull(payload);
                    var firstGame = payload.GameRounds?.FirstOrDefault();
                    Assert.NotNull(firstGame);
                    Assert.Equal(GamePhase.GameOver, firstGame.Phase);
                    onePlayerResigned = true;
                }
            });

            setup.Player2Connection.On<object>(ServerEventTypes.GameEndedEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchStatePayload payload)
                {
                    Assert.NotNull(payload);
                    var firstGame = payload.GameRounds?.FirstOrDefault();
                    Assert.NotNull(firstGame);
                    Assert.Equal(GamePhase.GameOver, firstGame.Phase);
                    onePlayerResigned = true;
                }
            });

            await setup.Player1Connection.StartAsync();
            await setup.Player2Connection.StartAsync();


            while (!timeout1Received || !timeout2Received)
            {
                await Task.Delay(250);
            }

            Assert.True(timeout1Received);
            Assert.True(timeout2Received);
            timeout1Received = false;
            timeout2Received = false;

            await setup.Player1Connection.InvokeAsync(ServerCommands.JoinMatchCommand, setup.MatchId, setup.Player1.PlayerId.ToString());
            await setup.Player2Connection.InvokeAsync(ServerCommands.JoinMatchCommand, setup.MatchId, setup.Player2.PlayerId.ToString());

            while (!timeout1Received || !timeout2Received)
            {
                await Task.Delay(250);
            }

            Assert.True(timeout1Received);
            Assert.True(timeout2Received);
            timeout1Received = false;
            timeout2Received = false;

            await setup.Player1Connection.InvokeAsync(ServerCommands.StartMatchCommand, setup.MatchId);
            await setup.Player2Connection.InvokeAsync(ServerCommands.StartMatchCommand, setup.MatchId);

            while (!onePlayerResigned)
            {
                await Task.Delay(250);
            }

            Assert.True(onePlayerResigned);
        }
    }
}
