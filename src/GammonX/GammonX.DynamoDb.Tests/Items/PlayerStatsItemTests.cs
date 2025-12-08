using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;
using GammonX.DynamoDb.Tests.Helper;

using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;


using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Items
{
    public class PlayerStatsItemTests
    {
        private readonly IDynamoDbRepository _repo;

        public PlayerStatsItemTests()
        {
            var serviceProvider = DynamoDbProvider.Configure();
            Assert.NotNull(serviceProvider);
            _repo = serviceProvider.GetRequiredService<IDynamoDbRepository>();
            Assert.NotNull(_repo);
        }

        [Theory]
        [InlineData("STATS#Backgammon#SevenPointGame#Ranked", "STATS#Backgammon#SevenPointGame", "STATS#Backgammon", MatchVariant.Backgammon, MatchType.SevenPointGame)]
        [InlineData("STATS#Tavla#FivePointGame#Normal", "STATS#Tavla#FivePointGame", "STATS#Tavla", MatchVariant.Tavla, MatchType.FivePointGame)]
        [InlineData("STATS#Tavli#CashGame#Bot", "STATS#Tavli#CashGame", "STATS#Tavli", MatchVariant.Tavli, MatchType.CashGame)]
        public async Task CanCreateAndSearchMultipleStatsPerPlayer(string oneSk, string threeSk, string nineSk, MatchVariant expVariant, MatchType expType)
        {
            var player = ItemFactory.CreatePlayer();
            // create all combinations of player stats (27 total)
            var allPlayerStats = ItemFactory.CreateAllPlayerStats(player);
            // create them all!
            foreach (var playeStat in allPlayerStats)
            {
                await _repo.SaveAsync(playeStat);
            }
            // read and query them
            var all = await _repo.GetItemsAsync<PlayerStatsItem>(player.Id);
            Assert.NotNull(all);
            Assert.Equal(27, all.Count());
            var one = await _repo.GetItemsAsync<PlayerStatsItem>(player.Id, oneSk);
            Assert.NotNull(one);
            Assert.Single(one);
            var three = await _repo.GetItemsAsync<PlayerStatsItem>(player.Id, threeSk);
            Assert.NotNull(three);
            Assert.Equal(3, three.Count());
            Assert.True(three.All(t => t.Variant == expVariant));
            Assert.True(three.All(t => t.Type == expType));
            var nine = await _repo.GetItemsAsync<PlayerStatsItem>(player.Id, nineSk);
            Assert.NotNull(nine);
            Assert.Equal(9, nine.Count());
            Assert.True(nine.All(n => n.Variant == expVariant));
            // delete them all!
            foreach (var playerStat in allPlayerStats)
            {
                await _repo.DeleteAsync<PlayerStatsItem>(playerStat.PlayerId, playerStat.SK);
            }
        }

        [Fact]
        public async Task CreateSearchUpdateDeletePlayerStats()
        {
            var player = ItemFactory.CreatePlayer();
            var playerStats = ItemFactory.CreatePlayerStats(player, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);
            // create
            await _repo.SaveAsync(playerStats);
            // read
            var stats = await _repo.GetItemsAsync<PlayerStatsItem>(player.Id);
            Assert.NotNull(stats);
            Assert.Single(stats);
            var statFromRepo = stats.First();
            Assert.Equal($"PLAYER#{player.Id}", statFromRepo.PK);
            Assert.Equal($"STATS#Backgammon#CashGame#Ranked", statFromRepo.SK);
            Assert.Equal(player.Id, statFromRepo.PlayerId);
            Assert.Equal(MatchVariant.Backgammon, statFromRepo.Variant);
            Assert.Equal(MatchModus.Ranked, statFromRepo.Modus);
            Assert.Equal(MatchType.CashGame, statFromRepo.Type);
            Assert.Equal(100, statFromRepo.MatchesPlayed);
            Assert.Equal(60, statFromRepo.MatchesWon);
            Assert.Equal(40, statFromRepo.MatchesLost);
            Assert.Equal(60.0, statFromRepo.WinRate);
            Assert.Equal(5, statFromRepo.WinStreak);
            Assert.Equal(10, statFromRepo.LongestWinStreak);
            Assert.Equal(TimeSpan.FromHours(50), statFromRepo.TotalPlayTime);
            Assert.Equal(playerStats.LastMatch.Date, statFromRepo.LastMatch.Date);
            Assert.Equal(20, statFromRepo.MatchesLast7);
            Assert.Equal(80, statFromRepo.MatchesLast30);
            Assert.Equal(2.5, statFromRepo.AvgGammons);
            Assert.Equal(1.5, statFromRepo.AvgBackgammons);
            Assert.Equal(TimeSpan.FromMinutes(25), statFromRepo.AvgDuration);
            Assert.Equal(0.3, statFromRepo.WAvgDoubles);
            Assert.Equal(0.4, statFromRepo.WAvgDoubleDices);
            Assert.Equal(TimeSpan.FromMinutes(27), statFromRepo.WAvgDuration);
            Assert.Equal(3.2, statFromRepo.WAvgPipesLeft);
            Assert.Equal(15.0, statFromRepo.WAvgTurns);
            // update
            statFromRepo.MatchesPlayed++;
            await _repo.SaveAsync(statFromRepo);
            stats = await _repo.GetItemsAsync<PlayerStatsItem>(player.Id);
            Assert.NotNull(stats);
            Assert.Single(stats);
            statFromRepo = stats.First();
            Assert.Equal(101, statFromRepo.MatchesPlayed);
            // delete
            var deleted = await _repo.DeleteAsync<PlayerStatsItem>(player.Id, "STATS#Backgammon#CashGame#Ranked");
            Assert.True(deleted);
            stats = await _repo.GetItemsAsync<PlayerStatsItem>(player.Id);
            Assert.NotNull(stats);
            Assert.Empty(stats);
        }

        [Fact]
        public void PlayerStatsItemDoesNotSupportGlobalSearchIndices()
        {
            var playerItemFactory = ItemFactoryCreator.Create<PlayerStatsItem>();
            Assert.NotNull(playerItemFactory);
            Assert.Equal("PLAYER#{0}", playerItemFactory.PKFormat);
            Assert.Equal("STATS#{0}#{1}#{2}", playerItemFactory.SKFormat);
            Assert.Equal("STATS#", playerItemFactory.SKPrefix);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1PKFormat);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1SKFormat);
            Assert.Throws<InvalidOperationException>(() => playerItemFactory.GSI1SKPrefix);
        }
    }
}
