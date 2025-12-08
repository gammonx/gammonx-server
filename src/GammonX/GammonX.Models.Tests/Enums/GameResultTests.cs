using GammonX.Models.Enums;

namespace GammonX.Models.Tests.Enums
{
    public class GameResultTests
    {
        [Fact]
        public void HasWonReturnsTrueForWinningResults()
        {
            // verifying all winning outcomes
            Assert.True(GameResult.Single.HasWon());
            Assert.True(GameResult.Gammon.HasWon());
            Assert.True(GameResult.Backgammon.HasWon());
            Assert.True(GameResult.DoubleDeclined.HasWon());
            Assert.True(GameResult.Resign.HasWon());
        }

        [Fact]
        public void HasWonReturnsFalseForLosingResults()
        {
            // verifying all losing outcomes
            Assert.False(GameResult.LostSingle.HasWon());
            Assert.False(GameResult.LostGammon.HasWon());
            Assert.False(GameResult.LostBackgammon.HasWon());
            Assert.False(GameResult.LostDoubleDeclined.HasWon());
            Assert.False(GameResult.LostResign.HasWon());
        }

        [Fact]
        public void HasWonReturnsNullForUnknownOrDraw()
        {
            // unknown is not a win or loss
            Assert.Null(GameResult.Unknown.HasWon());
            // draw is neither win nor loss
            Assert.Null(GameResult.Draw.HasWon());
        }

        [Fact]
        public void HasWonReturnsNullForUndefinedEnumValue()
        {
            // undefined enum values should fall into default branch
            var undefined = (GameResult)(-1);
            Assert.Null(undefined.HasWon());
        }
    }
}
