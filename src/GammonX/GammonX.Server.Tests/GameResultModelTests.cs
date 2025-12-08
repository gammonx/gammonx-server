using GammonX.Models.Enums;

using GammonX.Server.Models;

namespace GammonX.Server.Tests
{
    public class GameResultModelTests
    {
        [Fact]
        public void ConstructorShouldInitializeProperties()
        {
            var winnerId = Guid.NewGuid();

            var model = new GameResultModel(
                winnerId,
                GameResult.Gammon,
                GameResult.LostGammon,
                5
            );

            Assert.Equal(winnerId, model.WinnerId);
            Assert.Equal(GameResult.Gammon, model.WinnerResult);
            Assert.Equal(GameResult.LostGammon, model.LoserResult);
            Assert.Equal(5, model.Points);
        }

        [Fact]
        public void EmptyShouldReturnDefaultValues()
        {
            var empty = GameResultModel.Empty();

            Assert.Equal(Guid.Empty, empty.WinnerId);
            Assert.Equal(GameResult.Unknown, empty.WinnerResult);
            Assert.Equal(GameResult.Unknown, empty.LoserResult);
            Assert.Equal(0, empty.Points);
        }

        [Fact]
        public void GetResultShouldReturnWinnerResultForWinner()
        {
            var winnerId = Guid.NewGuid();

            var model = new GameResultModel(
                winnerId,
                GameResult.Gammon,
                GameResult.LostGammon,
                3
            );

            Assert.Equal(GameResult.Gammon, model.GetResult(winnerId));
        }

        [Fact]
        public void GetResultShouldReturnLoserResultForLoser()
        {
            var winnerId = Guid.NewGuid();
            var loserId = Guid.NewGuid();

            var model = new GameResultModel(
                winnerId,
                GameResult.Backgammon,
                GameResult.LostGammon,
                6
            );

            Assert.Equal(GameResult.LostGammon, model.GetResult(loserId));
        }

        [Fact]
        public void IsDrawShouldBeTrueWhenBothAreDrawAndWinnerIdEmpty()
        {
            var model = GameResultModel.Draw();

            Assert.True(model.IsDraw);
        }

        [Fact]
        public void IsDrawShouldBeFalseWhenWinnerIdIsNotEmpty()
        {
            var model = new GameResultModel(
                Guid.NewGuid(),
                GameResult.Draw,
                GameResult.Draw,
                0
            );

            Assert.False(model.IsDraw);
        }

        [Fact]
        public void IsDrawShouldBeFalseWhenResultsMismatch()
        {
            var model = new GameResultModel(
                Guid.Empty,
                GameResult.Gammon,
                GameResult.Draw,
                0
            );

            Assert.False(model.IsDraw);
        }

        [Fact]
        public void IsConcludedShouldBeTrueWhenEqualToEmpty()
        {
            var empty = GameResultModel.Empty();

            Assert.False(empty.IsConcluded);
        }

        [Fact]
        public void IsConcludedShouldBeFalseWhenDifferentFromEmpty()
        {
            var model = new GameResultModel(
                Guid.NewGuid(),
                GameResult.Single,
                GameResult.LostSingle,
                1
            );

            Assert.True(model.IsConcluded);
        }

        [Fact]
        public void StructEqualityForIsConcludedWorksOnlyWhenFieldsMatch()
        {
            var empty1 = GameResultModel.Empty();
            var empty2 = GameResultModel.Empty();

            Assert.True(empty1.Equals(empty2));
            Assert.False(empty1.IsConcluded);
            Assert.False(empty2.IsConcluded);
        }

        [Fact]
        public void PointsShouldBeZeroWhenDraw()
        {
            var model = new GameResultModel(
                Guid.Empty,
                GameResult.Draw,
                GameResult.Draw,
                0
            );

            Assert.Equal(0, model.Points);
        }
    }
}
