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


		[Fact]
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
			Assert.Equal(string.Format(PlayerRatingItem.SKFormat, WellKnownMatchVariant.Backgammon, WellKnownMatchModus.Ranked), bgRating.SK, true);
			Assert.Equal(playerId, bgRating.PlayerId);
			Assert.Equal(WellKnownMatchVariant.Backgammon, bgRating.Variant);
			Assert.Equal(WellKnownMatchModus.Ranked, bgRating.Modus);
			Assert.Equal(WellKnownMatchType.CashGame, bgRating.Type);
			Assert.Equal(1350, bgRating.Rating);
			Assert.Equal(1800, bgRating.HighestRating);
			Assert.Equal(1100, bgRating.LowestRating);

			var tavliRating = playerRatings.Last();
			Assert.NotNull(tavliRating.PK);
			Assert.NotNull(tavliRating.SK);
			Assert.Equal(ItemTypes.PlayerRatingItemType, tavliRating.ItemType);
			Assert.Equal(string.Format(PlayerRatingItem.PKFormat, playerId), tavliRating.PK);
			Assert.Equal(string.Format(PlayerRatingItem.SKFormat, WellKnownMatchVariant.Tavli, WellKnownMatchModus.Normal), tavliRating.SK, true);
			Assert.Equal(playerId, tavliRating.PlayerId);
			Assert.Equal(WellKnownMatchVariant.Tavli, tavliRating.Variant);
			Assert.Equal(WellKnownMatchModus.Normal, tavliRating.Modus);
			Assert.Equal(WellKnownMatchType.SevenPointGame, tavliRating.Type);
			Assert.Equal(1350, tavliRating.Rating);
			Assert.Equal(1800, tavliRating.HighestRating);
			Assert.Equal(1100, tavliRating.LowestRating);

			await playerRepo.DeleteAsync(playerId);

			player = await playerRepo.GetAsync(playerId);
			Assert.Null(player);
			playerRatings = await playerRepo.GetRatingsAsync(playerId);
			Assert.Empty(playerRatings);
		}	
	}
}
