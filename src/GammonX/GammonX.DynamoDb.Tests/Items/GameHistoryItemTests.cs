using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.DynamoDb.Tests.Helper;

using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.DynamoDb.Tests.Items
{
    public class GameHistoryItemTests
    {
        private readonly IDynamoDbRepository _repo;

        public GameHistoryItemTests()
        {
            var serviceProvider = DynamoDbProvider.Configure();
            Assert.NotNull(serviceProvider);
            _repo = serviceProvider.GetRequiredService<IDynamoDbRepository>();
            Assert.NotNull(_repo);
        }

        [Fact]
        public async Task CreateSearchUpdateDeleteGameHistory()
        {
            var gameId = Guid.NewGuid();
            var historyItem = ItemFactory.CreateGameHistory(gameId);
            // create
            await _repo.SaveAsync(historyItem);
            // read
            var games = await _repo.GetItemsAsync<GameHistoryItem>(gameId);
            Assert.NotNull(games);
            Assert.Single(games);
            var historyFromRepo = games.First();
            Assert.Equal(ItemTypes.GameHistoryItemType, historyFromRepo.ItemType);
            Assert.Equal($"GAME#{gameId}", historyFromRepo.PK);
            Assert.Equal("HISTORY", historyFromRepo.SK);
            Assert.Equal(HistoryFormat.MAT, historyFromRepo.Format);
            Assert.Equal("empty", historyFromRepo.Data);
            Assert.Equal(gameId, historyFromRepo.GameId);
            // update
            historyFromRepo.Data = "not-empty";
            await _repo.SaveAsync(historyFromRepo);
            games = await _repo.GetItemsAsync<GameHistoryItem>(gameId);
            Assert.NotNull(games);
            Assert.Single(games);
            historyFromRepo = games.First();
            Assert.Equal("not-empty", historyFromRepo.Data);
            // delete
            var deleted = await _repo.DeleteAsync<GameHistoryItem>(gameId, historyFromRepo.SK);
            Assert.True(deleted);
            games = await _repo.GetItemsAsync<GameHistoryItem>(gameId);
            Assert.NotNull(games);
            Assert.Empty(games);
        }

        [Fact]
        public void GameHistoryItemDoesSupportGlobalSearchIndices()
        {
            var gameHistoryItemFactory = ItemFactoryCreator.Create<GameHistoryItem>();
            Assert.NotNull(gameHistoryItemFactory);
            Assert.Equal("GAME#{0:D}", gameHistoryItemFactory.PKFormat);
            Assert.Equal("HISTORY", gameHistoryItemFactory.SKFormat);
            Assert.Equal("HISTORY", gameHistoryItemFactory.SKPrefix);
            Assert.Throws<InvalidOperationException>(() => gameHistoryItemFactory.GSI1PKFormat);
            Assert.Throws<InvalidOperationException>(() => gameHistoryItemFactory.GSI1SKFormat);
            Assert.Throws<InvalidOperationException>(() => gameHistoryItemFactory.GSI1SKPrefix);
        }

        [Fact]
        public void GameHistoryFactoryStoresDataAsBinary()
        {
            var historyItem = ItemFactory.CreateGameHistory(Guid.NewGuid());
            historyItem.Data = ";[Game 'history']\r\nWhite Roll 1 2\n";
            var factory = ItemFactoryCreator.Create<GameHistoryItem>();

            var attributes = factory.CreateItem(historyItem);

            Assert.NotNull(attributes["Data"].B);
            Assert.NotEqual(0, attributes["Data"].B.Length);
            Assert.Null(attributes["Data"].S);
            Assert.Equal(nameof(HistoryFormat.MAT), attributes["Format"].S);
            Assert.Equal(historyItem.Data, factory.CreateItem(attributes).Data);
        }
    }
}
