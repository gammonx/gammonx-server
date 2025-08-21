using GammonX.Engine.Models;

using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Stubs;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using System.Net.Http.Json;

namespace GammonX.Server.Tests
{
	public class EndGameAndMatchIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly WebApplicationFactory<Program> _factory;

		public EndGameAndMatchIntegrationTests(WebApplicationFactory<Program> factory)
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

					services.AddSingleton<IGameSessionFactory>(new TavliEndGameSessionFactoryStub());
				});
			});
		}

		[Fact]
		public async Task EndGameAndMatchIntegrationTest()
		{
			var client = _factory.CreateClient();
			var serverUri = client.BaseAddress!.ToString().TrimEnd('/');

			var player1 = new
			{
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
			// PLAYERS STARTING THE GAME
			// ##################################################

			player1Connection.On<object>(ServerEventTypes.GameStartedEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventGameStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventGameStatePayload payload)
				{
					if (payload.Modus == GameModus.Portes)
					{
						Assert.Equal(GamePhase.WaitingForRoll, payload.Phase);
					}
					if (payload.Modus == GameModus.Plakoto)
					{
						Assert.Equal(GamePhase.WaitingForRoll, payload.Phase);
					}
					if (payload.Modus == GameModus.Fevga)
					{
						Assert.Equal(GamePhase.WaitingForRoll, payload.Phase);
					}
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventGameStatePayload> but got: " + response?.GetType().Name);
				}
			});

			player2Connection.On<object>(ServerEventTypes.GameStartedEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventGameStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventGameStatePayload payload)
				{
					if (payload.Modus == GameModus.Portes)
					{
						Assert.Equal(GamePhase.WaitingForOpponent, payload.Phase);
					}
					if (payload.Modus == GameModus.Plakoto)
					{
						Assert.Equal(GamePhase.WaitingForOpponent, payload.Phase);
					}
					if (payload.Modus == GameModus.Fevga)
					{
						Assert.Equal(GamePhase.WaitingForOpponent, payload.Phase);
					}
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventGameStatePayload> but got: " + response?.GetType().Name);
				}
			});

			// ##################################################
			// PLAYERS WINING THE GAMES AND THE MATCH
			// ##################################################

			player1Connection.On<object>(ServerEventTypes.GameEndedEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventMatchStatePayload payload)
				{
					if (payload.GameRound == 1)
					{
						Assert.Contains(ServerCommands.StartGameCommand, payload.AllowedCommands);
						Assert.Equal(2, payload.Player1.Score);
						Assert.Equal(0, payload.Player2.Score);
					}
					else if (payload.GameRound == 2)
					{
						Assert.Contains(ServerCommands.StartGameCommand, payload.AllowedCommands);
						Assert.Equal(4, payload.Player1.Score);
						Assert.Equal(0, payload.Player2.Score);
					}
					else if (payload.GameRound == 3)
					{
						Assert.Empty(payload.AllowedCommands);
						Assert.Equal(6, payload.Player1.Score);
						Assert.Equal(0, payload.Player2.Score);
					}
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventMatchStatePayload> but got: " + response?.GetType().Name);
				}
			});

			player2Connection.On<object>(ServerEventTypes.GameEndedEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventMatchStatePayload payload)
				{
					if (payload.GameRound == 1)
					{
						Assert.Contains(ServerCommands.StartGameCommand, payload.AllowedCommands);
						Assert.Equal(2, payload.Player1.Score);
						Assert.Equal(0, payload.Player2.Score);
					}
					else if (payload.GameRound == 2)
					{
						Assert.Contains(ServerCommands.StartGameCommand, payload.AllowedCommands);
						Assert.Equal(4, payload.Player1.Score);
						Assert.Equal(0, payload.Player2.Score);
					}
					else if (payload.GameRound == 3)
					{
						Assert.Empty(payload.AllowedCommands);
						Assert.Equal(6, payload.Player1.Score);
						Assert.Equal(0, payload.Player2.Score);
					}
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventMatchStatePayload> but got: " + response?.GetType().Name);
				}
			});

			var matchEndedForPlayer1 = false;
			player1Connection.On<object>(ServerEventTypes.MatchEndedEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventMatchStatePayload payload)
				{
					Assert.Empty(payload.AllowedCommands);
					Assert.Equal(6, payload.Player1.Score);
					Assert.Equal(0, payload.Player2.Score);
					matchEndedForPlayer1 = true;
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventMatchStatePayload> but got: " + response?.GetType().Name);
				}
			});

			var matchEndedForPlayer2 = false;
			player2Connection.On<object>(ServerEventTypes.MatchEndedEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventMatchStatePayload payload)
				{
					Assert.Empty(payload.AllowedCommands);
					Assert.Equal(6, payload.Player1.Score);
					Assert.Equal(0, payload.Player2.Score);
					matchEndedForPlayer2 = true;
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventMatchStatePayload> but got: " + response?.GetType().Name);
				}
			});


			await player1Connection.StartAsync();
			await player2Connection.StartAsync();

			// join the match
			await player1Connection.InvokeAsync(ServerCommands.JoinMatchCommand, joinPayload1.MatchId.ToString(), player1.PlayerId.ToString());
			await player2Connection.InvokeAsync(ServerCommands.JoinMatchCommand, joinPayload2?.MatchId.ToString(), player2.PlayerId.ToString());

			await Task.Delay(500);

			// start the game
			await player1Connection.InvokeAsync(ServerCommands.StartGameCommand, joinPayload1.MatchId.ToString());
			await player2Connection.InvokeAsync(ServerCommands.StartGameCommand, joinPayload2?.MatchId.ToString());

			await Task.Delay(500);

			// player 1 rolls the dice
			await player1Connection.InvokeAsync(ServerCommands.RollCommand, joinPayload1.MatchId.ToString());

			await Task.Delay(1500);

			// player 1 moves first checker and wins the first game
			await player1Connection.InvokeAsync(ServerCommands.MoveCommand, joinPayload1.MatchId.ToString(), 23, WellKnownBoardPositions.BearOffWhite);

			await Task.Delay(500);

			// start game round 2
			await player1Connection.InvokeAsync(ServerCommands.StartGameCommand, joinPayload1.MatchId.ToString());
			await player2Connection.InvokeAsync(ServerCommands.StartGameCommand, joinPayload2?.MatchId.ToString());

			await Task.Delay(500);

			// player 1 rolls the dice
			await player1Connection.InvokeAsync(ServerCommands.RollCommand, joinPayload1.MatchId.ToString());

			await Task.Delay(1500);

			// player 1 moves first checker and wins the second game
			await player1Connection.InvokeAsync(ServerCommands.MoveCommand, joinPayload1.MatchId.ToString(), 23, WellKnownBoardPositions.BearOffWhite);

			await Task.Delay(500);

			// start game round 3
			await player1Connection.InvokeAsync(ServerCommands.StartGameCommand, joinPayload1.MatchId.ToString());
			await player2Connection.InvokeAsync(ServerCommands.StartGameCommand, joinPayload2?.MatchId.ToString());

			await Task.Delay(500);

			// player 1 rolls the dice
			await player1Connection.InvokeAsync(ServerCommands.RollCommand, joinPayload1.MatchId.ToString());

			await Task.Delay(1500);

			// player 1 moves first checker and wins the third game
			await player1Connection.InvokeAsync(ServerCommands.MoveCommand, joinPayload1.MatchId.ToString(), 23, WellKnownBoardPositions.BearOffWhite);

			// match ended
			await Task.Delay(500);

			while (!matchEndedForPlayer1 || !matchEndedForPlayer2)
			{
				await Task.Delay(200);
			}
		}
	}
}