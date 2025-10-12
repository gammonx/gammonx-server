using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Server.Analysis;
using GammonX.Server.Bot;
using GammonX.Server.Contracts;
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
					descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMatchAnalysisQueue));
					if (descriptor != null)
					{
						services.Remove(descriptor);
					}
					Mock<IMatchAnalysisQueue> analysisQueue = new();
					analysisQueue.Setup(x => x.EnqueueAsync(It.IsAny<MatchAnalysisJob>())).Returns(new ValueTask());
					analysisQueue.Setup(x => x.DequeueAsync(It.IsAny<CancellationToken>())).Returns(new ValueTask<MatchAnalysisJob>());
					services.AddSingleton(analysisQueue.Object);
				});
			});
		}

		[Fact]
		public async Task StartSimpleBotMatch()
		{
			var client = _factory.CreateClient();
			var serverUri = client.BaseAddress!.ToString().TrimEnd('/');

			var player1 = new JoinRequest(Guid.Parse("fdd907ca-794a-43f4-83e6-cadfabc57c45"), WellKnownMatchVariant.Tavli, WellKnownMatchModus.Bot, WellKnownMatchType.CashGame);
			var response1 = await client.PostAsJsonAsync("/api/matches/join", player1);
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
				result1 = await client.PollAsync(player1.PlayerId, joinPayload1.QueueId.Value, WellKnownMatchModus.Bot);
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
				Assert.Fail();
			});

			player1Connection.On<object>(ServerEventTypes.GameWaitingEvent, response =>
			{
				Assert.Fail();
			});

			MoveModel? nextMove = null;
			player1Connection.On<object>(ServerEventTypes.GameStateEvent, response =>
			{
				Assert.NotNull(response);
				var contract = JsonConvert.DeserializeObject<EventResponseContract<EventGameStatePayload>>(response.ToString() ?? "");
				if (contract?.Payload is EventGameStatePayload payload)
				{
					nextMove = payload.MoveSequences.SelectMany(ms => ms.Moves)?.FirstOrDefault();

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
			await player1Connection.SendAsync(ServerCommands.StartGameCommand, matchId);

			// player 1 rolls the dice
			await player1Connection.SendAsync(ServerCommands.RollCommand, matchId);

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
		}
	}
}
