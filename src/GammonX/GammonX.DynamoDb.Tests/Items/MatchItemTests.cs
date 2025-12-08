using GammonX.DynamoDb.Items;
using GammonX.DynamoDb.Repository;

using GammonX.DynamoDb.Tests.Helper;

using GammonX.Models.Enums;

using Microsoft.Extensions.DependencyInjection;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.DynamoDb.Tests.Items
{
    public class MatchItemTests
    {
        private readonly IDynamoDbRepository _repo;

        public MatchItemTests()
        {
            var serviceProvider = DynamoDbProvider.Configure();
            Assert.NotNull(serviceProvider);
            _repo = serviceProvider.GetRequiredService<IDynamoDbRepository>();
            Assert.NotNull(_repo);
        }

        [Fact]
        public async Task CreateAndSearchMultipleMatches()
        {
            var match1Id = Guid.NewGuid();
            var match2Id = Guid.NewGuid();
            var match3Id = Guid.NewGuid();

            var winner = ItemFactory.CreatePlayer();
            var win1 = ItemFactory.CreateMatch(match1Id, winner, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);
            await _repo.SaveAsync(win1);
            var win2 = ItemFactory.CreateMatch(match2Id, winner, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame);
            await _repo.SaveAsync(win2);
            var loss3 = ItemFactory.CreateMatch(match3Id, winner, MatchResult.Lost, MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame);
            await _repo.SaveAsync(loss3);

            var loser = ItemFactory.CreatePlayer();
            var loss1 = ItemFactory.CreateMatch(match1Id, loser, MatchResult.Lost, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);
            await _repo.SaveAsync(loss1);
            var loss2 = ItemFactory.CreateMatch(match2Id, loser, MatchResult.Lost, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.FivePointGame);
            await _repo.SaveAsync(loss2);
            var win3 = ItemFactory.CreateMatch(match3Id, loser, MatchResult.Won, MatchVariant.Tavli, MatchModus.Normal, MatchType.CashGame);
            await _repo.SaveAsync(win3);

            var winnersMatches = await _repo.GetItemsByGSIPKAsync<MatchItem>(winner.Id);
            Assert.Equal(3, winnersMatches.Count());
            var loserMatches = await _repo.GetItemsByGSIPKAsync<MatchItem>(winner.Id);
            Assert.Equal(3, loserMatches.Count());

            var winAndLoss = await _repo.GetItemsAsync<MatchItem>(win1.Id);
            Assert.Equal(2, winAndLoss.Count());
            Assert.Equal(winAndLoss.First().Id, winAndLoss.Last().Id);
            Assert.Contains(winAndLoss, wl => wl.PlayerId == winner.Id);
            Assert.Contains(winAndLoss, wl => wl.PlayerId == loser.Id);

            var winnerWins = await _repo.GetItemsByGSIPKAsync<MatchItem>(winner.Id, "MATCH#Backgammon#FivePointGame#Ranked#WON");
            Assert.Single(winnerWins);
            winnerWins = await _repo.GetItemsByGSIPKAsync<MatchItem>(winner.Id, "MATCH#Backgammon#CashGame#Ranked#WON");
            Assert.Single(winnerWins);
            var winnerLosses = await _repo.GetItemsByGSIPKAsync<MatchItem>(winner.Id, "MATCH#Tavli#CashGame#Normal#LOST");
            Assert.Single(winnerLosses);
            var winnerBackgammonCashGame = await _repo.GetItemsByGSIPKAsync<MatchItem>(winner.Id, "MATCH#Backgammon#CashGame");
            Assert.Single(winnerBackgammonCashGame);
            var winnerBackgammon = await _repo.GetItemsByGSIPKAsync<MatchItem>(winner.Id, "MATCH#Backgammon");
            Assert.Equal(2, winnerBackgammon.Count());
            var winnerAll = await _repo.GetItemsByGSIPKAsync<MatchItem>(winner.Id, "MATCH#");
            Assert.Equal(3, winnerAll.Count());
            var loserDetails = await _repo.GetItemsAsync<MatchItem>(loss1.Id, "DETAILS#LOST");
            Assert.Single(loserDetails);

            await _repo.DeleteAsync<MatchItem>(win1.Id, win1.SK);
            await _repo.DeleteAsync<MatchItem>(win2.Id, win2.SK);
            await _repo.DeleteAsync<MatchItem>(win3.Id, win3.SK);
            await _repo.DeleteAsync<MatchItem>(loss1.Id, loss1.SK);
            await _repo.DeleteAsync<MatchItem>(loss2.Id, loss2.SK);
            await _repo.DeleteAsync<MatchItem>(loss3.Id, loss3.SK);
        }

        [Fact]
        public async Task CreateSearchUpdateDeleteMatch()
        {
            var player = ItemFactory.CreatePlayer();
            var match = ItemFactory.CreateMatch(Guid.NewGuid(), player, MatchResult.Won, MatchVariant.Backgammon, MatchModus.Ranked, MatchType.CashGame);
            // create
            await _repo.SaveAsync(match);
            // read
            var matches = await _repo.GetItemsAsync<MatchItem>(match.Id);
            Assert.NotNull(matches);
            Assert.Single(matches);
            var matchFromRepo = matches.First();
            Assert.Equal($"MATCH#{match.Id}", matchFromRepo.PK);
            Assert.Equal($"DETAILS#WON", matchFromRepo.SK);
            Assert.Equal($"PLAYER#{player.Id}", matchFromRepo.GSI1PK);
            Assert.Equal($"MATCH#Backgammon#CashGame#Ranked#WON", matchFromRepo.GSI1SK);
            Assert.Equal(player.Id, matchFromRepo.PlayerId);
            Assert.Equal(MatchVariant.Backgammon, matchFromRepo.Variant);
            Assert.Equal(MatchModus.Ranked, matchFromRepo.Modus);
            Assert.Equal(MatchType.CashGame, matchFromRepo.Type);
            Assert.Equal(MatchResult.Won, matchFromRepo.Result);
            Assert.Equal(7, matchFromRepo.Points);
            Assert.Equal(4, matchFromRepo.Length);
            Assert.Equal(3, matchFromRepo.Gammons);
            Assert.Equal(0, matchFromRepo.Backgammons);
            Assert.Equal(0.4, matchFromRepo.AvgDoubleDices);
            Assert.Equal(0.3, matchFromRepo.AvgDoubles);
            Assert.Equal(2.5, matchFromRepo.AvgPipesLeft);
            Assert.Equal(15, matchFromRepo.AvgTurns);
            Assert.Equal(TimeSpan.FromMinutes(10), matchFromRepo.AvgDuration);
            Assert.Equal(TimeSpan.FromMinutes(40), matchFromRepo.Duration);
            // update
            matchFromRepo.Points++;
            await _repo.SaveAsync(matchFromRepo);
            matches = await _repo.GetItemsAsync<MatchItem>(match.Id);
            Assert.NotNull(matches);
            Assert.Single(matches);
            matchFromRepo = matches.First();
            Assert.Equal(8, matchFromRepo.Points);
            // delete
            var deleted = await _repo.DeleteAsync<MatchItem>(match.Id, match.SK);
            Assert.True(deleted);
            matches = await _repo.GetItemsAsync<MatchItem>(match.Id);
            Assert.NotNull(matches);
            Assert.Empty(matches);
        }

        [Fact]
        public void MatchItemDoesSupportGlobalSearchIndices()
        {
            var matchItemFactory = ItemFactoryCreator.Create<MatchItem>();
            Assert.NotNull(matchItemFactory);
            Assert.Equal("MATCH#{0}", matchItemFactory.PKFormat);
            Assert.Equal("DETAILS#{0}", matchItemFactory.SKFormat);
            Assert.Equal("DETAILS#", matchItemFactory.SKPrefix);
            Assert.Equal("PLAYER#{0}", matchItemFactory.GSI1PKFormat);
            Assert.Equal("MATCH#{0}#{1}#{2}#{3}", matchItemFactory.GSI1SKFormat);
            Assert.Equal("MATCH#", matchItemFactory.GSI1SKPrefix);
            Assert.Equal("MATCH#{0}#{1}#{2}", ((MatchItemFactory)matchItemFactory).GSI1SKAllFormat);
        }
    }
}
