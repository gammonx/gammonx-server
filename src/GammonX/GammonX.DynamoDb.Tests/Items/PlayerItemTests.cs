using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.DynamoDb.Tests.Helper;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.DynamoDb.Tests.Items
{
    public class PlayerItemTests
    {
        private readonly IDynamoDbRepository _repo;

        public PlayerItemTests() 
        {
            var serviceProvider = DynamoDbProvider.Configure();
            Assert.NotNull(serviceProvider);
            _repo = serviceProvider.GetRequiredService<IDynamoDbRepository>();
            Assert.NotNull(_repo);
        }

        [Fact]
        public async Task CreateSearchUpdateDeletePlayer()
        {
            var player = ItemFactory.CreatePlayer();
            // create
            await _repo.SaveAsync(player);
            // read
            var players = await _repo.GetItemsAsync<PlayerItem>(player.Id);
            Assert.NotNull(players);
            Assert.Single(players);
            var playerFromRepo = players.First();
            Assert.Equal(player.Id, playerFromRepo.Id);
            Assert.Equal(player.UserName, playerFromRepo.UserName);
            Assert.Equal(player.CreatedAt, playerFromRepo.CreatedAt);
            Assert.Equal(ItemTypes.PlayerItemType, playerFromRepo.ItemType);
            Assert.Equal($"PLAYER#{player.Id}", playerFromRepo.PK);
            Assert.Equal("PROFILE", playerFromRepo.SK);
            // update
            playerFromRepo.UserName = "new-username";
            await _repo.SaveAsync(playerFromRepo);
            players = await _repo.GetItemsAsync<PlayerItem>(player.Id);
            Assert.NotNull(players);
            Assert.Single(players);
            playerFromRepo = players.First();
            Assert.Equal("new-username", playerFromRepo.UserName);
            // delete
            var deleted = await _repo.DeleteAsync<PlayerItem>(player.Id, "PROFILE");
            Assert.True(deleted);
            players = await _repo.GetItemsAsync<PlayerItem>(player.Id);
            Assert.NotNull(players);
            Assert.Empty(players);
        }

        [Fact]
        public void PlayerItemDoesNotSupportGlobalSearchIndices()
        {
            var playerItemFactory = ItemFactoryCreator.Create<PlayerItem>();
            Assert.NotNull(playerItemFactory);
            Assert.Equal("PLAYER#{0}", playerItemFactory.PKFormat);
            Assert.Equal("PROFILE", playerItemFactory.SKFormat);
            Assert.Equal("PROFILE", playerItemFactory.SKPrefix);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1PKFormat);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1SKFormat);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1SKPrefix);
        }
    }
}
