using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;
using GammonX.Server.Bot;
using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;
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
	public class SimpleBotGameIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly WebApplicationFactory<Program> _factory;

		public SimpleBotGameIntegrationTests(WebApplicationFactory<Program> factory)
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
					descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBotService));
					if (descriptor != null)
					{
						services.Remove(descriptor);
					}
					services.AddSingleton<IBotService>(new SimpleBotService());
				});
			});
		}

		[Fact]
		public async Task StartSimpleBotMatch()
		{
			var client = _factory.CreateClient();
			var serverUri = client.BaseAddress!.ToString().TrimEnd('/');

			var player1 = new JoinRequest(Guid.Parse("fdd907ca-794a-43f4-83e6-cadfabc57c45"), MatchVariant.Tavli, MatchModus.Bot, MatchType.CashGame);
			var response1 = await client.PostAsJsonAsync("/game/api/matches/join", player1);
			var resultJson1 = await response1.Content.ReadAsStringAsync();
			var joinResponse1 = JsonConvert.DeserializeObject<RequestResponseContract<RequestQueueEntryPayload>>(resultJson1);
			var joinPayload1 = joinResponse1?.Payload;
			Assert.NotNull(joinPayload1);
			var player1Connection = new HubConnectionBuilder()
				.WithUrl($"{serverUri}/matchhub", options =>
				{
					options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
				})
				.Build();

			Assert.NotNull(joinPayload1.QueueId);
			RequestQueueEntryPayload? result1 = null;
			do
			{
				result1 = await client.PollAsync(player1.PlayerId, joinPayload1.QueueId.Value, MatchModus.Bot);
			}
			while (result1?.Status == QueueEntryStatus.WaitingForOpponent);

			Assert.NotNull(result1);
			var matchId = result1.MatchId;

			player1Connection.On<object>(ServerEventTypes.ErrorEvent, response =>
			{
				Assert.Fail();
			});

			player1Connection.On<object>(ServerEventTypes.MatchLobbyWaitingEvent, response =>
			{
                // we must not wait if opponent is a bot
                Assert.Fail();
			});

			player1Connection.On<object>(ServerEventTypes.GameWaitingEvent, response =>
			{
				// we must not wait if opponent is a bot
				Assert.Fail();
			});

            player1Connection.On<object>(ServerEventTypes.MatchWaitingEvent, response =>
            {
                // we must not wait if opponent is a bot
                Assert.Fail();
            });

            player1Connection.On<object>(ServerEventTypes.MatchLobbyFoundEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchLobbyPayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchLobbyPayload payload)
                {
					Assert.True(payload.MatchFound);
					Assert.Equal(matchId, payload.Id);
					// we expect the static bot player id as opponent
					Assert.Equal(Guid.Parse("7d7f63ca-112a-4d92-9881-36ee1a66aeb6"), payload.Player2);
					Assert.Empty(payload.AllowedCommands);
                }
            });

            player1Connection.On<object>(ServerEventTypes.MatchWaitingForStartEvent, response =>
            {
				Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchStatePayload payload)
                {
					Assert.Equal(3, payload.AllowedCommands.Length);
					Assert.Contains(ServerCommands.ResignGameCommand, payload.AllowedCommands);
                    Assert.Contains(ServerCommands.ResignMatchCommand, payload.AllowedCommands);
                    Assert.Contains(ServerCommands.StartMatchCommand, payload.AllowedCommands);
					Assert.Equal(matchId, payload.Id);
					Assert.Equal($"match_{matchId}", payload.GroupName);
                    Assert.Equal(1, payload.GameRound);
					Assert.Null(payload.Winner);
					Assert.Null(payload.WinnerPoints);
					Assert.Null(payload.Loser);
					Assert.Null(payload.LoserPoints);
					Assert.Equal(MatchModus.Bot, payload.Modus);
					Assert.NotNull(payload.Player1);
					Assert.NotNull(payload.Player2);
                    Assert.Equal(Guid.Parse("7d7f63ca-112a-4d92-9881-36ee1a66aeb6"), payload.Player2.Id);
                    Assert.Null(payload.Player1.UserName);
                    Assert.Null(payload.Player2.UserName);
                    // we should not have opening dices before the match is started
                    Assert.Null(payload.Player1.StartDiceRoll);
                    Assert.Null(payload.Player2.StartDiceRoll);
                }
            });

            player1Connection.On<object>(ServerEventTypes.MatchStartedEvent, response =>
            {
                Assert.NotNull(response);
                var contract = JsonConvert.DeserializeObject<EventResponseContract<EventMatchStatePayload>>(response.ToString() ?? "");
                if (contract?.Payload is EventMatchStatePayload payload)
                {
                    Assert.Equal(3, payload.AllowedCommands.Length);
                    Assert.Contains(ServerCommands.ResignGameCommand, payload.AllowedCommands);
                    Assert.Contains(ServerCommands.ResignMatchCommand, payload.AllowedCommands);
					// starting player can directly move with the opening dices
                    Assert.Contains(ServerCommands.MoveCommand, payload.AllowedCommands);
                    Assert.Equal(matchId, payload.Id);
                    Assert.Equal(1, payload.GameRound);
                    Assert.Null(payload.Winner);
                    Assert.Null(payload.WinnerPoints);
                    Assert.Null(payload.Loser);
                    Assert.Null(payload.LoserPoints);
                    Assert.Equal(MatchModus.Bot, payload.Modus);
                    Assert.NotNull(payload.Player1);
                    Assert.NotNull(payload.Player2);
                    Assert.Equal(Guid.Parse("7d7f63ca-112a-4d92-9881-36ee1a66aeb6"), payload.Player2.Id);
                    Assert.Null(payload.Player1.UserName);
                    Assert.Null(payload.Player2.UserName);
                    // we should now have opening dices for both players
                    Assert.NotNull(payload.Player1.StartDiceRoll);
                    Assert.NotNull(payload.Player2.StartDiceRoll);
                }
            });

            MoveModel? nextMove = null;
			player1Connection.On<object>(ServerEventTypes.GameStartedEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventGameStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventGameStatePayload payload)
				{
					nextMove = payload.MoveSequences.SelectMany(ms => ms.Moves)?.FirstOrDefault();
					Assert.Equal(matchId, payload.MatchId);
					if (payload.Phase == GamePhase.WaitingForOpponent)
					{
						Assert.Equal(2, payload.TurnNumber);
						Assert.NotEqual(player1.PlayerId, payload.ActiveTurn);
					}

					if (payload.AllowedCommands.Contains(ServerCommands.RollCommand))
					{
						Assert.Equal(player1.PlayerId, payload.ActiveTurn);
					}
				}
			});

			await player1Connection.StartAsync();

			// join the match
			await player1Connection.SendAsync(ServerCommands.JoinMatchCommand, matchId, player1.PlayerId.ToString());

			// start the game
			await player1Connection.SendAsync(ServerCommands.StartMatchCommand, matchId);

			// player 1 can directly move with the initial rolled dices
			// await player1Connection.SendAsync(ServerCommands.RollCommand, matchId);

			while (nextMove == null)
			{
				await Task.Delay(250);
			}

			// player 1 moves first checker
			await player1Connection.SendAsync(ServerCommands.MoveCommand, matchId, nextMove.From, nextMove.To);

			// player 1 moves second checker
			await player1Connection.SendAsync(ServerCommands.MoveCommand, matchId, nextMove.From, nextMove.To);

			// player 1 ends his turn
			await player1Connection.SendAsync(ServerCommands.EndTurnCommand, matchId);

			// bot has its turn

			// player 1 rolls for his second turn
			await player1Connection.SendAsync(ServerCommands.RollCommand, matchId);

			await player1Connection.DisposeAsync();
			client.Dispose();
		}
	}
}
