using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using Newtonsoft.Json;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Models.Tests
{
    public class ContractTests
    {
        [Fact]
        public void PlayerRecordIsSerializedProperly()
        {
            var playerId = Guid.NewGuid();
            var userName = "bestInTown";

            var playerRecord = new PlayerRecordContract()
            {
                Id = playerId,
                UserName = userName,
            };

            var json = JsonConvert.SerializeObject(playerRecord);
            Assert.Contains(playerId.ToString(), json);
            Assert.Contains(userName, json);
        }

        [Fact]
        public void PlayerRecordIsDeserializedProperly()
        {
            var recordPath = Path.Combine("Data", "PlayerRecord.json");
            var recordJsonStr = File.ReadAllText(recordPath);

            var playerRecord = JsonConvert.DeserializeObject<PlayerRecordContract>(recordJsonStr);
            Assert.NotNull(playerRecord);
            Assert.Equal(Guid.Parse("8eded23b-8ed1-41c2-8b83-c9ca9ce27a42"), playerRecord.Id);
            Assert.Equal("bestInTown", playerRecord.UserName);
        }

        [Fact]
        public void GameRecordIsSerializedProperly()
        {
            var gameId = Guid.Parse("c57e0961-02e7-4aac-857f-565e9d78db09");
            var playerId = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
            var gameHistoryPath = Path.Combine("Data", "PortesGameHistory.txt");
            var gameHistory = File.ReadAllText(gameHistoryPath);
            var gameRecord = new GameRecordContract()
            {
                Id = gameId,
                PlayerId = playerId,
                Result = GameResult.Gammon,
                DoublingCubeValue = null,
                PipesLeft = 0,
                Format = HistoryFormat.MAT,
                GameHistory = gameHistory
            };

            var json = JsonConvert.SerializeObject(gameRecord);
            Assert.Contains(gameId.ToString(), json);
            Assert.Contains(playerId.ToString(), json);
            Assert.Contains("\"Result\":1", json);
            Assert.Contains("\"Format\":0", json);
            Assert.Contains(";[Game Modus 'Portes']", json);
        }

        [Fact]
        public void GameRecordIsDeserializedProperly()
        {
            var recordPath = Path.Combine("Data", "GameRecord.json");
            var recordJsonStr = File.ReadAllText(recordPath);

            var gameRecord = JsonConvert.DeserializeObject<GameRecordContract>(recordJsonStr);
            Assert.NotNull(gameRecord);
            Assert.Equal(Guid.Parse("c57e0961-02e7-4aac-857f-565e9d78db09"), gameRecord.Id);
            Assert.Equal(Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba"), gameRecord.PlayerId);
            Assert.Equal(GameResult.Gammon, gameRecord.Result);
            Assert.Null(gameRecord.DoublingCubeValue);
            Assert.Equal(0, gameRecord.PipesLeft);
            Assert.Equal(HistoryFormat.MAT, gameRecord.Format);
            Assert.StartsWith(";[Game 'c57e0961-02e7-4aac-857f-565e9d78db09']", gameRecord.GameHistory);
        }

        [Fact]
        public void MatchRecordIsSerializedProperly()
        {
            var matchId = Guid.Parse("888a356e-e09f-4a0f-b909-581f1ffb167e");
            var playerId = Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8");

            var gameId = Guid.Parse("c57e0961-02e7-4aac-857f-565e9d78db09");
            var gameHistoryPath = Path.Combine("Data", "PortesGameHistory.txt");
            var gameHistory = File.ReadAllText(gameHistoryPath);
            var gameRecord = new GameRecordContract()
            {
                Id = gameId,
                PlayerId = playerId,
                Result = GameResult.Gammon,
                DoublingCubeValue = null,
                PipesLeft = 0,
                Format = HistoryFormat.MAT,
                GameHistory = gameHistory
            };

            var matchHistorypath = Path.Combine("Data", "TavliMatchHistory.txt");
            var matchHistory = File.ReadAllText(matchHistorypath);
            var matchRecord = new MatchRecordContract()
            {
                Id = matchId,
                PlayerId = playerId,
                Result = MatchResult.Won,
                Modus = MatchModus.Normal,
                Type = MatchType.CashGame,
                Variant = MatchVariant.Tavli,
                Games = new GameRecordContract[] { gameRecord },
                Format = HistoryFormat.MAT,
                MatchHistory = matchHistory
            };

            var json = JsonConvert.SerializeObject(matchRecord);
            Assert.Contains(playerId.ToString(), json);
            // game
            Assert.Contains(gameId.ToString(), json);
            Assert.Contains("\"Result\":1", json);
            Assert.Contains("\"Format\":0", json);
            Assert.Contains(";[Game Modus 'Portes']", json);
            // match
            Assert.Contains(";[Match '888a356e-e09f-4a0f-b909-581f1ffb167e']", json);
            Assert.Contains(matchId.ToString(), json);
            Assert.Contains("\"Result\":1", json);
            Assert.Contains("\"Variant\":2", json);
            Assert.Contains("\"Type\":2", json);
            Assert.Contains("\"Modus\":0", json);
        }

        [Fact]
        public void MatchRecordIsDeserializedProperly()
        {
            var recordPath = Path.Combine("Data", "MatchRecord.json");
            var recordJsonStr = File.ReadAllText(recordPath);

            var matchRecord = JsonConvert.DeserializeObject<MatchRecordContract>(recordJsonStr);
            Assert.NotNull(matchRecord);
            Assert.NotEmpty(matchRecord.Games);
            Assert.Equal(Guid.Parse("888a356e-e09f-4a0f-b909-581f1ffb167e"), matchRecord.Id);
            Assert.Equal(Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8"), matchRecord.PlayerId);
            Assert.Equal(MatchResult.Won, matchRecord.Result);
            Assert.Equal(MatchModus.Normal, matchRecord.Modus);
            Assert.Equal(MatchType.CashGame, matchRecord.Type);
            Assert.Equal(MatchVariant.Tavli, matchRecord.Variant);
            Assert.Equal(HistoryFormat.MAT, matchRecord.Format);
            Assert.StartsWith(";[Match '888a356e-e09f-4a0f-b909-581f1ffb167e']", matchRecord.MatchHistory);

            var gameRecord = matchRecord.Games.First();
            Assert.NotNull(gameRecord);
            Assert.Equal(Guid.Parse("c57e0961-02e7-4aac-857f-565e9d78db09"), gameRecord.Id);
            Assert.Equal(Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8"), gameRecord.PlayerId);
            Assert.Equal(GameResult.Gammon, gameRecord.Result);
            Assert.Null(gameRecord.DoublingCubeValue);
            Assert.Equal(0, gameRecord.PipesLeft);
            Assert.Equal(HistoryFormat.MAT, gameRecord.Format);
            Assert.StartsWith(";[Game 'c57e0961-02e7-4aac-857f-565e9d78db09']", gameRecord.GameHistory);
        }
    }
}
