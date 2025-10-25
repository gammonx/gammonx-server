using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Server.Analysis;
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
	public class StartMatchAndGameIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly WebApplicationFactory<Program> _factory;
		private readonly Guid _player1Id = Guid.Parse("fdd907ca-794a-43f4-83e6-cadfabc57c45");
		private readonly Guid _player2Id = Guid.Parse("f6f9bb06-cbf6-4f42-80bf-5d62be34cff6");

		private static volatile bool _player1EndedHisTurn;

		public StartMatchAndGameIntegrationTests(WebApplicationFactory<Program> factory)
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
					descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IPlayerService));
					if (descriptor != null)
					{
						services.Remove(descriptor);
					}
					Mock<IPlayerService> service = new();
					service.Setup(x => x.GetWithRatingAsync(_player1Id, default)).Returns(() => Task.FromResult(new Player { Id = _player1Id }));
					service.Setup(x => x.GetWithRatingAsync(_player2Id, default)).Returns(() => Task.FromResult(new Player { Id = _player2Id }));
					services.AddSingleton(service.Object);
				});
			});
		}

		[Theory]
		[InlineData(WellKnownMatchModus.Normal)]
		[InlineData(WellKnownMatchModus.Ranked)]
		public async Task StartMatchAndGameIntegrationTest(WellKnownMatchModus modus)
		{
			var client = _factory.CreateClient();
			var serverUri = client.BaseAddress!.ToString().TrimEnd('/');

			var player1 = new JoinRequest(_player1Id, WellKnownMatchVariant.Tavli, modus, WellKnownMatchType.CashGame);
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

			var player2 = new JoinRequest(_player2Id, WellKnownMatchVariant.Tavli, modus, WellKnownMatchType.CashGame);
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
			var player1Waiting = false;
			player1Connection.On<object>(ServerEventTypes.MatchLobbyWaitingEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchLobbyPayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventMatchLobbyPayload payload)
				{
					Assert.Equal(joinPayload1.MatchId, payload.Id);
					Assert.False(payload.MatchFound, "Player 1 should not have a match found yet.");
					Assert.Equal(player1.PlayerId, payload.Player1);
					Assert.Null(payload.Player2);
					Assert.Empty(payload.AllowedCommands);
					player1Waiting = true;
				}
			});

			player2Connection.On<object>(ServerEventTypes.MatchLobbyWaitingEvent, response =>
			{
				Assert.Fail();
			});

			var player1MatchFound = false;
			player1Connection.On<object>(ServerEventTypes.MatchLobbyFoundEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchLobbyPayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventMatchLobbyPayload payload)
				{
					Assert.Equal(joinPayload1.MatchId, payload.Id);
					Assert.Equal(player1.PlayerId, payload.Player1);
					Assert.Equal(player2.PlayerId, payload.Player2);
					Assert.NotNull(payload.Player2);
					Assert.True(payload.MatchFound);
					Assert.Empty(payload.AllowedCommands);
					player1MatchFound = true;
				}				
			});

			var player2MatchFound = false;
			player2Connection.On<object>(ServerEventTypes.MatchLobbyFoundEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchLobbyPayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventMatchLobbyPayload payload)
				{
					Assert.Equal(joinPayload1.MatchId, payload.Id);
					Assert.Equal(player1.PlayerId, payload.Player1);
					Assert.Equal(player2.PlayerId, payload.Player2);
					Assert.NotNull(payload.Player2);
					Assert.True(payload.MatchFound);
					Assert.Empty(payload.AllowedCommands);
					player2MatchFound = true;
				}
			});

			var player1MatchStarted = false;
			player1Connection.On<object>(ServerEventTypes.MatchStartedEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventMatchStatePayload payload)
				{
					Assert.Equal(joinPayload1.MatchId, payload.Id);
					Assert.Equal(player1.PlayerId, payload.Player1?.Id);
					Assert.Equal(player2.PlayerId, payload.Player2?.Id);
					Assert.Equal(1, payload.GameRound);
					Assert.NotNull(payload.Player2);
					Assert.NotNull(payload.GameRounds);
					Assert.Empty(payload.GameRounds);
					Assert.Contains(ServerCommands.StartGameCommand, payload.AllowedCommands);
					player1MatchStarted = true;
				}
			});

			var player2MatchStarted = false;
			player2Connection.On<object>(ServerEventTypes.MatchStartedEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventMatchStatePayload payload)
				{
					Assert.Equal(joinPayload1.MatchId, payload.Id);
					Assert.Equal(player1.PlayerId, payload.Player1?.Id);
					Assert.Equal(player2.PlayerId, payload.Player2?.Id);
					Assert.Equal(1, payload.GameRound);
					Assert.NotNull(payload.Player2);
					Assert.NotNull(payload.GameRounds);
					Assert.Empty(payload.GameRounds);
					Assert.Contains(ServerCommands.StartGameCommand, payload.AllowedCommands);
					player2MatchStarted = true;
				}
			});

			// ##################################################
			// PLAYERS STARTING THE GAME
			// ##################################################
			var player1GameWaiting = false;
			player1Connection.On<object>(ServerEventTypes.GameWaitingEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventMatchStatePayload payload)
				{
					Assert.Equal(joinPayload1.MatchId, payload.Id);
					Assert.Equal(player1.PlayerId, payload.Player1?.Id);
					Assert.Empty(payload.AllowedCommands);
					Assert.NotNull(payload.GameRounds);
					Assert.Empty(payload.GameRounds);
					player1GameWaiting = true;
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventMatchStatePayload> but got: " + response?.GetType().Name);
				}
			});

			player2Connection.On<object>(ServerEventTypes.GameWaitingEvent, response =>
			{
				Assert.Fail();
			});

			var gameId = Guid.Empty;

			var player1GameStarted = false;
			player1Connection.On<object>(ServerEventTypes.GameStartedEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventGameStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventGameStatePayload payload)
				{
					Assert.NotEqual(Guid.Empty, payload.Id);
					gameId = payload.Id;
					Assert.Equal(player1.PlayerId, payload.ActiveTurn);
					Assert.Equal(GamePhase.WaitingForRoll, payload.Phase);
					Assert.Equal(1, payload.TurnNumber);
					Assert.Empty(payload.DiceRolls);
					Assert.Empty(payload.MoveSequences);
					Assert.NotNull(payload.BoardState);
					Assert.Contains(ServerCommands.RollCommand, payload.AllowedCommands);
					Assert.DoesNotContain(ServerCommands.MoveCommand, payload.AllowedCommands);
					Assert.DoesNotContain(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					player1GameStarted = true;
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventGameStatePayload> but got: " + response?.GetType().Name);
				}
			});

			var player2GameStarted = false;
			player2Connection.On<object>(ServerEventTypes.GameStartedEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventGameStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventGameStatePayload payload)
				{
					Assert.NotEqual(Guid.Empty, payload.Id);
					gameId = payload.Id;
					Assert.NotEqual(player2.PlayerId, payload.ActiveTurn);
					Assert.Equal(GamePhase.WaitingForOpponent, payload.Phase);
					Assert.Equal(1, payload.TurnNumber);
					Assert.Empty(payload.DiceRolls);
					Assert.Empty(payload.MoveSequences);
					Assert.NotNull(payload.BoardState);
					Assert.DoesNotContain(ServerCommands.RollCommand, payload.AllowedCommands);
					Assert.DoesNotContain(ServerCommands.MoveCommand, payload.AllowedCommands);
					Assert.DoesNotContain(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					player2GameStarted = true;
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventGameStatePayload> but got: " + response?.GetType().Name);
				}
			});

			// ##################################################
			// PLAYERS ROLLING THE DICES, MOVING AND END TURNS
			// ##################################################

			var player1RolledTheDice = false;
			DiceRollContract? player1FirstDiceRoll = null;
			MoveModel? player1FirstRollMove = null;
			DiceRollContract? player1SecondDiceRoll = null;
			MoveModel? player1SecondRollMove = null;
			var player1FirstMove = false;
			var player1SecondMove = false;

			var player2StartedTurn = false;
			var player2RolledTheDice = false;
			DiceRollContract? player2FirstDiceRoll = null;
			MoveModel? player2FirstRollMove = null;
			DiceRollContract? player2SecondDiceRoll = null;
			var player2FirstMove = false;
			var player2EndedHisTurn = false;

			player1Connection.On<object>(ServerEventTypes.GameStateEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventGameStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventGameStatePayload payload)
				{
					if (!player1RolledTheDice)
					{
						Assert.Equal(gameId, payload.Id);
						Assert.Equal(player1.PlayerId, payload.ActiveTurn);
						Assert.Equal(GamePhase.Rolling, payload.Phase);
						Assert.Equal(1, payload.TurnNumber);
						Assert.NotEmpty(payload.DiceRolls);
						Assert.True(payload.DiceRolls.Length >= 2);
						player1FirstDiceRoll = payload.DiceRolls[0];
						player1SecondDiceRoll = payload.DiceRolls[1];
						Assert.NotEmpty(payload.MoveSequences);
						player1FirstRollMove = payload.MoveSequences.SelectMany(ms => ms.Moves).First(lm => Math.Abs(lm.From - lm.To) == player1FirstDiceRoll.Roll);
						player1RolledTheDice = true;
						Assert.NotNull(payload.BoardState);
						Assert.DoesNotContain(ServerCommands.RollCommand, payload.AllowedCommands);
						Assert.Contains(ServerCommands.MoveCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					}
					else if (!player1FirstMove)
					{
						Assert.Equal(player1.PlayerId, payload.ActiveTurn);
						Assert.Equal(GamePhase.Moving, payload.Phase);
						Assert.NotEmpty(payload.DiceRolls);
						Assert.True(payload.DiceRolls.Length >= 2);
						Assert.True(payload.DiceRolls[0].Used);
						Assert.False(payload.DiceRolls[1].Used);
						Assert.NotEmpty(payload.MoveSequences);
						Assert.NotNull(player1SecondDiceRoll);
						player1SecondRollMove = payload.MoveSequences.SelectMany(ms => ms.Moves).First(lm => Math.Abs(lm.From - lm.To) == player1SecondDiceRoll.Roll);
						player1FirstMove = true;
						Assert.NotNull(payload.BoardState);
						Assert.DoesNotContain(ServerCommands.RollCommand, payload.AllowedCommands);
						Assert.Contains(ServerCommands.MoveCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					}
					else if (!player1SecondMove)
					{
						player1SecondMove = true;
						Assert.Equal(player1.PlayerId, payload.ActiveTurn);
						Assert.Equal(GamePhase.WaitingForEndTurn, payload.Phase);
						Assert.NotEmpty(payload.DiceRolls);
						Assert.True(payload.DiceRolls.Length >= 2);
						Assert.True(payload.DiceRolls[0].Used);
						Assert.True(payload.DiceRolls[1].Used);
						Assert.Empty(payload.MoveSequences);
						Assert.NotNull(payload.BoardState);
						Assert.DoesNotContain(ServerCommands.RollCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.MoveCommand, payload.AllowedCommands);
						Assert.Contains(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					}
					else if (!_player1EndedHisTurn)
					{
						_player1EndedHisTurn = true;
						Assert.NotEqual(player1.PlayerId, payload.ActiveTurn);
						Assert.Equal(2, payload.TurnNumber);
						Assert.Equal(GamePhase.WaitingForOpponent, payload.Phase);
						Assert.Empty(payload.DiceRolls);
						Assert.Empty(payload.MoveSequences);
						Assert.NotNull(payload.BoardState);
						Assert.DoesNotContain(ServerCommands.RollCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.MoveCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					}
					else if (!player2EndedHisTurn)
					{
						// player 2 turn
						Assert.Equal(player2.PlayerId, payload.ActiveTurn);
						Assert.NotEqual(player1.PlayerId, payload.ActiveTurn);
						Assert.Equal(2, payload.TurnNumber);
						Assert.Equal(GamePhase.WaitingForOpponent, payload.Phase);
						Assert.DoesNotContain(ServerCommands.RollCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.MoveCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					}
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventGameStatePayload> but got: " + response?.GetType().Name);
				}
			});

			player2Connection.On<object>(ServerEventTypes.GameStateEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventGameStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventGameStatePayload payload)
				{
					if (_player1EndedHisTurn && !player2StartedTurn)
					{
						player2StartedTurn = true;
						Assert.Equal(gameId, payload.Id);
						Assert.Equal(player2.PlayerId, payload.ActiveTurn);
						Assert.Equal(GamePhase.WaitingForRoll, payload.Phase);
						Assert.Equal(2, payload.TurnNumber);
						Assert.Empty(payload.DiceRolls);
						Assert.Empty(payload.MoveSequences);
						Assert.NotNull(payload.BoardState);
						Assert.Contains(ServerCommands.RollCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.MoveCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					}
					else if (_player1EndedHisTurn && !player2RolledTheDice)
					{
						Assert.Equal(gameId, payload.Id);
						Assert.Equal(player2.PlayerId, payload.ActiveTurn);
						Assert.Equal(GamePhase.Rolling, payload.Phase);
						Assert.Equal(2, payload.TurnNumber);
						Assert.NotEmpty(payload.DiceRolls);
						Assert.True(payload.DiceRolls.Length >= 2);
						player2FirstDiceRoll = payload.DiceRolls[0];
						player2SecondDiceRoll = payload.DiceRolls[1];
						Assert.NotEmpty(payload.MoveSequences);
						// execute a combined dice roll
						var moveSeq = payload.MoveSequences.Select(ms => ms.Moves).First(ms => Math.Abs(ms[0].From - ms[1].To) == player2FirstDiceRoll.Roll + player2SecondDiceRoll.Roll);
						player2FirstRollMove = new MoveModel(moveSeq[0].From, moveSeq[1].To);
						player2RolledTheDice = true;
						Assert.NotNull(payload.BoardState);
						Assert.DoesNotContain(ServerCommands.RollCommand, payload.AllowedCommands);
						Assert.Contains(ServerCommands.MoveCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					}
					else if (_player1EndedHisTurn && !player2FirstMove)
					{
						player2FirstMove = true;
						Assert.Equal(player2.PlayerId, payload.ActiveTurn);
						// both dices were used in the rolling phase, no moving phase
						Assert.Equal(GamePhase.WaitingForEndTurn, payload.Phase);
						Assert.NotEmpty(payload.DiceRolls);
						Assert.True(payload.DiceRolls.Length >= 2);
						Assert.True(payload.DiceRolls[0].Used);
						Assert.True(payload.DiceRolls[1].Used);
						Assert.Empty(payload.MoveSequences);
						Assert.NotNull(payload.BoardState);
						Assert.DoesNotContain(ServerCommands.RollCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.MoveCommand, payload.AllowedCommands);
						Assert.Contains(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					}
					else if (_player1EndedHisTurn && !player2EndedHisTurn)
					{
						player2EndedHisTurn = true;
						Assert.NotEqual(player2.PlayerId, payload.ActiveTurn);
						Assert.Equal(player1.PlayerId, payload.ActiveTurn);
						Assert.Equal(3, payload.TurnNumber);
						Assert.Equal(GamePhase.WaitingForOpponent, payload.Phase);
						Assert.Empty(payload.DiceRolls);
						Assert.Empty(payload.MoveSequences);
						Assert.NotNull(payload.BoardState);
						Assert.DoesNotContain(ServerCommands.RollCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.MoveCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					}
					else if (!_player1EndedHisTurn && !player2StartedTurn)
					{
						// player 1 turn
						Assert.Equal(player1.PlayerId, payload.ActiveTurn);
						Assert.NotEqual(player2.PlayerId, payload.ActiveTurn);
						Assert.Equal(GamePhase.WaitingForOpponent, payload.Phase);
						Assert.DoesNotContain(ServerCommands.RollCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.MoveCommand, payload.AllowedCommands);
						Assert.DoesNotContain(ServerCommands.EndTurnCommand, payload.AllowedCommands);
					}
				}
				else
				{
					Assert.Fail("Expected EventResponseContract<EventGameStatePayload> but got: " + response?.GetType().Name);
				}
			});

			await player1Connection.StartAsync();
			await player2Connection.StartAsync();

			// join the match
			await player1Connection.InvokeAsync(ServerCommands.JoinMatchCommand, matchId, player1.PlayerId.ToString());
			await player2Connection.InvokeAsync(ServerCommands.JoinMatchCommand, matchId, player2.PlayerId.ToString());

			while(!player1Waiting || !player1MatchFound || !player1MatchStarted || !player2MatchFound || !player2MatchStarted)
			{
				await Task.Delay(100);
			}

			Assert.True(player1Waiting);
			Assert.True(player1MatchFound);
			Assert.True(player1MatchStarted);
			Assert.True(player2MatchFound);
			Assert.True(player2MatchStarted);

			// start the game
			await player1Connection.InvokeAsync(ServerCommands.StartGameCommand, matchId);
			await player2Connection.InvokeAsync(ServerCommands.StartGameCommand, matchId);

			while(!player1GameWaiting || !player1GameStarted || !player2GameStarted)
			{
				await Task.Delay(100);
			}

			Assert.True(player1GameWaiting);
			Assert.True(player1GameStarted);
			Assert.True(player2GameStarted);
			Assert.NotEqual(Guid.Empty, gameId);

			// player 1 rolls the dice
			await player1Connection.InvokeAsync(ServerCommands.RollCommand, matchId);

			while (!player1RolledTheDice)
			{
				await Task.Delay(100);
			}

			Assert.True(player1RolledTheDice);
			Assert.NotNull(player1FirstDiceRoll);
			Assert.NotNull(player1SecondDiceRoll);
			Assert.NotNull(player1FirstRollMove);

			// player 1 moves first checker
			await player1Connection.InvokeAsync(ServerCommands.MoveCommand, matchId, player1FirstRollMove.From, player1FirstRollMove.To);

			while (!player1FirstMove)
			{
				await Task.Delay(100);
			}

			Assert.True(player1FirstMove);
			Assert.NotNull(player1SecondRollMove);

			// player 1 moves second checker
			await player1Connection.InvokeAsync(ServerCommands.MoveCommand, matchId, player1SecondRollMove.From, player1SecondRollMove.To);

			while (!player1SecondMove)
			{
				await Task.Delay(100);
			}

			Assert.True(player1SecondMove);

			// player 1 ends his turn
			await player1Connection.InvokeAsync(ServerCommands.EndTurnCommand, matchId);
			// we set it here explicitly because the timing does not quite match up
			_player1EndedHisTurn = true;

			while (!player2StartedTurn)
			{
				await Task.Delay(100);
			}

			Assert.True(_player1EndedHisTurn);
			Assert.True(player2StartedTurn);

			// player 2 rolls the dice
			await player2Connection.InvokeAsync(ServerCommands.RollCommand, matchId);

			while (!player2RolledTheDice)
			{
				await Task.Delay(100);
			}

			Assert.True(player2RolledTheDice);
			Assert.NotNull(player2FirstDiceRoll);
			Assert.NotNull(player2SecondDiceRoll);
			Assert.NotNull(player2FirstRollMove);

			// player 1 moves a checker with both dice rolls
			await player2Connection.InvokeAsync(ServerCommands.MoveCommand, joinPayload1.MatchId.ToString(), player2FirstRollMove.From, player2FirstRollMove.To);

			while (!player2FirstMove)
			{
				await Task.Delay(100);
			}

			Assert.True(player2FirstMove);

			// player 2 ends his turn
			await player2Connection.InvokeAsync(ServerCommands.EndTurnCommand, joinPayload1.MatchId.ToString());
			// we set it here explicitly because the timing does not quite match up
			player2EndedHisTurn = true;

			while (!player2EndedHisTurn)
			{
				await Task.Delay(100);
			}

			Assert.True(player2EndedHisTurn);

			await player1Connection.DisposeAsync();
			await player2Connection.DisposeAsync();
			client.Dispose();
		}
	}
}