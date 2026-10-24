using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Utils;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using System.Net;
using System.Net.Http.Json;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests
{
	public class MatchesControllerTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly WebApplicationFactory<Program> _factory;
		private readonly IServiceProvider _serviceProvider;
		private readonly Guid _player1Id = Guid.Parse("fdd907ca-794a-43f4-83e6-cadfabc57c45");
		private readonly Guid _player2Id = Guid.Parse("f6f9bb06-cbf6-4f42-80bf-5d62be34cff6");

		public MatchesControllerTests(WebApplicationFactory<Program> factory)
		{
			_factory = factory.WithWebHostBuilder(_ =>
			{
				// pass
			});
			_serviceProvider = _factory.Services;
			Assert.NotNull(_serviceProvider);
			Assert.NotNull(_factory);
		}

		[Theory]
		[InlineData(false, null)]
		[InlineData(true, null)]
		[InlineData(true, BotLevel.Unknown)]
		[InlineData(true, (BotLevel)(-1))]
		[InlineData(true, (BotLevel)4)]
		[InlineData(true, (BotLevel)int.MinValue)]
		[InlineData(true, (BotLevel)int.MaxValue)]
		public async Task BotQueueRejectsMissingOrInvalidBotLevel(bool includeBotLevel, BotLevel? botLevel)
		{
			using var client = _factory.CreateClient();
			var playerId = Guid.NewGuid();
			var request = new Dictionary<string, object?>
			{
				["playerId"] = playerId,
				["matchVariant"] = MatchVariant.Backgammon,
				["matchModus"] = MatchModus.Bot,
				["matchType"] = MatchType.CashGame,
			};
			if (includeBotLevel)
			{
				request["botLevel"] = botLevel;
			}

			using var response = await client.PostAsJsonAsync("/game/api/matches/join", request, TestContext.Current.CancellationToken);
			var resultJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
			var joinResponse = JsonConvert.DeserializeObject<ResponseContract<RequestErrorPayload>>(resultJson);

			Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
			Assert.NotNull(joinResponse);
			Assert.Equal("ERROR", joinResponse.Type);
			Assert.Equal("JOIN_ERROR", joinResponse.Payload.Code);
			Assert.Equal("bot level must be specified in order to join this queue", joinResponse.Payload.Message);

			var matchmakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(MatchModus.Bot);
			Assert.DoesNotContain(matchmakingService.GetQueueEntries(), entry => entry.PlayerId == playerId);
			Assert.DoesNotContain(matchmakingService.GetMatchLobbies(), lobby => lobby.Player1.Id == playerId);
		}

		[Theory]
		[InlineData(BotLevel.Easy)]
		[InlineData(BotLevel.Medium)]
		[InlineData(BotLevel.Hard)]
		[InlineData(BotLevel.Expert)]
		public async Task BotQueuePreservesRequestedBotLevel(BotLevel botLevel)
		{
			using var client = _factory.CreateClient();
			var request = new JoinRequest(Guid.NewGuid(), MatchVariant.Backgammon, MatchModus.Bot, MatchType.CashGame, botLevel);

			using var response = await client.PostAsJsonAsync("/game/api/matches/join", request, TestContext.Current.CancellationToken);
			var resultJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
			var joinResponse = JsonConvert.DeserializeObject<ResponseContract<RequestQueueEntryPayload>>(resultJson);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(joinResponse);
			Assert.Equal("OK", joinResponse.Type);
			Assert.NotNull(joinResponse.Payload.QueueId);

			var matchmakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(MatchModus.Bot);
			Assert.True(matchmakingService.TryFindMatchLobby(joinResponse.Payload.QueueId.Value, out var lobby));
			Assert.NotNull(lobby);
			Assert.Equal(request.PlayerId, lobby.Player1.Id);
			Assert.Equal(botLevel, lobby.QueueKey.BotLevel);
			Assert.True(matchmakingService.TryRemoveMatchLobby(lobby.MatchId));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task NormalQueueAcceptsMissingBotLevel(bool includeBotLevel)
		{
			using var client = _factory.CreateClient();
			var request = new Dictionary<string, object?>
			{
				["playerId"] = Guid.NewGuid(),
				["matchVariant"] = MatchVariant.Backgammon,
				["matchModus"] = MatchModus.Normal,
				["matchType"] = MatchType.CashGame,
			};
			if (includeBotLevel)
			{
				request["botLevel"] = null;
			}

			using var response = await client.PostAsJsonAsync("/game/api/matches/join", request, TestContext.Current.CancellationToken);
			var resultJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
			var joinResponse = JsonConvert.DeserializeObject<ResponseContract<RequestQueueEntryPayload>>(resultJson);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.NotNull(joinResponse);
			Assert.Equal("OK", joinResponse.Type);
			Assert.NotNull(joinResponse.Payload.QueueId);

			var matchmakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(MatchModus.Normal);
			Assert.True(matchmakingService.TryFindQueueEntry(joinResponse.Payload.QueueId.Value, out var entry));
			Assert.NotNull(entry);
			Assert.Equal(BotLevel.Unknown, entry.QueueKey.BotLevel);
			await matchmakingService.LeaveQueueAsync(entry);
		}

		[Theory]
		[InlineData(MatchModus.Normal)]
		[InlineData(MatchModus.Ranked)]
		public async Task TwoPlayersCanJoinAndCreateLobbyAsync(MatchModus modus)
		{
			var client = _factory.CreateClient();
			var matchmakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(modus);
				
			var player1 = CreatePlayer(_player1Id, MatchVariant.Backgammon, modus);
			var player2 = CreatePlayer(_player2Id, MatchVariant.Backgammon, modus);
			// join player 1
			var response1 = await client.PostAsJsonAsync("/game/api/matches/join", player1, TestContext.Current.CancellationToken);
			var resultJson1 = await response1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
			var joinResponse1 = JsonConvert.DeserializeObject<ResponseContract<RequestQueueEntryPayload>>(resultJson1);
			var joinPayload1 = joinResponse1?.Payload;
			Assert.NotNull(joinPayload1);
			Assert.Equal(QueueEntryStatus.WaitingForOpponent, joinPayload1.Status);
			Assert.Null(joinPayload1.MatchId);
			Assert.NotNull(joinPayload1.QueueId);
			// status for player 1
			var statusRequest1 = new StatusRequest(_player1Id, modus);
			var statusResponse1 = await client.PostAsJsonAsync($"/api/matches/queues/{joinPayload1.QueueId}", statusRequest1, TestContext.Current.CancellationToken);
			var statusJson1 = await statusResponse1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
			Assert.NotNull(statusJson1);
			var statusContract1 = JsonConvert.DeserializeObject<ResponseContract<RequestQueueEntryPayload>>(statusJson1);
			Assert.NotNull(statusContract1);
			Assert.Equal(QueueEntryStatus.WaitingForOpponent, statusContract1.Payload.Status);
			Assert.Null(statusContract1.Payload.MatchId);
			Assert.NotNull(statusContract1.Payload.QueueId);
			// join player 2
			var response2 = await client.PostAsJsonAsync("/game/api/matches/join", player2, TestContext.Current.CancellationToken);
			var resultJson2 = await response2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
			var joinResponse2 = JsonConvert.DeserializeObject<ResponseContract<RequestQueueEntryPayload>>(resultJson2);
			var joinPayload2 = joinResponse2?.Payload;
			Assert.NotNull(joinPayload2);
			Assert.Equal(QueueEntryStatus.WaitingForOpponent, joinPayload2.Status);
			Assert.Null(joinPayload2.MatchId);
			Assert.NotNull(joinPayload2.QueueId);

			Assert.NotNull(joinPayload1.QueueId);
			Assert.NotNull(joinPayload2.QueueId);
			RequestQueueEntryPayload? result1;
			RequestQueueEntryPayload? result2;
			do
			{
				result1 = await client.PollAsync(player1.PlayerId, joinPayload1.QueueId.Value, modus);
			}
			while (result1.Status == QueueEntryStatus.WaitingForOpponent);

			do
			{
				result2 = await client.PollAsync(player2.PlayerId, joinPayload2.QueueId.Value, modus);
			}
			while (result2.Status == QueueEntryStatus.WaitingForOpponent);

			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.Equal(result1.MatchId, result2.MatchId);
			var matchId = result1.MatchId;
			Assert.NotNull(matchId);

			var matchLobbyCreated = matchmakingService.TryFindMatchLobby(matchId.Value, out var lobby);
			Assert.NotNull(lobby);
			Assert.True(matchLobbyCreated);

			Assert.True(matchmakingService.TryRemoveMatchLobby(matchId.Value));
			Assert.False(matchmakingService.TryFindMatchLobby(matchId.Value, out var _));
			Assert.False(matchmakingService.TryRemoveMatchLobby(matchId.Value));
		}

		[Theory]
		[InlineData(MatchModus.Normal)]
		[InlineData(MatchModus.Ranked)]
		public async Task SamePlayerCannotJoinTwice(MatchModus modus)
		{
			var player1 = CreatePlayer(_player1Id, MatchVariant.Backgammon, modus);
			var player2 = CreatePlayer(_player2Id, MatchVariant.Backgammon, modus);
			var queueKey = new QueueKey(MatchVariant.Backgammon, modus, MatchType.CashGame, BotLevel.Hard);
			var matchmakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(modus);

			await matchmakingService.JoinQueueAsync(player1.PlayerId, queueKey);
			await Assert.ThrowsAsync<InvalidOperationException>(() => matchmakingService.JoinQueueAsync(player1.PlayerId, queueKey));
			await matchmakingService.JoinQueueAsync(player2.PlayerId, queueKey);
			await Assert.ThrowsAsync<InvalidOperationException>(() => matchmakingService.JoinQueueAsync(player2.PlayerId, queueKey));
		}

		[Theory]
		[InlineData(MatchModus.Normal)]
		[InlineData(MatchModus.Ranked)]
		public async Task PlayerCanJoinAgainAfterLeavingTheQueue(MatchModus modus)
		{
			var player1 = CreatePlayer(_player1Id, MatchVariant.Backgammon, modus);
			var player2 = CreatePlayer(_player2Id, MatchVariant.Backgammon, modus);
			var queueKey = new QueueKey(MatchVariant.Backgammon, modus, MatchType.CashGame, BotLevel.Hard);
			var matchmakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(modus);
			// join queue
			await matchmakingService.JoinQueueAsync(player1.PlayerId, queueKey);
			await matchmakingService.JoinQueueAsync(player2.PlayerId, queueKey);
			// get a match
			await matchmakingService.MatchQueuedPlayersAsync();
			// join again
			await matchmakingService.JoinQueueAsync(player1.PlayerId, queueKey);
			await matchmakingService.JoinQueueAsync(player2.PlayerId, queueKey);
		}

        [Fact]
        public async Task KnownPlayersCanJoinRankedMatch()
        {
            var player1Id = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
            var player2Id = Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8");
            var player1 = CreatePlayer(player1Id, MatchVariant.Tavli, MatchModus.Ranked);
            var player2 = CreatePlayer(player2Id, MatchVariant.Tavli, MatchModus.Ranked);
            var queueKey = new QueueKey(MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame, BotLevel.Hard);
            var matchmakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(MatchModus.Ranked);
            // join queue and fetch rating
            await matchmakingService.JoinQueueAsync(player1.PlayerId, queueKey);
            await matchmakingService.JoinQueueAsync(player2.PlayerId, queueKey);
        }

        private static JoinRequest CreatePlayer(Guid playerId, MatchVariant variant, MatchModus queueType)
		{
			return new JoinRequest(playerId, variant, queueType, MatchType.CashGame, BotLevel.Hard);
		}
	}
}
