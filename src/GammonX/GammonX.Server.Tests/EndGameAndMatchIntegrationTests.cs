using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Contracts;
using GammonX.Server.EntityFramework.Entities;
using GammonX.Server.EntityFramework.Services;
using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Stubs;
using GammonX.Server.Tests.Utils;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using Newtonsoft.Json;

using System.Net.Http.Json;

namespace GammonX.Server.Tests
{
	public class EndGameAndMatchIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly WebApplicationFactory<Program> _factory;
		private readonly Guid _player1Id = Guid.Parse("fdd907ca-794a-43f4-83e6-cadfabc57c45");
		private readonly Guid _player2Id = Guid.Parse("f6f9bb06-cbf6-4f42-80bf-5d62be34cff6");

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
					descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDiceServiceFactory));
					if (descriptor != null)
					{
						services.Remove(descriptor);
					}
					services.AddSingleton<IDiceServiceFactory>(new StartDiceServiceFactoryStub());
					descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPlayerService));
					if (descriptor != null)
					{
						services.Remove(descriptor);
					}
					Mock<IPlayerService> service = new();
					service.Setup(x => x.GetWithRatingAsync(_player1Id, default)).Returns(() => Task.FromResult(new Player { Id = _player1Id }));
					service.Setup(x => x.GetWithRatingAsync(_player2Id, default)).Returns(() => Task.FromResult(new Player { Id = _player2Id }));
					services.AddSingleton<IPlayerService>(service.Object);
				});
			});
		}

		[Theory]
		[InlineData(WellKnownMatchModus.Normal)]
		[InlineData(WellKnownMatchModus.Ranked)]
		public async Task EndGameAndMatchIntegrationTest(WellKnownMatchModus modus)
		{
			var client = _factory.CreateClient();
			var serverUri = client.BaseAddress!.ToString().TrimEnd('/');

			var player1 = new JoinRequest(_player1Id, WellKnownMatchVariant.Tavli, modus, WellKnownMatchType.CashGame);
			var response1 = await client.PostAsJsonAsync("/api/matches/join", player1);
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

			var player2 = new JoinRequest(_player2Id, WellKnownMatchVariant.Tavli, modus, WellKnownMatchType.CashGame);
			var response2 = await client.PostAsJsonAsync("/api/matches/join", player2);
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
				result1 = await client.PollAsync(player1.PlayerId, joinPayload1.QueueId.Value, modus);
			}
			while (result1?.Status == QueueEntryStatus.WaitingForOpponent);

			do
			{
				result2 = await client.PollAsync(player2.PlayerId, joinPayload2.QueueId.Value, modus);
			}
			while (result2?.Status == QueueEntryStatus.WaitingForOpponent);

			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.Equal(result1.MatchId, result2.MatchId);
			var matchId = result1.MatchId;

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
						Assert.Equal(2, payload.Player1?.Score);
						Assert.Equal(0, payload.Player2?.Score);
						Assert.NotNull(payload.GameRounds);
						Assert.Single(payload.GameRounds);
						Assert.Equal(GameModus.Portes, payload.GameRounds[0].Modus);
						Assert.Equal(GamePhase.GameOver, payload.GameRounds[0].Phase);
						Assert.Equal(2, payload.GameRounds[0].Score);
						Assert.Equal(0, payload.GameRounds[0].GameRoundIndex);
						Assert.Equal(player1.PlayerId, payload.GameRounds[0].Winner);
						Assert.Equal(payload.Player1?.Id, payload.GameRounds[0].Winner);
					}
					else if (payload.GameRound == 2)
					{
						Assert.Contains(ServerCommands.StartGameCommand, payload.AllowedCommands);
						Assert.Equal(4, payload.Player1?.Score);
						Assert.Equal(0, payload.Player2?.Score);
						Assert.NotNull(payload.GameRounds);
						Assert.Equal(2, payload.GameRounds.Length);
						Assert.Equal(GameModus.Plakoto, payload.GameRounds[1].Modus);
						Assert.Equal(GamePhase.GameOver, payload.GameRounds[1].Phase);
						Assert.Equal(2, payload.GameRounds[1].Score);
						Assert.Equal(1, payload.GameRounds[1].GameRoundIndex);
						Assert.Equal(player1.PlayerId, payload.GameRounds[1].Winner);
						Assert.Equal(payload.Player1?.Id, payload.GameRounds[1].Winner);
					}
					else if (payload.GameRound == 3)
					{
						Assert.Empty(payload.AllowedCommands);
						Assert.Equal(6, payload.Player1?.Score);
						Assert.Equal(0, payload.Player2?.Score);
						Assert.NotNull(payload.GameRounds);
						Assert.Equal(3, payload.GameRounds.Length);
						Assert.Equal(GameModus.Fevga, payload.GameRounds[2].Modus);
						Assert.Equal(GamePhase.GameOver, payload.GameRounds[2].Phase);
						Assert.Equal(2, payload.GameRounds[2].Score);
						Assert.Equal(2, payload.GameRounds[2].GameRoundIndex);
						Assert.Equal(player1.PlayerId, payload.GameRounds[2].Winner);
						Assert.Equal(payload.Player1?.Id, payload.GameRounds[2].Winner);
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
						Assert.Equal(2, payload.Player1?.Score);
						Assert.Equal(0, payload.Player2?.Score);
						Assert.NotNull(payload.GameRounds);
						Assert.Single(payload.GameRounds);
						Assert.Equal(GameModus.Portes, payload.GameRounds[0].Modus);
						Assert.Equal(GamePhase.GameOver, payload.GameRounds[0].Phase);
						Assert.Equal(2, payload.GameRounds[0].Score);
						Assert.Equal(0, payload.GameRounds[0].GameRoundIndex);
						Assert.Equal(player1.PlayerId, payload.GameRounds[0].Winner);
						Assert.Equal(payload.Player1?.Id, payload.GameRounds[0].Winner);
					}
					else if (payload.GameRound == 2)
					{
						Assert.Contains(ServerCommands.StartGameCommand, payload.AllowedCommands);
						Assert.Equal(4, payload.Player1?.Score);
						Assert.Equal(0, payload.Player2?.Score);
						Assert.NotNull(payload.GameRounds);
						Assert.Equal(2, payload.GameRounds.Length);
						Assert.Equal(GameModus.Plakoto, payload.GameRounds[1].Modus);
						Assert.Equal(GamePhase.GameOver, payload.GameRounds[1].Phase);
						Assert.Equal(2, payload.GameRounds[1].Score);
						Assert.Equal(1, payload.GameRounds[1].GameRoundIndex);
						Assert.Equal(player1.PlayerId, payload.GameRounds[1].Winner);
						Assert.Equal(payload.Player1?.Id, payload.GameRounds[1].Winner);
					}
					else if (payload.GameRound == 3)
					{
						Assert.Empty(payload.AllowedCommands);
						Assert.Equal(6, payload.Player1?.Score);
						Assert.Equal(0, payload.Player2?.Score);
						Assert.NotNull(payload.GameRounds);
						Assert.Equal(3, payload.GameRounds.Length);
						Assert.Equal(GameModus.Fevga, payload.GameRounds[2].Modus);
						Assert.Equal(GamePhase.GameOver, payload.GameRounds[2].Phase);
						Assert.Equal(2, payload.GameRounds[2].Score);
						Assert.Equal(2, payload.GameRounds[2].GameRoundIndex);
						Assert.Equal(player1.PlayerId, payload.GameRounds[2].Winner);
						Assert.Equal(payload.Player1?.Id, payload.GameRounds[2].Winner);
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
					Assert.Equal(6, payload.Player1?.Score);
					Assert.Equal(0, payload.Player2?.Score);
					Assert.NotNull(payload.GameRounds);
					Assert.Equal(3, payload.GameRounds.Length);
					Assert.Equal(GameModus.Portes, payload.GameRounds[0].Modus);
					Assert.Equal(GamePhase.GameOver, payload.GameRounds[0].Phase);
					Assert.Equal(2, payload.GameRounds[0].Score);
					Assert.Equal(0, payload.GameRounds[0].GameRoundIndex);
					Assert.Equal(player1.PlayerId, payload.GameRounds[0].Winner);
					Assert.Equal(payload.Player1?.Id, payload.GameRounds[0].Winner);
					Assert.Equal(GameModus.Plakoto, payload.GameRounds[1].Modus);
					Assert.Equal(GamePhase.GameOver, payload.GameRounds[1].Phase);
					Assert.Equal(2, payload.GameRounds[1].Score);
					Assert.Equal(1, payload.GameRounds[1].GameRoundIndex);
					Assert.Equal(player1.PlayerId, payload.GameRounds[1].Winner);
					Assert.Equal(payload.Player1?.Id, payload.GameRounds[1].Winner);
					Assert.Equal(GameModus.Fevga, payload.GameRounds[2].Modus);
					Assert.Equal(GamePhase.GameOver, payload.GameRounds[2].Phase);
					Assert.Equal(2, payload.GameRounds[2].Score);
					Assert.Equal(2, payload.GameRounds[2].GameRoundIndex);
					Assert.Equal(player1.PlayerId, payload.GameRounds[2].Winner);
					Assert.Equal(payload.Player1?.Id, payload.GameRounds[2].Winner);
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
					Assert.Equal(6, payload.Player1?.Score);
					Assert.Equal(0, payload.Player2?.Score);
					Assert.NotNull(payload.GameRounds);
					Assert.Equal(3, payload.GameRounds.Length);
					Assert.Equal(GameModus.Portes, payload.GameRounds[0].Modus);
					Assert.Equal(GamePhase.GameOver, payload.GameRounds[0].Phase);
					Assert.Equal(2, payload.GameRounds[0].Score);
					Assert.Equal(0, payload.GameRounds[0].GameRoundIndex);
					Assert.Equal(player1.PlayerId, payload.GameRounds[0].Winner);
					Assert.Equal(payload.Player1?.Id, payload.GameRounds[0].Winner);
					Assert.Equal(GameModus.Plakoto, payload.GameRounds[1].Modus);
					Assert.Equal(GamePhase.GameOver, payload.GameRounds[1].Phase);
					Assert.Equal(2, payload.GameRounds[1].Score);
					Assert.Equal(1, payload.GameRounds[1].GameRoundIndex);
					Assert.Equal(player1.PlayerId, payload.GameRounds[1].Winner);
					Assert.Equal(payload.Player1?.Id, payload.GameRounds[1].Winner);
					Assert.Equal(GameModus.Fevga, payload.GameRounds[2].Modus);
					Assert.Equal(GamePhase.GameOver, payload.GameRounds[2].Phase);
					Assert.Equal(2, payload.GameRounds[2].Score);
					Assert.Equal(2, payload.GameRounds[2].GameRoundIndex);
					Assert.Equal(player1.PlayerId, payload.GameRounds[2].Winner);
					Assert.Equal(payload.Player1?.Id, payload.GameRounds[2].Winner);
					matchEndedForPlayer2 = true;
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventMatchStatePayload> but got: " + response?.GetType().Name);
				}
			});

			// ##################################################
			// PLAYERS RECEIVE DISCONNECT EVENT
			// ##################################################

			var player1MustDisconnect = false;
			player1Connection.On<object>(ServerEventTypes.ForceDisconnect, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EmptyEventPayload>>(response.ToString() ?? "");
				if (contract?.Payload is EmptyEventPayload payload)
				{
					Assert.Empty(payload.AllowedCommands);
				}
				Assert.Equal(HubConnectionState.Connected, player1Connection.State);
				player1Connection.StopAsync().ContinueWith(t =>
				{
					player1MustDisconnect = true;
				});
			});

			var player2MustDisconnect = false;
			player2Connection.On<object>(ServerEventTypes.ForceDisconnect, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EmptyEventPayload>>(response.ToString() ?? "");
				if (contract?.Payload is EmptyEventPayload payload)
				{
					Assert.Empty(payload.AllowedCommands);
				}
				Assert.Equal(HubConnectionState.Connected, player2Connection.State);
				player2Connection.StopAsync().ContinueWith(t =>
				{
					player2MustDisconnect = true;
				});
			});

			await player1Connection.StartAsync();
			await player2Connection.StartAsync();

			// join the match
			await player1Connection.InvokeAsync(ServerCommands.JoinMatchCommand, matchId, player1.PlayerId.ToString());
			await player2Connection.InvokeAsync(ServerCommands.JoinMatchCommand, matchId, player2.PlayerId.ToString());

			await Task.Delay(500);

			// start the game
			await player1Connection.InvokeAsync(ServerCommands.StartGameCommand, matchId);
			await player2Connection.InvokeAsync(ServerCommands.StartGameCommand, matchId);

			await Task.Delay(500);

			// player 1 rolls the dice
			await player1Connection.InvokeAsync(ServerCommands.RollCommand, matchId);

			await Task.Delay(1500);

			// player 1 moves first checker and wins the first game
			await player1Connection.InvokeAsync(ServerCommands.MoveCommand, matchId, 23, WellKnownBoardPositions.BearOffWhite);

			await Task.Delay(500);

			// start game round 2
			await player1Connection.InvokeAsync(ServerCommands.StartGameCommand, matchId);
			await player2Connection.InvokeAsync(ServerCommands.StartGameCommand, matchId);

			await Task.Delay(500);

			// player 1 rolls the dice
			await player1Connection.InvokeAsync(ServerCommands.RollCommand, matchId);

			await Task.Delay(1500);

			// player 1 moves first checker and wins the second game
			await player1Connection.InvokeAsync(ServerCommands.MoveCommand, matchId, 23, WellKnownBoardPositions.BearOffWhite);

			await Task.Delay(500);

			// start game round 3
			await player1Connection.InvokeAsync(ServerCommands.StartGameCommand, matchId);
			await player2Connection.InvokeAsync(ServerCommands.StartGameCommand, matchId);

			await Task.Delay(500);

			// player 1 rolls the dice
			await player1Connection.InvokeAsync(ServerCommands.RollCommand, matchId);

			await Task.Delay(1500);

			// player 1 moves first checker and wins the third game
			await player1Connection.InvokeAsync(ServerCommands.MoveCommand, matchId, 23, WellKnownBoardPositions.BearOffWhite);

			// match ended
			await Task.Delay(500);

			while (!matchEndedForPlayer1 || !matchEndedForPlayer2)
			{
				await Task.Delay(200);
			}

			while (!player1MustDisconnect || !player2MustDisconnect)
			{
				await Task.Delay(200);
			}

			Assert.Equal(HubConnectionState.Disconnected, player1Connection.State);
			Assert.Equal(HubConnectionState.Disconnected, player2Connection.State);
		}
	}
}