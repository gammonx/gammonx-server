using GammonX.Models.Enums;
using GammonX.Models.Helpers;
using GammonX.Models.History;

namespace GammonX.Models.Tests
{
    public class HistoryParserTests
    {
        // TODO :: add unit tests for all variants and types

        [Fact]
        public void UnknownParserThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => HistoryParserFactory.Create<IMatchHistoryParser>(Enums.HistoryFormat.Unknown));
            Assert.Throws<InvalidOperationException>(() => HistoryParserFactory.Create<IGameHistoryParser>(Enums.HistoryFormat.Unknown));
        }

        [Fact]
        public void MATTavliMatchIsParsedProperly()
        {
            var parser = HistoryParserFactory.Create<IMatchHistoryParser>(HistoryFormat.MAT);

            var historyPath = Path.Combine("Data", "TavliMatchHistory.txt");
            var history = File.ReadAllText(historyPath);

            var match = parser.ParseMatch(history);

            var whitePlayer = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
            var blackPlayer = Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8");

            Assert.NotNull(match);
            Assert.Equal(Guid.Parse("888a356e-e09f-4a0f-b909-581f1ffb167e"), match.Id);
            Assert.Equal("Tavli Normal CashGame", match.Name);
            Assert.Equal(3, match.Games.Count);
            Assert.Equal(whitePlayer, match.Player1Id);
            Assert.Equal(blackPlayer, match.Player2Id);
            Assert.Equal(HistoryFormat.MAT, match.Format);
            Assert.Equal(3, match.Length);
            Assert.Equal(DateTimeHelper.ParseFlexible("29/11/2025 09:33:37"), match.StartedAt);
            Assert.Equal(DateTimeHelper.ParseFlexible("29/11/2025 09:33:41"), match.EndedAt);
            Assert.Equal(3, match.PointCount(whitePlayer));
            Assert.Equal(1, match.PointCount(blackPlayer));
            Assert.Equal(11, match.AvgDoubleDiceCount(whitePlayer));
            Assert.Equal(7.6666666666666666666666666666667, match.AvgDoubleDiceCount(blackPlayer));
            Assert.Equal(TimeSpan.FromSeconds(1.3333333333333333333333333333333), match.AvgDuration());
            Assert.Equal(39, match.AvgTurnCount(whitePlayer));
            Assert.Equal(39, match.AvgTurnCount(blackPlayer));
            Assert.Equal(0, match.AvgDoubleOfferCount(whitePlayer));
            Assert.Equal(0, match.AvgDoubleOfferCount(blackPlayer));
        }

        [Fact]
        public void MATPortesGameIsParsedProperly()
        {
            var parser = HistoryParserFactory.Create<IGameHistoryParser>(HistoryFormat.MAT);

            var historyPath = Path.Combine("Data", "PortesGameHistory.txt");
            var history = File.ReadAllText(historyPath);

            var game = parser.ParseGame(history);

            var whitePlayer = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
            var blackPlayer = Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8");

            var expStartAt = DateTimeHelper.ParseFlexible("29/11/2025 09:33:37");
            var expEndedAt = DateTimeHelper.ParseFlexible("29/11/2025 09:33:37");

            Assert.NotNull(game);
            Assert.Equal(HistoryFormat.MAT, game.Format);
            Assert.Equal(blackPlayer, game.Winner);
            Assert.Equal(1, game.Points);
            Assert.Equal(expStartAt, game.StartedAt);
            Assert.Equal(expEndedAt, game.EndedAt);
            Assert.Equal(GameModus.Portes, game.Modus);
            Assert.Equal(7, game.DoubleDiceCount(whitePlayer));
            Assert.Equal(4, game.DoubleDiceCount(blackPlayer));
            Assert.Equal(28, game.TurnCount(whitePlayer));
            Assert.Equal(28, game.TurnCount(blackPlayer));
            var duration = expStartAt - expEndedAt;
            Assert.Equal(duration, game.Duration());
        }

        [Fact]
        public void MATPlakotoGameIsParsedProperly()
        {
            var parser = HistoryParserFactory.Create<IGameHistoryParser>(HistoryFormat.MAT);

            var historyPath = Path.Combine("Data", "PlakotoGameHistory.txt");
            var history = File.ReadAllText(historyPath);

            var game = parser.ParseGame(history);

            var whitePlayer = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
            var blackPlayer = Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8");

            var expStartAt = DateTimeHelper.ParseFlexible("29/11/2025 09:33:37");
            var expEndedAt = DateTimeHelper.ParseFlexible("29/11/2025 09:33:39");

            Assert.NotNull(game);
            Assert.Equal(HistoryFormat.MAT, game.Format);
            Assert.Equal(whitePlayer, game.Winner);
            Assert.Equal(2, game.Points);
            Assert.Equal(expStartAt, game.StartedAt);
            Assert.Equal(expEndedAt, game.EndedAt);
            Assert.Equal(GameModus.Plakoto, game.Modus);
            Assert.Equal(12, game.DoubleDiceCount(whitePlayer));
            Assert.Equal(12, game.DoubleDiceCount(blackPlayer));
            Assert.Equal(44, game.TurnCount(whitePlayer));
            Assert.Equal(44, game.TurnCount(blackPlayer));
            var duration = expEndedAt - expStartAt;
            Assert.Equal(duration, game.Duration());
        }

        [Fact]
        public void MATFevgaGameIsParsedProperly()
        {
            var parser = HistoryParserFactory.Create<IGameHistoryParser>(HistoryFormat.MAT);

            var historyPath = Path.Combine("Data", "FevgaGameHistory.txt");
            var history = File.ReadAllText(historyPath);

            var game = parser.ParseGame(history);

            var whitePlayer = Guid.Parse("cf0ab132-2279-43d3-911f-ed139ce5e7ba");
            var blackPlayer = Guid.Parse("e51f307e-3bf6-4408-b4b7-5fabd41b57b8");

            var expStartAt = DateTimeHelper.ParseFlexible("29/11/2025 09:33:39");
            var expEndedAt = DateTimeHelper.ParseFlexible("29/11/2025 09:33:41");

            Assert.NotNull(game);
            Assert.Equal(HistoryFormat.MAT, game.Format);
            Assert.Equal(whitePlayer, game.Winner);
            Assert.Equal(1, game.Points);
            Assert.Equal(expStartAt, game.StartedAt);
            Assert.Equal(expEndedAt, game.EndedAt);
            Assert.Equal(GameModus.Fevga, game.Modus);
            Assert.Equal(14, game.DoubleDiceCount(whitePlayer));
            Assert.Equal(7, game.DoubleDiceCount(blackPlayer));
            Assert.Equal(46, game.TurnCount(whitePlayer));
            Assert.Equal(45, game.TurnCount(blackPlayer));
            var duration = expEndedAt - expStartAt;
            Assert.Equal(duration, game.Duration());
        }
    }
}
