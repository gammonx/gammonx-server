using GammonX.Server.Contracts;
using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.AspNetCore.Mvc.Testing;

using Newtonsoft.Json;

using System.Net.Http.Json;

namespace GammonX.Server.Tests
{
	public class MatchesControllerTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly WebApplicationFactory<Program> _factory;
		private readonly IMatchmakingService _matchmakingService;

		public MatchesControllerTests(WebApplicationFactory<Program> factory)
		{
			var service = factory.Services.GetService(typeof(IMatchmakingService));
			Assert.NotNull(service);
			_matchmakingService = (IMatchmakingService)service;
			Assert.NotNull(_matchmakingService);
			_factory = factory;
		}

		[Fact]
		public async Task TwoPlayersCanJoinAndCreateLobbyAsync()
		{
			var client = _factory.CreateClient();
			var serverUri = client.BaseAddress!.ToString().TrimEnd('/');
			var player1Id = Guid.NewGuid();
			var player2Id = Guid.NewGuid();
				
			var player1 = CreatePlayer(player1Id, WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal);
			var player2 = CreatePlayer(player2Id, WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal);
			
			var response1 = await client.PostAsJsonAsync("/api/matches/join", player1);
			var resultJson1 = await response1.Content.ReadAsStringAsync();
			var joinResponse1 = JsonConvert.DeserializeObject<RequestResponseContract<RequestMatchIdPayload>>(resultJson1);
			var joinPayload1 = joinResponse1?.Payload;
			Assert.NotNull(joinPayload1);
			var matchId = joinPayload1.MatchId;

			Assert.True(_matchmakingService.TryFindMatchLobby(matchId, out var matchLobby));
			Assert.NotNull(matchLobby);
			Assert.Equal(matchId, matchLobby.MatchId);
			Assert.Equal(player1.PlayerId, matchLobby.Player1.PlayerId);
			Assert.Null(matchLobby.Player2);

			var response2 = await client.PostAsJsonAsync("/api/matches/join", player2);
			var resultJson2 = await response2.Content.ReadAsStringAsync();
			var joinResponse2 = JsonConvert.DeserializeObject<RequestResponseContract<RequestMatchIdPayload>>(resultJson2);
			var joinPayload2 = joinResponse2?.Payload;
			Assert.NotNull(joinPayload2);

			Assert.Equal(matchId, joinPayload2.MatchId);

			Assert.True(_matchmakingService.TryFindMatchLobby(matchId, out matchLobby));
			Assert.NotNull(matchLobby);
			Assert.Equal(matchId, matchLobby.MatchId);
			Assert.Equal(player1.PlayerId, matchLobby.Player1.PlayerId);
			Assert.NotNull(matchLobby.Player2);
			Assert.Equal(player2.PlayerId, matchLobby.Player2.PlayerId);

			Assert.True(_matchmakingService.TryRemoveMatchLobby(matchId));
			Assert.False(_matchmakingService.TryFindMatchLobby(matchId, out matchLobby));
		}

		[Fact]
		public void SamePlayerCannotJoinTwice()
		{
			var player1Id = Guid.NewGuid();
			var player2Id = Guid.NewGuid();
			var player1 = CreatePlayer(player1Id, WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal);
			var player2 = CreatePlayer(player2Id, WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal);
			var queueKey = new QueueKey(WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Normal, WellKnownMatchType.CashGame);

			_matchmakingService.JoinQueue(new LobbyEntry(player1.PlayerId), queueKey);
			Assert.Throws<InvalidOperationException>(() => _matchmakingService.JoinQueue(new LobbyEntry(player1.PlayerId), queueKey));
			_matchmakingService.JoinQueue(new LobbyEntry(player2.PlayerId), queueKey);
			Assert.Throws<InvalidOperationException>(() => _matchmakingService.JoinQueue(new LobbyEntry(player2.PlayerId), queueKey));
		}

		private static JoinRequest CreatePlayer(Guid playerId, WellKnownMatchVariant variant, WellKnownMatchModus queueType)
		{
			return new JoinRequest(playerId, variant, queueType, WellKnownMatchType.CashGame);
		}
	}
}
