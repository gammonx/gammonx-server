using GammonX.Server.Contracts;
using GammonX.Server.Models;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

using Newtonsoft.Json;

using System.Net.Http.Json;

namespace GammonX.Server.Tests
{
	public class MatchLobbyHubTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly WebApplicationFactory<Program> _factory;

		public MatchLobbyHubTests(WebApplicationFactory<Program> factory)
		{
			_factory = factory;
		}

		[Fact]
		public async Task MatchAndGameIntegrationTest()
		{
			// arrange in memory rest service
			var client = _factory.CreateClient();
			var serverUri = client.BaseAddress!.ToString().TrimEnd('/');

			// arrange player 1
			var player1 = new {
				PlayerId = "fdd907ca-794a-43f4-83e6-cadfabc57c45",
				MatchVariant = WellKnownMatchVariant.Tavli
			};
			var response1 = await client.PostAsJsonAsync("/api/matches/join", player1);
			var resultJson1 = await response1.Content.ReadAsStringAsync();
			var joinResponse1 = JsonConvert.DeserializeObject<RequestResponseContract<RequestMatchIdPayload>>(resultJson1);
			var joinPayload1 = joinResponse1?.Payload as RequestMatchIdPayload;
			Assert.NotNull(joinPayload1);
			var player1Connection = new HubConnectionBuilder()
				.WithUrl($"{serverUri}/matchhub", options =>
				{
					options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
				})
				.Build();

			// arrange player 2
			var player2 = new
			{
				PlayerId = "f6f9bb06-cbf6-4f42-80bf-5d62be34cff6",
				MatchVariant = WellKnownMatchVariant.Tavli
			};
			var response2 = await client.PostAsJsonAsync("/api/matches/join", player2);
			var resultJson2 = await response2.Content.ReadAsStringAsync();
			var joinResponse2 = JsonConvert.DeserializeObject<RequestResponseContract<RequestMatchIdPayload>>(resultJson2);
			var joinPayload2 = joinResponse2?.Payload as RequestMatchIdPayload;
			Assert.NotNull(joinPayload2);
			var player2Connection = new HubConnectionBuilder()
				.WithUrl($"{serverUri}/matchhub", options =>
				{
					options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
				})
				.Build();

			Assert.Equal(joinPayload1.MatchId, joinPayload2.MatchId);

			player1Connection.On<EventResponseContract<EventMatchLobbyPayload>>(ServerEventTypes.MatchLobbyWaitingEvent, response =>
			{
				Assert.True(response is not null);
				Assert.True(response.Payload is EventMatchLobbyPayload);
				if (response.Payload is EventMatchLobbyPayload payload)
				{
					Assert.Equal(joinPayload1.MatchId, payload.Id);
					Assert.False(payload.MatchFound, "Player 1 should not have a match found yet.");
					Assert.Equal(player1.PlayerId, payload.Player1.ToString());
					Assert.Null(payload.Player2);
				}
			});

			player2Connection.On<EventResponseContract<EventMatchLobbyPayload>>(ServerEventTypes.MatchLobbyWaitingEvent, response =>
			{
				Assert.Fail();
			});

			player1Connection.On<EventResponseContract<EventMatchLobbyPayload>>(ServerEventTypes.MatchLobbyFoundEvent, response =>
			{
				Assert.True(response is not null);
				Assert.True(response.Payload is EventMatchLobbyPayload);
				if (response.Payload is EventMatchLobbyPayload payload)
				{
					Assert.Equal(joinPayload1.MatchId, payload.Id);
					Assert.Equal(player1.PlayerId, payload.Player1.ToString());
					Assert.Equal(player2.PlayerId, payload.Player2.ToString());
					Assert.NotNull(payload.Player2);
					Assert.True(payload.MatchFound);
				}				
			});

			player2Connection.On<EventResponseContract<EventMatchLobbyPayload>>(ServerEventTypes.MatchLobbyFoundEvent, response =>
			{
				Assert.True(response is not null);
				Assert.True(response.Payload is EventMatchLobbyPayload);
				if (response.Payload is EventMatchLobbyPayload payload)
				{
					Assert.Equal(joinPayload1.MatchId, payload.Id);
					Assert.Equal(player1.PlayerId, payload.Player1.ToString());
					Assert.Equal(player2.PlayerId, payload.Player2.ToString());
					Assert.NotNull(payload.Player2);
					Assert.True(payload.MatchFound);
				}
			});

			await player1Connection.StartAsync();
			await player2Connection.StartAsync();

			// player 1 joins
			await player1Connection.InvokeAsync(nameof(MatchLobbyHub.JoinMatch), joinPayload1.MatchId.ToString(), player1.PlayerId.ToString());

			await Task.Delay(500);

			// player 2 joins
			await player2Connection.InvokeAsync(nameof(MatchLobbyHub.JoinMatch), joinPayload2?.MatchId.ToString(), player2.PlayerId.ToString());

			await Task.Delay(500);

			// TODO :: StartGame

			// TODO :: Roll

			// TODO :: Move

			// TODO :: Move

			// TODO :: Roll

			// TODO :: Move
		}
	}
}