using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
		public async Task PlayersCanJoinAMatchAsync()
		{
			// arrange in memory rest service
			var client = _factory.CreateClient();
			var serverUri = client.BaseAddress!.ToString().TrimEnd('/');

			// arrange player 1
			var player1 = new {
				ClientId = "fdd907ca-794a-43f4-83e6-cadfabc57c45",
				Mode = "Backgammon"
			};
			var response1 = await client.PostAsJsonAsync("/api/matches/join", player1);
			var resultJson1 = await response1.Content.ReadAsStringAsync();
			var joinResponse1 = JsonConvert.DeserializeObject<ResponseContract>(resultJson1);
			var player1Connection = new HubConnectionBuilder()
				.WithUrl($"{serverUri}/matchhub", options =>
				{
					options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
				})
				.Build();

			// arrange player 2
			var player2 = new
			{
				ClientId = "f6f9bb06-cbf6-4f42-80bf-5d62be34cff6",
				Mode = "Backgammon"
			};
			var response2 = await client.PostAsJsonAsync("/api/matches/join", player2);
			var resultJson2 = await response2.Content.ReadAsStringAsync();
			var joinResponse2 = JsonConvert.DeserializeObject<ResponseContract>(resultJson2);
			var player2Connection = new HubConnectionBuilder()
				.WithUrl($"{serverUri}/matchhub", options =>
				{
					options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
				})
				.Build();

			Assert.Equal(joinResponse1?.Data, joinResponse2?.Data);

			object? receivedState = null;

			player1Connection.On<MatchLobby>(ServerEventTypes.MatchLobbyFoundEvent, state =>
			{
				receivedState = state;
			});

			player1Connection.On<MatchLobby>(ServerEventTypes.MatchLobbyWaitingEvent, state =>
			{
				receivedState = state;
			});

			player1Connection.On<ResponseContract>(ServerEventTypes.ErrorEvent, state =>
			{
				receivedState = state;
			});

			player2Connection.On<MatchLobby>(ServerEventTypes.MatchLobbyFoundEvent, state =>
			{
				receivedState = state;
			});

			player2Connection.On<MatchLobby>(ServerEventTypes.MatchLobbyWaitingEvent, state =>
			{
				receivedState = state;
			});

			player2Connection.On<ResponseContract>(ServerEventTypes.ErrorEvent, state =>
			{
				receivedState = state;
			});

			await player1Connection.StartAsync();
			await player2Connection.StartAsync();

			await player1Connection.InvokeAsync("JoinMatch", joinResponse1?.Data, player1.ClientId.ToString());
			await player2Connection.InvokeAsync("JoinMatch", joinResponse2?.Data, player2.ClientId.ToString());

			await Task.Delay(500);

			Assert.NotNull(receivedState);
		}
	}
}