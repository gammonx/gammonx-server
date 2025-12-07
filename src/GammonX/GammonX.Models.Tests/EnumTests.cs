using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Models.Tests
{
    public class EnumTests
    {
        [Fact]
        public void EnsureBoardPositionValues()
        {
            Assert.Equal(-100, BoardPositions.BearOffWhite);
            Assert.Equal(100, BoardPositions.BearOffBlack);
            Assert.Equal(-1, BoardPositions.HomeBarWhite);
            Assert.Equal(24, BoardPositions.HomeBarBlack);
        }

        [Fact]
        public void EnsureGameModusValues()
        {
            Assert.Equal(0, (int)GameModus.Backgammon);
            Assert.Equal(1, (int)GameModus.Tavla);
            Assert.Equal(2, (int)GameModus.Portes);
            Assert.Equal(3, (int)GameModus.Plakoto);
            Assert.Equal(4, (int)GameModus.Fevga);
            Assert.Equal(99, (int)GameModus.Unknown);
        }

        [Fact]
        public void EnsureGameResultValues()
        {
            Assert.Equal(0, (int)GameResult.Single);
            Assert.Equal(1, (int)GameResult.Gammon);
            Assert.Equal(2, (int)GameResult.Backgammon);
            Assert.Equal(97, (int)GameResult.LostDoubleDeclined);
            Assert.Equal(98, (int)GameResult.LostSingle);
            Assert.Equal(99, (int)GameResult.Unknown);
        }

        [Fact]
        public void EnsureProperGameResultIsReturned()
        {
            Assert.True(GameResult.Single.HasWon());
            Assert.True(GameResult.Gammon.HasWon());
            Assert.True(GameResult.Backgammon.HasWon());
            Assert.False(GameResult.LostDoubleDeclined.HasWon());
            Assert.False(GameResult.LostSingle.HasWon());
            Assert.Null(GameResult.Unknown.HasWon());
        }

        [Fact]
        public void EnsureMatchResultValues()
        {
            Assert.Equal(1, (int)MatchResult.Won);
            Assert.Equal(2, (int)MatchResult.Lost);
            Assert.Equal(99, (int)MatchResult.Unknown);
        }

        [Fact]
        public void EnsurePropertMatchResultIsReturned()
        {
            Assert.True(MatchResult.Won.HasWon());
            Assert.False(MatchResult.Lost.HasWon());
            Assert.Null(MatchResult.Unknown.HasWon());
        }

        [Fact]
        public void EnsureMatchTypeValues()
        {
            Assert.Equal(0, (int)MatchType.FivePointGame);
            Assert.Equal(1, (int)MatchType.SevenPointGame);
            Assert.Equal(2, (int)MatchType.CashGame);
            Assert.Equal(99, (int)MatchType.Unknown);
        }

        [Fact]
        public void EnsureMatchModusValues()
        {
            Assert.Equal(0, (int)MatchModus.Normal);
            Assert.Equal(1, (int)MatchModus.Ranked);
            Assert.Equal(2, (int)MatchModus.Bot);
            Assert.Equal(99, (int)MatchModus.Unknown);
        }

        [Fact]
        public void EnsureHistoryFormatValues()
        {
            Assert.Equal(0, (int)HistoryFormat.MAT);
            Assert.Equal(99, (int)HistoryFormat.Unknown);
        }
    }
}