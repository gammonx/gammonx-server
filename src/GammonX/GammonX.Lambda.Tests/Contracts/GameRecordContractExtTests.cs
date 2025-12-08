using GammonX.Lambda.Extensions;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;
using GammonX.Models.History;

using Moq;

using Xunit;

namespace GammonX.Lambda.Tests.Contracts
{
    public class GameRecordContractExtTests
    {
        [Fact]
        public void ToGameMapsAllFieldsCorrectly()
        {
            // create stable ids
            var gameId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            var matchId = Guid.NewGuid();

            var contract = new GameRecordContract
            {
                Id = gameId,
                PlayerId = playerId,
                MatchId = matchId,
                PipesLeft = 3,
                Result = GameResult.Single,
                DoublingCubeValue = 2
            };

            // prepare history object with controlled values
            var history = new Mock<IParsedGameHistory>();
            history.Setup(h => h.Points).Returns(4);
            history.Setup(h => h.Modus).Returns(GameModus.Portes);

            // use realistic timestamps to validate duration mapping
            var start = DateTime.UtcNow.AddMinutes(-3);
            var end = DateTime.UtcNow;
            history.Setup(h => h.StartedAt).Returns(start);
            history.Setup(h => h.EndedAt).Returns(end);
            history.Setup(h => h.Duration()).Returns(end - start);

            history.Setup(h => h.DoubleDiceCount(playerId)).Returns(2);
            history.Setup(h => h.TurnCount(playerId)).Returns(15);

            var item = contract.ToGame(history.Object);

            Assert.Equal(gameId, item.Id);
            Assert.Equal(playerId, item.PlayerId);
            Assert.Equal(matchId, item.MatchId);
            Assert.Equal(4, item.Points);
            Assert.Equal(15, item.Length);
            Assert.Equal(GameModus.Portes, item.Modus);
            Assert.Equal(start, item.StartedAt);
            Assert.Equal(end, item.EndedAt);
            Assert.Equal(end - start, item.Duration);
            Assert.Equal(3, item.PipesLeft);
            Assert.Equal(2, item.DiceDoubles);
            Assert.Equal(GameResult.Single, item.Result);
            Assert.Equal(2, item.DoublingCubeValue);
        }

        [Fact]
        public void ToGameAllowsNullDoublingCube()
        {
            var contract = new GameRecordContract
            {
                Id = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),
                MatchId = Guid.NewGuid(),
                DoublingCubeValue = null
            };

            // minimal setup because only null behavior matters
            var history = new Mock<IParsedGameHistory>();
            history.Setup(h => h.Points).Returns(1);
            history.Setup(h => h.Modus).Returns(GameModus.Portes);
            history.Setup(h => h.Duration()).Returns(TimeSpan.FromSeconds(5));
            history.Setup(h => h.StartedAt).Returns(DateTime.UtcNow.AddSeconds(-5));
            history.Setup(h => h.EndedAt).Returns(DateTime.UtcNow);
            history.Setup(h => h.DoubleDiceCount(contract.PlayerId)).Returns(0);
            history.Setup(h => h.TurnCount(contract.PlayerId)).Returns(3);

            var item = contract.ToGame(history.Object);

            Assert.Null(item.DoublingCubeValue);
        }

        [Fact]
        public void ToGameHandlesNegativePointsAndTurns()
        {
            var contract = new GameRecordContract
            {
                Id = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),
                MatchId = Guid.NewGuid()
            };

            // negative values are unusual and must still map directly
            var history = new Mock<IParsedGameHistory>();
            history.Setup(h => h.Points).Returns(-5);
            history.Setup(h => h.Modus).Returns(GameModus.Portes);
            history.Setup(h => h.Duration()).Returns(TimeSpan.Zero);
            history.Setup(h => h.StartedAt).Returns(DateTime.UtcNow);
            history.Setup(h => h.EndedAt).Returns(DateTime.UtcNow);
            history.Setup(h => h.DoubleDiceCount(contract.PlayerId)).Returns(-1);
            history.Setup(h => h.TurnCount(contract.PlayerId)).Returns(-10);

            var item = contract.ToGame(history.Object);

            Assert.Equal(-5, item.Points);
            Assert.Equal(-10, item.Length);
            Assert.Equal(-1, item.DiceDoubles);
        }

        [Fact]
        public void ToGameHandlesExtremeDurationValues()
        {
            var contract = new GameRecordContract
            {
                Id = Guid.NewGuid(),
                PlayerId = Guid.NewGuid(),
                MatchId = Guid.NewGuid()
            };

            // extremely long game duration
            var start = DateTime.UtcNow.AddHours(-30);
            var end = DateTime.UtcNow;

            var history = new Mock<IParsedGameHistory>();
            history.Setup(h => h.Points).Returns(3);
            history.Setup(h => h.Modus).Returns(GameModus.Backgammon);
            history.Setup(h => h.StartedAt).Returns(start);
            history.Setup(h => h.EndedAt).Returns(end);
            history.Setup(h => h.Duration()).Returns(end - start);
            history.Setup(h => h.DoubleDiceCount(contract.PlayerId)).Returns(0);
            history.Setup(h => h.TurnCount(contract.PlayerId)).Returns(100);

            var item = contract.ToGame(history.Object);

            Assert.Equal(end - start, item.Duration);
            Assert.Equal(start, item.StartedAt);
            Assert.Equal(end, item.EndedAt);
        }

        [Fact]
        public void ToGameHistoryMapsFieldsCorrectly()
        {
            // using fixed ids for stable PK comparisons
            var gameId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            // creating a contract with values to be mapped
            var contract = new GameRecordContract
            {
                Id = gameId,
                GameHistory = "some-history",
                Format = HistoryFormat.MAT
            };

            // mapping to a game history item
            var item = contract.ToGameHistory();

            // verifying direct mappings
            Assert.Equal(gameId, item.GameId);
            Assert.Equal("some-history", item.Data);
            Assert.Equal(HistoryFormat.MAT, item.Format);
        }

        [Fact]
        public void ToGameHistoryCreatesCorrectPrimaryKey()
        {
            var gameId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var contract = new GameRecordContract { Id = gameId };

            var item = contract.ToGameHistory();

            // pk must follow factory format (e.g. GAME#{id})
            var expectedPk = $"GAME#{gameId}";
            Assert.Equal(expectedPk, item.PK);
        }

        [Fact]
        public void ToGameHistoryCreatesCorrectSortKey()
        {
            var contract = new GameRecordContract { Id = Guid.NewGuid() };

            var item = contract.ToGameHistory();

            // sk must follow factory format (e.g. HISTORY)
            Assert.Equal("HISTORY", item.SK);
        }

        [Fact]
        public void ToGameHistoryHandlesEmptyHistoryGracefully()
        {
            var contract = new GameRecordContract
            {
                Id = Guid.NewGuid(),
                GameHistory = string.Empty,
                Format = HistoryFormat.Unknown
            };

            var item = contract.ToGameHistory();

            // verifying empty data maps correctly
            Assert.Equal(string.Empty, item.Data);
            Assert.Equal(HistoryFormat.Unknown, item.Format);
        }
    }
}
