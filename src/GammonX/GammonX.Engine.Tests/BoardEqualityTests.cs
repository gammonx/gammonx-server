using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Contracts;
using GammonX.Models.Enums;

namespace GammonX.Engine.Tests
{
    public class BoardEqualityTests
    {
        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void ClonedBoardsAreEqualAndHaveTheSameHash(GameModus modus)
        {
            var board = CreateBoard(modus);
            var clone = (IBoardModel)((ICloneable)board).Clone();

            Assert.True(board.Equals(clone));
            Assert.Equal(board.GetHashCode(), clone.GetHashCode());
            Assert.False(board.Equals(null));
            Assert.False(board.Equals(new object()));

            var boards = new HashSet<IBoardModel> { board };
            Assert.Contains(clone, boards);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void FieldDifferencesMakeBoardsUnequal(GameModus modus)
        {
            var first = CreateBoard(modus);
            var second = CreateBoard(modus);
            first.Fields[0] = -1;
            second.Fields[0] = 1;

            Assert.False(first.Equals(second));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Plakoto)]
        [InlineData(GameModus.Fevga)]
        public void BearOffDifferencesMakeBoardsUnequal(GameModus modus)
        {
            var first = CreateBoard(modus, contract => contract.BearOffCountWhite = 1);
            var second = CreateBoard(modus, contract => contract.BearOffCountWhite = 2);

            Assert.False(first.Equals(second));
        }

        [Fact]
        public void DifferentBoardVariantsAreUnequal()
        {
            var backgammon = CreateBoard(GameModus.Backgammon);
            var portes = CreateBoard(GameModus.Portes);

            Assert.False(backgammon.Equals(portes));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Fevga)]
        public void HomeBarDifferencesMakeBoardsUnequal(GameModus modus)
        {
            var first = CreateBoard(modus, contract => contract.HomeBarCountWhite = 1);
            var second = CreateBoard(modus, contract => contract.HomeBarCountWhite = 2);

            Assert.False(first.Equals(second));
        }

        [Fact]
        public void DoublingCubeDifferencesMakeBackgammonBoardsUnequal()
        {
            var first = CreateBoard(GameModus.Backgammon, contract =>
            {
                contract.DoublingCubeValue = 2;
                contract.DoublingCubeOwner = true;
            });
            var differentValue = CreateBoard(GameModus.Backgammon, contract =>
            {
                contract.DoublingCubeValue = 4;
                contract.DoublingCubeOwner = true;
            });
            var differentOwner = CreateBoard(GameModus.Backgammon, contract =>
            {
                contract.DoublingCubeValue = 2;
                contract.DoublingCubeOwner = false;
            });

            Assert.False(first.Equals(differentValue));
            Assert.False(first.Equals(differentOwner));
        }

        [Fact]
        public void PinnedFieldDifferencesMakePlakotoBoardsUnequal()
        {
            var first = CreateBoard(GameModus.Plakoto, contract => contract.PinnedFields![6] = 1);
            var second = CreateBoard(GameModus.Plakoto, contract => contract.PinnedFields![6] = -1);

            Assert.False(first.Equals(second));
        }

        [Fact]
        public void HistoryDoesNotAffectBoardEquality()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            var board = CreateBoard(GameModus.Portes);
            var equivalent = (IBoardModel)((ICloneable)board).Clone();

            service.AddRollEventToHistory(board, true, [1, 2]);

            Assert.True(board.Equals(equivalent));
            Assert.Equal(board.GetHashCode(), equivalent.GetHashCode());
        }

        private static IBoardModel CreateBoard(GameModus modus, Action<BoardModelContract>? configure = null)
        {
            var contract = new BoardModelContract
            {
                Fields = new int[24],
                PinnedFields = new int[24],
                DoublingCubeValue = 1
            };
            configure?.Invoke(contract);
            return BoardServiceFactory.Create(modus).CreateBoard(contract);
        }
    }
}
