using GammonX.Server.Models;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GammonX.Server.Tests
{
	public class PlayerRepositoryTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly WebApplicationFactory<Program> _factory;
		private readonly IServiceProvider _serviceProvider;

		public PlayerRepositoryTests(WebApplicationFactory<Program> factory)
		{
			_factory = factory.WithWebHostBuilder(builder =>
			{
				// pass
			});
			_serviceProvider = _factory.Services;
		}

		// TODO move tests

		//[Fact]
		//[Fact(Skip = "requires dynamo db instance")]
		//public async Task AddMatchAndSearchByGlobalSearchIndex()
		//{
		//	var scopedSp = _serviceProvider.CreateScope();
		//	var playerRepo = scopedSp.ServiceProvider.GetRequiredService<IPlayerRepository>();
		//	// create player 1
		//	var player1Id = Guid.NewGuid();
		//	var userName1 = $"TestPlayer_{player1Id}";
		//	var createdAt1 = DateTime.UtcNow;
		//	var playerItem1 = new PlayerItem
		//	{
		//		Id = player1Id,
		//		UserName = userName1,
		//		CreatedAt = createdAt1,
		//	};
		//	await playerRepo.SaveAsync(playerItem1);
		//	// create player 2
		//	var player2Id = Guid.NewGuid();
		//	var userName2 = $"TestPlayer_{player1Id}";
		//	var createdAt2 = DateTime.UtcNow;
		//	var playerItem2 = new PlayerItem
		//	{
		//		Id = player2Id,
		//		UserName = userName2,
		//		CreatedAt = createdAt2,
		//	};
		//	await playerRepo.SaveAsync(playerItem2);

		//	// save match of player
		//	var matchId = Guid.NewGuid();
		//	var myMatchItem = new MatchItem()
		//	{
		//		Id = matchId,
		//		PlayerId = player1Id,
		//		Length = 1,
		//		Points = 3,
		//		Variant = WellKnownMatchVariant.Backgammon,
		//		Type = WellKnownMatchType.CashGame,
		//		Modus = WellKnownMatchModus.Normal,
		//		StartedAt = DateTime.UtcNow,
		//		EndedAt = DateTime.UtcNow,
		//		Won = true
		//	};
		//	await playerRepo.SaveAsync(myMatchItem);
		//	// save match of another player
		//	var otherMatchItem = new MatchItem()
		//	{
		//		Id = matchId,
		//		PlayerId = player2Id,
		//		Length = 3,
		//		Points = 15,
		//		Variant = WellKnownMatchVariant.Tavli,
		//		Type = WellKnownMatchType.FivePointGame,
		//		Modus = WellKnownMatchModus.Ranked,
		//		StartedAt = DateTime.UtcNow,
		//		EndedAt = DateTime.UtcNow,
		//		Won = false
		//	};
		//	await playerRepo.SaveAsync(otherMatchItem);
			
		//	// search by pk and sk
		//	var myMatches = await playerRepo.GetMatchesAsync(matchId);
		//	Assert.NotNull(myMatches);
		//	Assert.NotEmpty(myMatches);
		//	// one for won and lost
		//	Assert.Equal(2, myMatches.Count());
		//	var wonMatch = myMatches.FirstOrDefault(m => m.PlayerId.Equals(player1Id));
		//	Assert.NotNull(wonMatch);
		//	Assert.True(wonMatch.Won);
		//	Assert.Equal(matchId, wonMatch.Id);
		//	Assert.Equal(player1Id, wonMatch.PlayerId);
		//	Assert.Equal(1, wonMatch.Length);
		//	Assert.Equal(3, wonMatch.Points);
		//	Assert.Equal(WellKnownMatchVariant.Backgammon, wonMatch.Variant);
		//	Assert.Equal(WellKnownMatchType.CashGame, wonMatch.Type);
		//	Assert.Equal(WellKnownMatchModus.Normal, wonMatch.Modus);
		//	Assert.Equal($"MATCH#{matchId}", wonMatch.PK);
		//	Assert.Equal("DETAILS#WON", wonMatch.SK);
		//	Assert.Equal($"PLAYER#{player1Id}", wonMatch.GSI1PK);
		//	Assert.Equal($"MATCH#{matchId}#Backgammon#CashGame#Normal#WON", wonMatch.GSI1SK);

		//	var otherMatches = await playerRepo.GetMatchesAsync(matchId);
		//	Assert.NotNull(otherMatches);
		//	Assert.NotEmpty(otherMatches);
		//	Assert.Equal(2, otherMatches.Count());
		//	var lostMatch = otherMatches.FirstOrDefault(m => m.PlayerId.Equals(player2Id));
		//	Assert.NotNull(lostMatch);
		//	Assert.False(lostMatch.Won);
		//	Assert.Equal(matchId, lostMatch.Id);
		//	Assert.Equal(player2Id, lostMatch.PlayerId);
		//	Assert.Equal(3, lostMatch.Length);
		//	Assert.Equal(15, lostMatch.Points);
		//	Assert.Equal(WellKnownMatchVariant.Tavli, lostMatch.Variant);
		//	Assert.Equal(WellKnownMatchType.FivePointGame, lostMatch.Type);
		//	Assert.Equal(WellKnownMatchModus.Ranked, lostMatch.Modus);
		//	Assert.Equal($"MATCH#{matchId}", lostMatch.PK);
		//	Assert.Equal("DETAILS#LOST", lostMatch.SK);
		//	Assert.Equal($"PLAYER#{player2Id}", lostMatch.GSI1PK);
		//	Assert.Equal($"MATCH#{matchId}#Tavli#FivePointGame#Ranked#LOST", lostMatch.GSI1SK);

		//	// search by gsi1pk and gsi1sk
		//	var playerMatches = await playerRepo.GetMatchesOfPlayerAsync(player1Id);
		//	Assert.NotNull(playerMatches);
		//	Assert.NotEmpty(playerMatches);
		//	Assert.Single(playerMatches);
		//	var playerMatch = playerMatches.First();
		//	Assert.NotNull(playerMatch);
		//	Assert.Equal(matchId, playerMatch.Id);
		//	Assert.Equal(wonMatch.Id, playerMatch.Id);

		//	var otherPlayersMatches = await playerRepo.GetMatchesOfPlayerAsync(player2Id);
		//	Assert.NotNull(otherPlayersMatches);
		//	Assert.NotEmpty(otherPlayersMatches);
		//	Assert.Single(otherPlayersMatches);
		//	var otherPlayersMatch = otherPlayersMatches.First();
		//	Assert.NotNull(otherPlayersMatch);
		//	Assert.Equal(matchId, otherPlayersMatch.Id);
		//	Assert.Equal(lostMatch.Id, otherPlayersMatch.Id);

		//	// cleanup
		//	await playerRepo.DeleteAsync(player1Id);
		//	await playerRepo.DeleteAsync(player2Id);			
		//	var matches = await playerRepo.GetMatchesAsync(player1Id);
		//	Assert.Empty(matches);
		//	matches = await playerRepo.GetMatchesAsync(player2Id);
		//	Assert.Empty(matches);
		//}

		//[Fact]
		//[Fact(Skip = "requires dynamo db instance")]
		//public async Task AddPlayerProfileTest()
		//{
		//	var scopedSp = _serviceProvider.CreateScope();
		//	var playerRepo = scopedSp.ServiceProvider.GetRequiredService<IPlayerRepository>();
		//	var playerId = Guid.NewGuid();
		//	var userName = $"TestPlayer_{playerId}";
		//	var createdAt = DateTime.UtcNow;

		//	var playerItem = new PlayerItem
		//	{
		//		Id = playerId,
		//		UserName = userName,
		//		CreatedAt = createdAt,
		//	};

		//	await playerRepo.SaveAsync(playerItem);

		//	var player = await playerRepo.GetAsync(playerId);

		//	Assert.NotNull(player);
		//	Assert.Equal(string.Format(PlayerItem.PKFormat, playerId), player.PK);
		//	Assert.Equal(PlayerItem.SKValue, player.SK);
		//	Assert.Equal(ItemTypes.PlayerItemType, player.ItemType);
		//	Assert.Equal(playerId, player.Id);
		//	Assert.Equal(userName, player.UserName);
		//	Assert.Equal($"PLAYER#{playerId}", player.PK);
		//	Assert.Equal(playerItem.SK, player.SK);

		//	var playerRatingBg = new PlayerRatingItem
		//	{
		//		PlayerId = playerId,
		//		Variant = WellKnownMatchVariant.Backgammon,
		//		Modus = WellKnownMatchModus.Ranked,
		//		Type = WellKnownMatchType.CashGame,
		//		Rating = 1350,
		//		HighestRating = 1800,
		//		LowestRating = 1100
		//	};

		//	await playerRepo.SaveAsync(playerRatingBg);

		//	var playerRatingTavli = new PlayerRatingItem
		//	{
		//		PlayerId = playerId,
		//		Variant = WellKnownMatchVariant.Tavli,
		//		Modus = WellKnownMatchModus.Normal,
		//		Type = WellKnownMatchType.SevenPointGame,
		//		Rating = 1350,
		//		HighestRating = 1800,
		//		LowestRating = 1100
		//	};

		//	await playerRepo.SaveAsync(playerRatingTavli);

		//	var playerRatings = await playerRepo.GetRatingsAsync(playerId);
		//	Assert.NotNull(playerRatings);
		//	Assert.NotEmpty(playerRatings);
		//	Assert.Equal(2, playerRatings.Count());
		//	var bgRating = playerRatings.First();
		//	Assert.NotNull(bgRating.PK);
		//	Assert.NotNull(bgRating.SK);
		//	Assert.Equal(ItemTypes.PlayerRatingItemType, bgRating.ItemType);
		//	Assert.Equal(string.Format(PlayerRatingItem.PKFormat, playerId), bgRating.PK);
		//	Assert.Equal(string.Format(PlayerRatingItem.SKFormat, WellKnownMatchVariant.Backgammon), bgRating.SK, true);
		//	Assert.Equal(playerId, bgRating.PlayerId);
		//	Assert.Equal(WellKnownMatchVariant.Backgammon, bgRating.Variant);
		//	Assert.Equal(WellKnownMatchModus.Ranked, bgRating.Modus);
		//	Assert.Equal(WellKnownMatchType.CashGame, bgRating.Type);
		//	Assert.Equal(1350, bgRating.Rating);
		//	Assert.Equal(1800, bgRating.HighestRating);
		//	Assert.Equal(1100, bgRating.LowestRating);
		//	Assert.Equal($"PLAYER#{playerId}", bgRating.PK);
		//	Assert.Equal($"RATING#Backgammon", bgRating.SK);

		//	var tavliRating = playerRatings.Last();
		//	Assert.NotNull(tavliRating.PK);
		//	Assert.NotNull(tavliRating.SK);
		//	Assert.Equal(ItemTypes.PlayerRatingItemType, tavliRating.ItemType);
		//	Assert.Equal(string.Format(PlayerRatingItem.PKFormat, playerId), tavliRating.PK);
		//	Assert.Equal(string.Format(PlayerRatingItem.SKFormat, WellKnownMatchVariant.Tavli), tavliRating.SK, true);
		//	Assert.Equal(playerId, tavliRating.PlayerId);
		//	Assert.Equal(WellKnownMatchVariant.Tavli, tavliRating.Variant);
		//	Assert.Equal(WellKnownMatchModus.Normal, tavliRating.Modus);
		//	Assert.Equal(WellKnownMatchType.SevenPointGame, tavliRating.Type);
		//	Assert.Equal(1350, tavliRating.Rating);
		//	Assert.Equal(1800, tavliRating.HighestRating);
		//	Assert.Equal(1100, tavliRating.LowestRating);
		//	Assert.Equal($"PLAYER#{playerId}", tavliRating.PK);
		//	Assert.Equal($"RATING#Tavli", tavliRating.SK);

		//	// cleanup
		//	await playerRepo.DeleteAsync(playerId);
		//	player = await playerRepo.GetAsync(playerId);
		//	Assert.Null(player);
		//	playerRatings = await playerRepo.GetRatingsAsync(playerId);
		//	Assert.Empty(playerRatings);
		//}	
	}
}
