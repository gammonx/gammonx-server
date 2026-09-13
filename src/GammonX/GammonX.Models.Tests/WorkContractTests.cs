using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Models.Tests
{
    public class WorkContractTests
    {
        [Fact]
        public void GameCompletedWorkContractRoundTripsAndSelectsWinner()
        {
            var matchId = Guid.NewGuid();
            var gameId = Guid.NewGuid();
            var winnerGame = CreateGame(gameId, matchId, Guid.NewGuid(), GameResult.Gammon, "history");
            var loserGGame = CreateGame(gameId, matchId, Guid.NewGuid(), GameResult.LostGammon, "history");
            var contract = new GameCompletedWorkContract { Records = [loserGGame, winnerGame] };

            var json = JsonConvert.SerializeObject(contract);
            var deserialized = JsonConvert.DeserializeObject<GameCompletedWorkContract>(json);
            var (winner, loser) = deserialized!.GetValidatedRecords();

            Assert.Equal(winnerGame.PlayerId, winner.PlayerId);
            Assert.Equal(loserGGame.PlayerId, loser.PlayerId);
            Assert.Equal(2, deserialized.Records.Length);
            }

        [Fact]
        public void MatchCompletedWorkContractRejectsDifferentHistories()
        {
            var matchId = Guid.NewGuid();
            var winner = CreateMatch(matchId, Guid.NewGuid(), MatchResult.Won, MatchModus.Normal, "history-a");
            var loser = CreateMatch(matchId, Guid.NewGuid(), MatchResult.Lost, MatchModus.Normal, "history-b");
            var contract = new MatchCompletedWorkContract { Records = [winner, loser] };

            var exception = Assert.Throws<InvalidOperationException>(() => contract.GetValidatedRecords());

            Assert.Contains("same non-empty history", exception.Message);
        }

        [Fact]
        public void RatingUpdateWorkContractRequiresRankedMatch()
        {
            var matchId = Guid.NewGuid();
            var winner = CreateMatch(matchId, Guid.NewGuid(), MatchResult.Won, MatchModus.Normal, "history");
            var loser = CreateMatch(matchId, Guid.NewGuid(), MatchResult.Lost, MatchModus.Normal, "history");
            var contract = new RatingUpdateWorkContract { Records = [winner, loser] };

            var exception = Assert.Throws<InvalidOperationException>(() => contract.GetValidatedRecords());

            Assert.Contains("ranked", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static GameRecordContract CreateGame(
            Guid gameId,
            Guid matchId,
            Guid playerId,
            GameResult result,
            string history)
        {
            return new GameRecordContract
            {
                Id = gameId,
                MatchId = matchId,
                PlayerId = playerId,
                Result = result,
                Format = HistoryFormat.MAT,
                GameHistory = history
            };
        }

        private static MatchRecordContract CreateMatch(
            Guid matchId,
            Guid playerId,
            MatchResult result,
            MatchModus modus,
            string history)
        {
            return new MatchRecordContract
            {
                Id = matchId,
                PlayerId = playerId,
                Result = result,
                Variant = MatchVariant.Backgammon,
                Type = MatchType.SevenPointGame,
                Modus = modus,
                BotLevel = BotLevel.Unknown,
                Format = HistoryFormat.MAT,
                MatchHistory = history
            };
        }
    }
}