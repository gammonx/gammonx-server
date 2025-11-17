using GammonX.Server.Data.Entities;
using GammonX.Server.Data.Repository;
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

		[Fact(Skip = "requires dynamo db instance")]
		public async Task AddMatchAndSearchByGlobalSearchIndex()
		{
			var scopedSp = _serviceProvider.CreateScope();
			var playerRepo = scopedSp.ServiceProvider.GetRequiredService<IPlayerRepository>();
			var playerId = Guid.NewGuid();
			var userName = $"TestPlayer_{playerId}";
			var createdAt = DateTime.UtcNow;

			var playerItem = new PlayerItem
			{
				Id = playerId,
				UserName = userName,
				CreatedAt = createdAt,
			};

			await playerRepo.SaveAsync(playerItem);

			// save match of player
			var myMatchId = Guid.NewGuid();
			var myMatchItem = new MatchItem()
			{
				Id = myMatchId,
				PlayerId = playerId,
				Length = 1,
				Points = 3,
				Variant = WellKnownMatchVariant.Backgammon,
				Type = WellKnownMatchType.CashGame,
				Modus = WellKnownMatchModus.Normal,
				StartedAt = DateTime.UtcNow,
				EndedAt = DateTime.UtcNow,
				Won = true
			};
			await playerRepo.SaveAsync(myMatchItem);
			// save match of another player
			var otherMatchId = Guid.NewGuid();
			var otherPlayerId = Guid.NewGuid();
			var otherMatchItem = new MatchItem()
			{
				Id = otherMatchId,
				PlayerId = otherPlayerId,
				Length = 3,
				Points = 15,
				Variant = WellKnownMatchVariant.Tavli,
				Type = WellKnownMatchType.FivePointGame,
				Modus = WellKnownMatchModus.Ranked,
				StartedAt = DateTime.UtcNow,
				EndedAt = DateTime.UtcNow,
				Won = false
			};
			await playerRepo.SaveAsync(otherMatchItem);
			
			// search by pk and sk
			var myMatches = await playerRepo.GetMatchesAsync(myMatchId);
			Assert.NotNull(myMatches);
			Assert.NotEmpty(myMatches);
			Assert.Single(myMatches);
			var myMatch = myMatches.First();
			Assert.NotNull(myMatch);
			Assert.Equal(myMatchId, myMatch.Id);
			Assert.Equal(playerId, myMatch.PlayerId);
			Assert.Equal(1, myMatch.Length);
			Assert.Equal(3, myMatch.Points);
			Assert.Equal(WellKnownMatchVariant.Backgammon, myMatch.Variant);
			Assert.Equal(WellKnownMatchType.CashGame, myMatch.Type);
			Assert.Equal(WellKnownMatchModus.Normal, myMatch.Modus);
			Assert.True(myMatch.Won);
			Assert.Equal($"MATCH#{myMatchId}", myMatch.PK);
			Assert.Equal("DETAILS#WON", myMatch.SK);
			Assert.Equal($"PLAYER#{playerId}", myMatch.GSI1PK);
			Assert.Equal($"MATCH#{myMatchId}#Backgammon#CashGame#Normal#WON", myMatch.GSI1SK);

			var otherMatches = await playerRepo.GetMatchesAsync(otherMatchId);
			Assert.NotNull(otherMatches);
			Assert.NotEmpty(otherMatches);
			Assert.Single(otherMatches);
			var otherMatch = otherMatches.First();
			Assert.NotNull(otherMatch);
			Assert.Equal(otherMatchId, otherMatch.Id);
			Assert.Equal(otherPlayerId, otherMatch.PlayerId);
			Assert.Equal(3, otherMatch.Length);
			Assert.Equal(15, otherMatch.Points);
			Assert.Equal(WellKnownMatchVariant.Tavli, otherMatch.Variant);
			Assert.Equal(WellKnownMatchType.FivePointGame, otherMatch.Type);
			Assert.Equal(WellKnownMatchModus.Ranked, otherMatch.Modus);
			Assert.False(otherMatch.Won);
			Assert.Equal($"MATCH#{otherMatchId}", otherMatch.PK);
			Assert.Equal("DETAILS#LOST", otherMatch.SK);
			Assert.Equal($"PLAYER#{otherPlayerId}", otherMatch.GSI1PK);
			Assert.Equal($"MATCH#{otherMatchId}#Tavli#FivePointGame#Ranked#LOST", otherMatch.GSI1SK);

			// search by gsi1pk and gsi1sk
			var playerMatches = await playerRepo.GetMatchesOfPlayerAsync(playerId);
			Assert.NotNull(playerMatches);
			Assert.NotEmpty(playerMatches);
			Assert.Single(playerMatches);
			var playerMatch = playerMatches.First();
			Assert.NotNull(playerMatch);
			Assert.Equal(myMatchId, playerMatch.Id);
			Assert.Equal(myMatch.Id, playerMatch.Id);

			var otherPlayersMatches = await playerRepo.GetMatchesOfPlayerAsync(otherPlayerId);
			Assert.NotNull(otherPlayersMatches);
			Assert.NotEmpty(otherPlayersMatches);
			Assert.Single(otherPlayersMatches);
			var otherPlayersMatch = otherPlayersMatches.First();
			Assert.NotNull(otherPlayersMatch);
			Assert.Equal(otherMatchId, otherPlayersMatch.Id);
			Assert.Equal(otherMatch.Id, otherPlayersMatch.Id);

			// cleanup
			await playerRepo.DeleteAsync(playerId);
			await playerRepo.DeleteAsync(otherPlayerId);			
			var matches = await playerRepo.GetMatchesAsync(playerId);
			Assert.Empty(matches);
			matches = await playerRepo.GetMatchesAsync(otherPlayerId);
			Assert.Empty(matches);

		}

		[Fact(Skip = "requires dynamo db instance")]
		public async Task AddPlayerProfileTest()
		{
			var scopedSp = _serviceProvider.CreateScope();
			var playerRepo = scopedSp.ServiceProvider.GetRequiredService<IPlayerRepository>();
			var playerId = Guid.NewGuid();
			var userName = $"TestPlayer_{playerId}";
			var createdAt = DateTime.UtcNow;

			var playerItem = new PlayerItem
			{
				Id = playerId,
				UserName = userName,
				CreatedAt = createdAt,
			};

			await playerRepo.SaveAsync(playerItem);

			var player = await playerRepo.GetAsync(playerId);

			Assert.NotNull(player);
			Assert.Equal(string.Format(PlayerItem.PKFormat, playerId), player.PK);
			Assert.Equal(PlayerItem.SKValue, player.SK);
			Assert.Equal(ItemTypes.PlayerItemType, player.ItemType);
			Assert.Equal(playerId, player.Id);
			Assert.Equal(userName, player.UserName);
			Assert.Equal($"PLAYER#{playerId}", player.PK);
			Assert.Equal(playerItem.SK, player.SK);

			var playerRatingBg = new PlayerRatingItem
			{
				PlayerId = playerId,
				Variant = WellKnownMatchVariant.Backgammon,
				Modus = WellKnownMatchModus.Ranked,
				Type = WellKnownMatchType.CashGame,
				Rating = 1350,
				HighestRating = 1800,
				LowestRating = 1100
			};

			await playerRepo.SaveAsync(playerRatingBg);

			var playerRatingTavli = new PlayerRatingItem
			{
				PlayerId = playerId,
				Variant = WellKnownMatchVariant.Tavli,
				Modus = WellKnownMatchModus.Normal,
				Type = WellKnownMatchType.SevenPointGame,
				Rating = 1350,
				HighestRating = 1800,
				LowestRating = 1100
			};

			await playerRepo.SaveAsync(playerRatingTavli);

			var playerRatings = await playerRepo.GetRatingsAsync(playerId);
			Assert.NotNull(playerRatings);
			Assert.NotEmpty(playerRatings);
			Assert.Equal(2, playerRatings.Count());
			var bgRating = playerRatings.First();
			Assert.NotNull(bgRating.PK);
			Assert.NotNull(bgRating.SK);
			Assert.Equal(ItemTypes.PlayerRatingItemType, bgRating.ItemType);
			Assert.Equal(string.Format(PlayerRatingItem.PKFormat, playerId), bgRating.PK);
			Assert.Equal(string.Format(PlayerRatingItem.SKFormat, WellKnownMatchVariant.Backgammon), bgRating.SK, true);
			Assert.Equal(playerId, bgRating.PlayerId);
			Assert.Equal(WellKnownMatchVariant.Backgammon, bgRating.Variant);
			Assert.Equal(WellKnownMatchModus.Ranked, bgRating.Modus);
			Assert.Equal(WellKnownMatchType.CashGame, bgRating.Type);
			Assert.Equal(1350, bgRating.Rating);
			Assert.Equal(1800, bgRating.HighestRating);
			Assert.Equal(1100, bgRating.LowestRating);
			Assert.Equal($"PLAYER#{playerId}", bgRating.PK);
			Assert.Equal($"RATING#Backgammon", bgRating.SK);

			var tavliRating = playerRatings.Last();
			Assert.NotNull(tavliRating.PK);
			Assert.NotNull(tavliRating.SK);
			Assert.Equal(ItemTypes.PlayerRatingItemType, tavliRating.ItemType);
			Assert.Equal(string.Format(PlayerRatingItem.PKFormat, playerId), tavliRating.PK);
			Assert.Equal(string.Format(PlayerRatingItem.SKFormat, WellKnownMatchVariant.Tavli), tavliRating.SK, true);
			Assert.Equal(playerId, tavliRating.PlayerId);
			Assert.Equal(WellKnownMatchVariant.Tavli, tavliRating.Variant);
			Assert.Equal(WellKnownMatchModus.Normal, tavliRating.Modus);
			Assert.Equal(WellKnownMatchType.SevenPointGame, tavliRating.Type);
			Assert.Equal(1350, tavliRating.Rating);
			Assert.Equal(1800, tavliRating.HighestRating);
			Assert.Equal(1100, tavliRating.LowestRating);
			Assert.Equal($"PLAYER#{playerId}", tavliRating.PK);
			Assert.Equal($"RATING#Tavli", tavliRating.SK);

			// cleanup
			await playerRepo.DeleteAsync(playerId);
			player = await playerRepo.GetAsync(playerId);
			Assert.Null(player);
			playerRatings = await playerRepo.GetRatingsAsync(playerId);
			Assert.Empty(playerRatings);
		}	
	}
}
