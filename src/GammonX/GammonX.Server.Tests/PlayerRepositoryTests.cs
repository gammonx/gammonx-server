using GammonX.Server.Data.Entities;
using GammonX.Server.Data.Repository;

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
			Assert.Equal(playerId, player.Id);
			Assert.Equal(userName, player.UserName);
		}
	}
}
