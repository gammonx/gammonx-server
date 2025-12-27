using GammonX.Engine.History;
using GammonX.Engine.Models;
using GammonX.Engine.Services;
using GammonX.Models.Enums;
using GammonX.Server.Contracts;
using GammonX.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GammonX.Server.Tests
{
    public class DiceRollsModelTests
    {
        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void TryUseDiceExactSingleDieMatchUsesThatDie(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var dices = new DiceRollsModel
            {
                new TestDiceRoll(5),
                new TestDiceRoll(3)
            };

            // fevga does only move in one direction, but for this test it does not matter
            var result = dices.TryUseDice(board, from: 5, to: 10);

            Assert.True(result);
            Assert.True(dices.Single(d => d.Roll == 5).Used);
            Assert.False(dices.Single(d => d.Roll == 3).Used);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void TryUseDiceCombinedDiceMatchUsesMultipleDice(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var dices = new DiceRollsModel
            {
                new TestDiceRoll(2),
                new TestDiceRoll(4),
                new TestDiceRoll(6)
            };

            // fevga does only move in one direction, but for this test it does not matter
            var result = dices.TryUseDice(board, from: 4, to: 10); // distance 6

            Assert.True(result);
            Assert.True(dices.Single(d => d.Roll == 2).Used);
            Assert.True(dices.Single(d => d.Roll == 4).Used);
            Assert.False(dices.Single(d => d.Roll == 6).Used);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void TryUseDiceDiceOrderDoesNotMatter(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var dices = new DiceRollsModel
            {
                new TestDiceRoll(6),
                new TestDiceRoll(1),
                new TestDiceRoll(5)
            };

            // fevga does only move in one direction, but for this test it does not matter
            var result = dices.TryUseDice(board, from: 0, to: 11); // distance 11

            Assert.True(result);
            Assert.Equal(11, dices.Where(d => d.Used).Sum(d => d.Roll));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void TryUseDicePaschUsesAllDiceWhenNeeded(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var dices = new DiceRollsModel
            {
                new TestDiceRoll(3),
                new TestDiceRoll(3),
                new TestDiceRoll(3),
                new TestDiceRoll(3)
            };

            var result = dices.TryUseDice(board, from: 12, to: 0); // distance 12

            Assert.True(result);
            Assert.Equal(4, dices.Count(d => d.Used));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void TryUseDicePaschAllowsPartialUsage(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var dices = new DiceRollsModel
            {
                new TestDiceRoll(3),
                new TestDiceRoll(3),
                new TestDiceRoll(3),
                new TestDiceRoll(3)
            };

            // fevga does only move in one direction, but for this test it does not matter
            var result = dices.TryUseDice(board, from: 0, to: 6); // distance 6

            Assert.True(result);
            Assert.Equal(2, dices.Count(d => d.Used));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Plakoto)]
        public void TryUseDiceBearOffUsesSmallestSufficientDie(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var dices = new DiceRollsModel
            {
                new TestDiceRoll(6),
                new TestDiceRoll(3)
            };

            var result = dices.TryUseDice(board, from: 21, to: BoardPositions.BearOffWhite);

            Assert.True(result);
            Assert.True(dices.Single(d => d.Roll == 3).Used);
            Assert.False(dices.Single(d => d.Roll == 6).Used);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Plakoto)]
        public void TryUseDiceBearOffAllowsCombinedDice(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var dices = new DiceRollsModel
            {
                new TestDiceRoll(2),
                new TestDiceRoll(4)
            };

            // distance 5
            var result = dices.TryUseDice(board, from: 19, to: BoardPositions.BearOffWhite);

            Assert.True(result);
            Assert.Equal(2, dices.Count(d => d.Used));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void TryUseDiceNoValidCombinationThrows(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();
            var dices = new DiceRollsModel
            {
                new TestDiceRoll(2),
                new TestDiceRoll(3)
            };

            var result = dices.TryUseDice(board, from: 10, to: 4);
            Assert.False(result);
            Assert.All(dices, d => Assert.False(d.Used));
        }

        [Fact]
        public void UndoDiceRollRestoresUsedDie()
        {
            var dices = new DiceRollsModel
            {
                new TestDiceRoll(4),
                new TestDiceRoll(6)
            };

            dices[0].Used = true;

            dices.UndoDiceRoll(4);

            Assert.False(dices[0].Used);
        }

        [Fact]
        public void UndoDiceRollOnUnusedDieThrows()
        {
            var dices = new DiceRollsModel
            {
                new TestDiceRoll(4)
            };

            Assert.Throws<InvalidOperationException>(() => dices.UndoDiceRoll(4));
        }

        [Fact]
        public void GetUnusedDiceRollsReturnsOnlyUnusedDice()
        {
            var dices = new DiceRollsModel
            {
                new TestDiceRoll(1) { Used = true },
                new TestDiceRoll(2),
                new TestDiceRoll(3)
            };

            var unused = dices.GetUnusedDiceRolls();

            Assert.Equal(2, unused.Length);
            Assert.Contains(unused, d => d.Roll == 2);
            Assert.Contains(unused, d => d.Roll == 3);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void BearOffUsesExactDieFirst(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();

            var dices = new DiceRollsModel
            {
                new TestDiceRoll(5),
                new TestDiceRoll(6)
            };

            var result = dices.TryUseDice(board, from: 18, to: BoardPositions.BearOffWhite);

            Assert.True(result);
            Assert.True(dices.Single(d => d.Roll == 6).Used);
            Assert.False(dices.Single(d => d.Roll == 5).Used);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void NextMoveUsesRemainingDiceAfterBearOff(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();

            var dices = new DiceRollsModel
            {
                new TestDiceRoll(5),
                new TestDiceRoll(6)
            };

            // bear-off first
            dices.TryUseDice(board, from: 18, to: BoardPositions.BearOffWhite);

            // next move: 18 > 23 (distance = 5)
            var result2 = dices.TryUseDice(board, from: 18, to: 23);

            Assert.True(result2);
            Assert.True(dices.Single(d => d.Roll == 5).Used);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void CannotUseDiceIfDistanceExceedsRemainingDice(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();

            var dices = new DiceRollsModel
            {
                new TestDiceRoll(2),
                new TestDiceRoll(3)
            };

            // attempt to move distance 6 (requires 2+3 < 6)
            var result = dices.TryUseDice(board, from: 18, to: BoardPositions.BearOffWhite);

            Assert.False(result);
            Assert.All(dices, d => Assert.False(d.Used));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void UndoDiceRollRestoresDice(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();

            var dices = new DiceRollsModel
            {
                new TestDiceRoll(5),
                new TestDiceRoll(6)
            };

            // use a dice
            dices.TryUseDice(board, from: 18, to: 23);
            Assert.True(dices.Single(d => d.Roll == 5).Used);

            // undo
            dices.UndoDiceRoll(5);
            Assert.False(dices.Single(d => d.Roll == 5).Used);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void SingleMoveUsesExactDieWhenAvailable(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();

            var dices = new DiceRollsModel
        {
            new TestDiceRoll(2),
            new TestDiceRoll(5)
        };

            var result = dices.TryUseDice(board, from: 13, to: 18); // distance = 5

            Assert.True(result);
            Assert.True(dices.Single(d => d.Roll == 5).Used);
            Assert.False(dices.Single(d => d.Roll == 2).Used);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void CombinedMoveUsesMultipleDice(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();

            var dices = new DiceRollsModel
        {
            new TestDiceRoll(2),
            new TestDiceRoll(3)
        };

            // distance = 5, requires both dice
            var result = dices.TryUseDice(board, from: 13, to: 18);

            Assert.True(result);
            Assert.All(dices, d => Assert.True(d.Used));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void PartialPaschUsesOnlyNeededDice(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();

            var dices = new DiceRollsModel
        {
            new TestDiceRoll(2),
            new TestDiceRoll(3),
            new TestDiceRoll(4),
            new TestDiceRoll(4) // pasch
        };

            // distance = 6, should use 2 + 4 and leave other 4 unused
            var result = dices.TryUseDice(board, from: 12, to: 18);

            Assert.True(result);
            Assert.Contains(dices, d => d.Roll == 2 && d.Used);
            Assert.Contains(dices, d => d.Roll == 4 && d.Used);
            Assert.Contains(dices, d => d.Roll == 3 && !d.Used);
            Assert.Contains(dices, d => d.Roll == 4 && !d.Used);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void FullPaschUsesAllDiceIfRequired(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();

            var dices = new DiceRollsModel
        {
            new TestDiceRoll(4),
            new TestDiceRoll(4),
            new TestDiceRoll(4),
            new TestDiceRoll(4)
        };

            // distance = 16, requires all four dice
            var result = dices.TryUseDice(board, from: 2, to: 18);

            Assert.True(result);
            Assert.All(dices, d => Assert.True(d.Used));
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void BearOffPrefersExactDieOrSmallestSufficient(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();

            var dices = new DiceRollsModel
        {
            new TestDiceRoll(3),
            new TestDiceRoll(5),
            new TestDiceRoll(6)
        };

            var bearOffPos = BoardPositions.BearOffWhite;

            // distance = 6, should use exact die 6
            var result = dices.TryUseDice(board, from: 18, to: bearOffPos);

            Assert.True(result);
            Assert.True(dices.Single(d => d.Roll == 6).Used);
            Assert.False(dices.Single(d => d.Roll == 5).Used);
            Assert.False(dices.Single(d => d.Roll == 3).Used);
        }

        [Theory]
        [InlineData(GameModus.Backgammon)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void SubsequentMoveUsesRemainingDiceAfterBearOff(GameModus modus)
        {
            var boardService = BoardServiceFactory.Create(modus);
            var board = boardService.CreateBoard();

            var dices = new DiceRollsModel
        {
            new TestDiceRoll(3),
            new TestDiceRoll(5),
            new TestDiceRoll(6)
        };

            var bearOffPos = BoardPositions.BearOffWhite;
            dices.TryUseDice(board, from: 18, to: bearOffPos);

            // remaining dice = 3 and 5
            var result2 = dices.TryUseDice(board, from: 13, to: 18); // distance = 5

            Assert.True(result2);
            Assert.True(dices.Single(d => d.Roll == 5).Used);
            Assert.False(dices.Single(d => d.Roll == 3).Used); // still remaining
        }

        internal sealed class TestDiceRoll : DiceRollContract
        {
            public TestDiceRoll(int roll) : base(roll)
            {
                Roll = roll;
                Used = false;
            }
        }
    }
}
