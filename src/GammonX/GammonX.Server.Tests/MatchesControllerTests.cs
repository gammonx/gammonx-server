using GammonX.Models.Enums;

using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;
using GammonX.Server.Tests.Utils;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

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
			_factory = factory.WithWebHostBuilder(builder =>
			{
				// pass
			});
			_serviceProvider = _factory.Services;
			Assert.NotNull(_serviceProvider);
			Assert.NotNull(_factory);
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
			var response1 = await client.PostAsJsonAsync("/game/api/matches/join", player1);
			var resultJson1 = await response1.Content.ReadAsStringAsync();
			var joinResponse1 = JsonConvert.DeserializeObject<RequestResponseContract<RequestQueueEntryPayload>>(resultJson1);
			var joinPayload1 = joinResponse1?.Payload;
			Assert.NotNull(joinPayload1);
			Assert.Equal(QueueEntryStatus.WaitingForOpponent, joinPayload1.Status);
			Assert.Null(joinPayload1.MatchId);
			Assert.NotNull(joinPayload1.QueueId);
			// status for player 1
			var statusRequest1 = new StatusRequest(_player1Id, modus);
			var statusResponse1 = await client.PostAsJsonAsync($"/api/matches/queues/{joinPayload1.QueueId}", statusRequest1);
			var statusJson1 = await statusResponse1.Content.ReadAsStringAsync();
			Assert.NotNull(statusJson1);
			var statusContract1 = JsonConvert.DeserializeObject<RequestResponseContract<RequestQueueEntryPayload>>(statusJson1);
			Assert.NotNull(statusContract1);
			Assert.Equal(QueueEntryStatus.WaitingForOpponent, statusContract1.Payload.Status);
			Assert.Null(statusContract1.Payload.MatchId);
			Assert.NotNull(statusContract1.Payload.QueueId);
			// join player 2
			var response2 = await client.PostAsJsonAsync("/game/api/matches/join", player2);
			var resultJson2 = await response2.Content.ReadAsStringAsync();
			var joinResponse2 = JsonConvert.DeserializeObject<RequestResponseContract<RequestQueueEntryPayload>>(resultJson2);
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
			var queueKey = new QueueKey(MatchVariant.Backgammon, modus, MatchType.CashGame);
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
			var queueKey = new QueueKey(MatchVariant.Backgammon, modus, MatchType.CashGame);
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
            var queueKey = new QueueKey(MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame);
            var matchmakingService = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(MatchModus.Ranked);
            // join queue and fetch rating
            await matchmakingService.JoinQueueAsync(player1.PlayerId, queueKey);
            await matchmakingService.JoinQueueAsync(player2.PlayerId, queueKey);
        }

        private static JoinRequest CreatePlayer(Guid playerId, MatchVariant variant, MatchModus queueType)
		{
			return new JoinRequest(playerId, variant, queueType, MatchType.CashGame);
		}
	}
}
