using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.DynamoDb.Tests.Helper;

using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.DynamoDb.Tests.Items
{
    public class MatchHistoryItemTests
    {
        private readonly IDynamoDbRepository _repo;

        public MatchHistoryItemTests()
        {
            var serviceProvider = DynamoDbProvider.Configure();
            Assert.NotNull(serviceProvider);
            _repo = serviceProvider.GetRequiredService<IDynamoDbRepository>();
            Assert.NotNull(_repo);
        }

        [Fact]
        public async Task CreateSearchUpdateDeleteMatchHistory()
        {
            var matchId = Guid.NewGuid();
            var historyItem = ItemFactory.CreateMatchHistory(matchId);
            // create
            await _repo.SaveAsync(historyItem);
            // read
            var matches = await _repo.GetItemsAsync<MatchHistoryItem>(matchId);
            Assert.NotNull(matches);
            Assert.Single(matches);
            var historyFromRepo = matches.First();
            Assert.Equal(ItemTypes.MatchHistoryItemType, historyFromRepo.ItemType);
            Assert.Equal($"MATCH#{matchId}", historyFromRepo.PK);
            Assert.Equal($"HISTORY", historyFromRepo.SK);
            Assert.Equal(HistoryFormat.MAT, historyFromRepo.Format);
            Assert.Equal("empty", historyFromRepo.Data);
            Assert.Equal(matchId, historyFromRepo.MatchId);
            // update
            historyFromRepo.Data = "not-empty";
            await _repo.SaveAsync(historyFromRepo);
            matches = await _repo.GetItemsAsync<MatchHistoryItem>(matchId);
            Assert.NotNull(matches);
            Assert.Single(matches);
            historyFromRepo = matches.First();
            Assert.Equal("not-empty", historyFromRepo.Data);
            // delete
            var deleted = await _repo.DeleteAsync<MatchHistoryItem>(matchId, historyFromRepo.SK);
            Assert.True(deleted);
            matches = await _repo.GetItemsAsync<MatchHistoryItem>(matchId);
            Assert.NotNull(matches);
            Assert.Empty(matches);
        }

        [Fact]
        public void MatchHistoryItemDoesSupportGlobalSearchIndices()
        {
            var matchHistoryItemFactory = ItemFactoryCreator.Create<MatchHistoryItem>();
            Assert.NotNull(matchHistoryItemFactory);
            Assert.Equal("MATCH#{0}", matchHistoryItemFactory.PKFormat);
            Assert.Equal("HISTORY", matchHistoryItemFactory.SKFormat);
            Assert.Equal("HISTORY", matchHistoryItemFactory.SKPrefix);
            Assert.Throws<InvalidOperationException>(() => matchHistoryItemFactory.GSI1PKFormat);
            Assert.Throws<InvalidOperationException>(() => matchHistoryItemFactory.GSI1SKFormat);
            Assert.Throws<InvalidOperationException>(() => matchHistoryItemFactory.GSI1SKPrefix);
        }
    }
}
