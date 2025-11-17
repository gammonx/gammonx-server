using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Engine.Tests.Data;
using GammonX.Engine.Tests.Utils;

namespace GammonX.Engine.Tests
{
    public class BoardServiceTests
    {
        #region Backgammon

        [Fact]
        public void BackgammonBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            Assert.Equal(GameModus.Backgammon, service.Modus);
            var boardModel = service.CreateBoard();
            Assert.Equal(GameModus.Backgammon, boardModel.Modus);
        }

        [Fact]
        public void BackgammonBoardIsCreatedProperly()
        {
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.IsAssignableFrom<IBoardModel>(boardModel);
            Assert.Equal(24, boardModel.Fields.Length);
            Assert.Equal(new Range(18, 23), boardModel.HomeRangeWhite);
            Assert.Equal(new Range(5, 0), boardModel.HomeRangeBlack);
			Assert.Equal(new Range(0, 5), boardModel.StartRangeWhite);
			Assert.Equal(new Range(23, 18), boardModel.StartRangeBlack);
			Assert.Equal(0, boardModel.BearOffCountWhite);
            Assert.Equal(0, boardModel.BearOffCountBlack);
            Assert.Equal(15, boardModel.WinConditionCount);
            Assert.Equal(167, boardModel.PipCountWhite);
			Assert.Equal(167, boardModel.PipCountBlack);
			var homebarModel = boardModel as IHomeBarModel;
            Assert.NotNull(homebarModel);
            Assert.Equal(0, homebarModel.HomeBarCountWhite);
            Assert.Equal(0, homebarModel.HomeBarCountBlack);
            Assert.Equal(-1, homebarModel.StartIndexWhite);
            Assert.Equal(24, homebarModel.StartIndexBlack);
			Assert.True(homebarModel.CanSendToHomeBar);
			Assert.True(homebarModel.MustEnterFromHomebar);
			var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.NotNull(doublingCubeModel);
            Assert.Equal(1, doublingCubeModel.DoublingCubeValue);
            Assert.False(doublingCubeModel.DoublingCubeOwner);
            var blockModel = boardModel as IPinModel;
            Assert.Null(blockModel);
        }

        [Fact]
        public void BackgammonBoardStartValuesAreCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.NotNull(boardModel);
            Assert.Equal(-2, boardModel.Fields[0]); // 2 white pieces on point 1
            Assert.Equal(5, boardModel.Fields[5]); // 5 black pieces on point 6
            Assert.Equal(3, boardModel.Fields[7]); // 3 black pieces on point 8
            Assert.Equal(-5, boardModel.Fields[11]); // 5 white pieces on point 12
            Assert.Equal(5, boardModel.Fields[12]); // 5 black pieces on point 13
            Assert.Equal(-3, boardModel.Fields[16]); // 3 white pieces on point 17
            Assert.Equal(-5, boardModel.Fields[18]); // 5 white pieces on point 19
            Assert.Equal(2, boardModel.Fields[23]); // 2 black pieces on point 24
        }

        [Fact]
        public void BackgammonBoardMovesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            var boardModel = service.CreateBoard();

            service.MoveChecker(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            Assert.Equal(-1, boardModel.Fields[0]); // Field 1 should now have 1 white checker
            Assert.Equal(-1, boardModel.Fields[6]); // Field 7 should now have 1 white checker

            service.MoveChecker(boardModel, 23, 6, false); // Move black checker from point 24 to point 18
            Assert.Equal(1, boardModel.Fields[23]); // Field 24 should now have 1 black checker
            Assert.Equal(1, boardModel.Fields[17]); // Field 18 should now have 1 black checker
        }

        [Fact]
        public void BackgammonBoardMovesToCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            var boardModel = service.CreateBoard();

            service.MoveCheckerTo(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            Assert.Equal(-1, boardModel.Fields[0]); // Field 1 should now have 1 white checker
            Assert.Equal(-1, boardModel.Fields[6]); // Field 7 should now have 1 white checker

            service.MoveCheckerTo(boardModel, 23, 17, false); // Move black checker from point 24 to point 18
            Assert.Equal(1, boardModel.Fields[23]); // Field 24 should now have 1 black checker
            Assert.Equal(1, boardModel.Fields[17]); // Field 18 should now have 1 black checker
        }

        [Fact]
        public void BackgammonBoardCanMovePiece()
        {
            var service = BoardServiceFactory.Create(GameModus.Backgammon);
            var boardModel = service.CreateBoard();
            // White can move from point 1 to point 7
            Assert.True(service.CanMoveChecker(boardModel, 0, 6, true));
            // Black can move from point 24 to point 18
            Assert.True(service.CanMoveChecker(boardModel, 23, 6, false));
            // White cannot move from point 1 to point 6 (blocked)
            Assert.False(service.CanMoveChecker(boardModel, 0, 5, true));
            // Black cannot move from point 24 to point 19 (blocked)
            Assert.False(service.CanMoveChecker(boardModel, 23, 5, false));

            // create a single white checker on 1 and 2
            service.MoveChecker(boardModel, 0, 1, true);
            Assert.True(service.CanMoveChecker(boardModel, 5, 4, false));
            Assert.True(service.CanMoveChecker(boardModel, 5, 5, false));

            // create a single black checker on 24 and 23
            service.MoveChecker(boardModel, 23, 1, false);
            Assert.True(service.CanMoveChecker(boardModel, 18, 4, true));
            Assert.True(service.CanMoveChecker(boardModel, 18, 5, true));
        }

        #endregion Backgammon

        #region Tavli

        [Fact]
        public void PlakotoBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            Assert.Equal(GameModus.Plakoto, service.Modus);
            var boardModel = service.CreateBoard();
            Assert.Equal(GameModus.Plakoto, boardModel.Modus);
        }

        [Fact]
        public void PlakotoBoardIsCreatedProperly()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.IsAssignableFrom<IBoardModel>(boardModel);
            Assert.Equal(24, boardModel.Fields.Length);
            Assert.Equal(new Range(18, 23), boardModel.HomeRangeWhite);
            Assert.Equal(new Range(5, 0), boardModel.HomeRangeBlack);
			Assert.Equal(new Range(0, 5), boardModel.StartRangeWhite);
			Assert.Equal(new Range(23, 18), boardModel.StartRangeBlack);
			Assert.Equal(0, boardModel.BearOffCountWhite);
            Assert.Equal(0, boardModel.BearOffCountBlack);
			Assert.Equal(15, boardModel.WinConditionCount);
			Assert.Equal(360, boardModel.PipCountWhite);
			Assert.Equal(360, boardModel.PipCountBlack);
			var homebarModel = boardModel as IHomeBarModel;
            Assert.Null(homebarModel);
            var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.Null(doublingCubeModel);
            var blockModel = boardModel as IPinModel;
            Assert.NotNull(blockModel);
            foreach (var point in blockModel.PinnedFields)
            {
                Assert.Equal(0, point); // There should be no blocked points at game start
            }
        }

        [Fact]
        public void PlakotoBoardStartValuesAreCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.NotNull(boardModel);
            Assert.Equal(-15, boardModel.Fields[0]); // 15 white pieces on point 1
            Assert.Equal(15, boardModel.Fields[23]); // 15 black pieces on point 24
        }

        [Fact]
        public void PlakotoBoardMovesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardModel = service.CreateBoard();

            service.MoveChecker(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            Assert.Equal(-14, boardModel.Fields[0]); // Field 1 should now have 14 white pieces
            Assert.Equal(-1, boardModel.Fields[6]); // Field 7 should now have 1 white checker

            service.MoveChecker(boardModel, 23, 6, false); // Move black checker from point 24 to point 18
            Assert.Equal(14, boardModel.Fields[23]); // Field 24 should now have 14 black pieces
            Assert.Equal(1, boardModel.Fields[17]); // Field 18 should now have 1 black checker
        }

        [Fact]
        public void PlakotoBoardMovesToCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardModel = service.CreateBoard();

            service.MoveCheckerTo(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            Assert.Equal(-14, boardModel.Fields[0]); // Field 1 should now have 14 white pieces
            Assert.Equal(-1, boardModel.Fields[6]); // Field 7 should now have 1 white checker

            service.MoveCheckerTo(boardModel, 23, 17, false); // Move black checker from point 24 to point 18
            Assert.Equal(14, boardModel.Fields[23]); // Field 24 should now have 14 black pieces
            Assert.Equal(1, boardModel.Fields[17]); // Field 18 should now have 1 black checker
        }

        [Fact]
        public void PlakotoBoardCanMovePiece()
        {
            var service = BoardServiceFactory.Create(GameModus.Plakoto);
            var boardModel = service.CreateBoard();
            // White can move from point 1 to point 7
            Assert.True(service.CanMoveChecker(boardModel, 0, 6, true));
            // Black can move from point 24 to point 18
            Assert.True(service.CanMoveChecker(boardModel, 23, 6, false));

            // creating single white checker on point 18
            service.MoveChecker(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            service.MoveChecker(boardModel, 6, 5, true); // Move white checker from point 7 to point 12
            service.MoveChecker(boardModel, 11, 6, true); // Move white checker from point 12 to point 18

            // creating double white checker on point 19
            service.MoveChecker(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            service.MoveChecker(boardModel, 6, 6, true); // Move white checker from point 7 to point 13
            service.MoveChecker(boardModel, 12, 6, true); // Move white checker from point 13 to point 19
            service.MoveChecker(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            service.MoveChecker(boardModel, 6, 6, true); // Move white checker from point 7 to point 13
            service.MoveChecker(boardModel, 12, 6, true); // Move white checker from point 13 to point 19

            // try to move from point 23 to 18 
            Assert.True(service.CanMoveChecker(boardModel, 23, 6, false));
            // try to move from point 23 to 19
            Assert.False(service.CanMoveChecker(boardModel, 23, 5, false));

            // creating single black checker on point 7
            service.MoveChecker(boardModel, 23, 6, false); // Move white checker from point 23 to point 18
            service.MoveChecker(boardModel, 17, 5, false); // Move white checker from point 18 to point 13
            service.MoveChecker(boardModel, 12, 6, false); // Move white checker from point 13 to point 7

            // creating double black checker on point 6
            service.MoveChecker(boardModel, 23, 6, false); // Move white checker from point 23 to point 18
            service.MoveChecker(boardModel, 17, 6, false); // Move white checker from point 18 to point 11
            service.MoveChecker(boardModel, 11, 6, false); // Move white checker from point 12 to point 6
            service.MoveChecker(boardModel, 23, 6, false); // Move white checker from point 23 to point 18
            service.MoveChecker(boardModel, 17, 6, false); // Move white checker from point 18 to point 11
            service.MoveChecker(boardModel, 11, 6, false); // Move white checker from point 12 to point 6

            // try to move from point 1 to 7 
            Assert.True(service.CanMoveChecker(boardModel, 0, 6, true));
            // try to move from point 1 to 6
            Assert.False(service.CanMoveChecker(boardModel, 0, 5, true));
        }

        [Fact]
        public void PortesBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            Assert.Equal(GameModus.Portes, service.Modus);
            var boardModel = service.CreateBoard();
            Assert.Equal(GameModus.Portes, boardModel.Modus);
        }

        [Fact]
        public void PortesBoardIsCreatedProperly()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.IsAssignableFrom<IBoardModel>(boardModel);
            Assert.Equal(24, boardModel.Fields.Length);
            Assert.Equal(new Range(18, 23), boardModel.HomeRangeWhite);
            Assert.Equal(new Range(5, 0), boardModel.HomeRangeBlack);
			Assert.Equal(new Range(0, 5), boardModel.StartRangeWhite);
			Assert.Equal(new Range(23, 18), boardModel.StartRangeBlack);
			Assert.Equal(0, boardModel.BearOffCountWhite);
            Assert.Equal(0, boardModel.BearOffCountBlack);
			Assert.Equal(15, boardModel.WinConditionCount);
			Assert.Equal(167, boardModel.PipCountWhite);
			Assert.Equal(167, boardModel.PipCountBlack);
			var homebarModel = boardModel as IHomeBarModel;
            Assert.NotNull(homebarModel);
            Assert.Equal(0, homebarModel.HomeBarCountWhite);
            Assert.Equal(0, homebarModel.HomeBarCountBlack);
            Assert.Equal(-1, homebarModel.StartIndexWhite);
            Assert.Equal(24, homebarModel.StartIndexBlack);
			Assert.True(homebarModel.CanSendToHomeBar);
			Assert.True(homebarModel.MustEnterFromHomebar);
			var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.Null(doublingCubeModel);
            var blockModel = boardModel as IPinModel;
            Assert.Null(blockModel);
        }

        [Fact]
        public void PortesBoardStartValuesAreCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.NotNull(boardModel);
            Assert.Equal(-2, boardModel.Fields[0]); // 2 white pieces on point 1
            Assert.Equal(5, boardModel.Fields[5]); // 5 black pieces on point 6
            Assert.Equal(3, boardModel.Fields[7]); // 3 black pieces on point 8
            Assert.Equal(-5, boardModel.Fields[11]); // 5 white pieces on point 12
            Assert.Equal(5, boardModel.Fields[12]); // 5 black pieces on point 13
            Assert.Equal(-3, boardModel.Fields[16]); // 3 white pieces on point 17
            Assert.Equal(-5, boardModel.Fields[18]); // 5 white pieces on point 19
            Assert.Equal(2, boardModel.Fields[23]); // 2 black pieces on point 24
        }

        [Fact]
        public void PortesBoardMovesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            var boardModel = service.CreateBoard();

            service.MoveChecker(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            Assert.Equal(-1, boardModel.Fields[0]); // Field 1 should now have 1 white checker
            Assert.Equal(-1, boardModel.Fields[6]); // Field 7 should now have 1 white checker

            service.MoveChecker(boardModel, 23, 6, false); // Move black checker from point 24 to point 18
            Assert.Equal(1, boardModel.Fields[23]); // Field 24 should now have 1 black checker
            Assert.Equal(1, boardModel.Fields[17]); // Field 18 should now have 1 black checker
        }

        [Fact]
        public void PortesBoardMovesToCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            var boardModel = service.CreateBoard();

            service.MoveCheckerTo(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            Assert.Equal(-1, boardModel.Fields[0]); // Field 1 should now have 1 white checker
            Assert.Equal(-1, boardModel.Fields[6]); // Field 7 should now have 1 white checker

            service.MoveCheckerTo(boardModel, 23, 17, false); // Move black checker from point 24 to point 18
            Assert.Equal(1, boardModel.Fields[23]); // Field 24 should now have 1 black checker
            Assert.Equal(1, boardModel.Fields[17]); // Field 18 should now have 1 black checker
        }

        [Fact]
        public void PortesBoardCanMovePiece()
        {
            var service = BoardServiceFactory.Create(GameModus.Portes);
            var boardModel = service.CreateBoard();
            // White can move from point 1 to point 7
            Assert.True(service.CanMoveChecker(boardModel, 0, 6, true));
            // Black can move from point 24 to point 18
            Assert.True(service.CanMoveChecker(boardModel, 23, 6, false));
            // White cannot move from point 1 to point 6 (blocked)
            Assert.False(service.CanMoveChecker(boardModel, 0, 5, true));
            // Black cannot move from point 24 to point 19 (blocked)
            Assert.False(service.CanMoveChecker(boardModel, 23, 5, false));

            // create a single white checker on 1 and 2
            service.MoveChecker(boardModel, 0, 1, true);
            Assert.True(service.CanMoveChecker(boardModel, 5, 4, false));
            Assert.True(service.CanMoveChecker(boardModel, 5, 5, false));

            // create a single black checker on 24 and 23
            service.MoveChecker(boardModel, 23, 1, false);
            Assert.True(service.CanMoveChecker(boardModel, 18, 4, true));
            Assert.True(service.CanMoveChecker(boardModel, 18, 5, true));
        }

        [Fact]
        public void FevgaBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            Assert.Equal(GameModus.Fevga, service.Modus);
            var boardModel = service.CreateBoard();
            Assert.Equal(GameModus.Fevga, boardModel.Modus);
        }

        [Fact]
        public void FevgaBoardIsCreatedProperly()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.IsAssignableFrom<IBoardModel>(boardModel);
            Assert.Equal(24, boardModel.Fields.Length);
            Assert.Equal(new Range(18, 23), boardModel.HomeRangeWhite);
            Assert.Equal(new Range(6, 11), boardModel.HomeRangeBlack);
			Assert.Equal(new Range(0, 5), boardModel.StartRangeWhite);
			Assert.Equal(new Range(12, 17), boardModel.StartRangeBlack);
			Assert.Equal(0, boardModel.BearOffCountWhite);
            Assert.Equal(0, boardModel.BearOffCountBlack);
			Assert.Equal(15, boardModel.WinConditionCount);
			Assert.Equal(360, boardModel.PipCountWhite);
			Assert.Equal(360, boardModel.PipCountBlack);
			var homebarModel = boardModel as IHomeBarModel;
            Assert.NotNull(homebarModel);
			Assert.False(homebarModel.CanSendToHomeBar);
			Assert.False(homebarModel.MustEnterFromHomebar);
			Assert.Equal(14, homebarModel.HomeBarCountWhite);
			Assert.Equal(14, homebarModel.HomeBarCountBlack);
			Assert.Equal(-1, homebarModel.StartIndexWhite);
			Assert.Equal(24, homebarModel.StartIndexBlack);
            homebarModel.AddToHomeBar(true, 1); homebarModel.AddToHomeBar(false, 1);
			Assert.Equal(15, homebarModel.HomeBarCountWhite);
			Assert.Equal(15, homebarModel.HomeBarCountBlack);
			var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.Null(doublingCubeModel);
        }

        [Fact]
        public void FevgaBoardStartValuesAreCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.NotNull(boardModel);
            Assert.Equal(-1, boardModel.Fields[0]); // 1 white pieces on point 1
            Assert.Equal(1, boardModel.Fields[12]); // 1 black pieces on point 6
        }

        [Fact]
        public void FevgaBoardMovesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            var boardModel = service.CreateBoard();
			var homebarModel = boardModel as IHomeBarModel;
			Assert.NotNull(homebarModel);

			service.MoveChecker(boardModel, homebarModel.StartIndexWhite, 6, true); // Move white checker from point 1 to point 6
            Assert.Equal(13, homebarModel.HomeBarCountWhite); // Homebar should now have 14 white pieces
            Assert.Equal(-1, boardModel.Fields[5]); // Field 6 should now have 1 white checker

            service.MoveChecker(boardModel, homebarModel.StartIndexBlack, 6, false); // Move black checker from point 13 to point 18
            Assert.Equal(13, homebarModel.HomeBarCountWhite); // Homebar should now have 14 black pieces
            Assert.Equal(1, boardModel.Fields[17]); // Field 18 should now have 1 black checker

            // move checker around from index 23 to index 1
            service.MoveChecker(boardModel, 17, 8, false); // Move black checker from point 18 to point 2
            Assert.Equal(0, boardModel.Fields[18]); // Field 18 should now have 0 black checker
            Assert.Equal(1, boardModel.Fields[1]); // Field 2 should now have 1 black checker
            
        }

        [Fact]
        public void FevgaBoardMovesToCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            var boardModel = service.CreateBoard();
			var homebarModel = boardModel as IHomeBarModel;
			Assert.NotNull(homebarModel);

			service.MoveCheckerTo(boardModel, homebarModel.StartIndexWhite, 5, true); // Move white checker from point 1 to point 6
            Assert.Equal(-1, boardModel.Fields[0]); // Field 1 should now have 1 white pieces
            Assert.Equal(-1, boardModel.Fields[5]); // Field 7 should now have 1 white checker

            service.MoveCheckerTo(boardModel, homebarModel.StartIndexBlack, 17, false); // Move black checker from point 13 to point 18
            Assert.Equal(1, boardModel.Fields[12]); // Field 13 should now have 14 black pieces
            Assert.Equal(1, boardModel.Fields[17]); // Field 19 should now have 1 black checker

            // move checker around from index 23 to index 1
            service.MoveCheckerTo(boardModel, 17, 1, false); // Move black checker from point 18 to point 2
            Assert.Equal(0, boardModel.Fields[18]); // Field 18 should now have 0 black checker
            Assert.Equal(1, boardModel.Fields[1]); // Field 2 should now have 1 black checker

        }

        [Fact]
        public void FevgaBoardCanMovePiece()
        {
            var service = BoardServiceFactory.Create(GameModus.Fevga);
            var boardModel = service.CreateBoard();
			var homebarModel = boardModel as IHomeBarModel;
			Assert.NotNull(homebarModel);

			// White can move from point to point 6
			Assert.True(service.CanMoveChecker(boardModel, homebarModel.StartIndexWhite, 6, true));
            // Black can move from point to point 19
            Assert.True(service.CanMoveChecker(boardModel, homebarModel.StartIndexBlack, 6, false));

            // creating single white checker on point 14
            service.MoveChecker(boardModel, homebarModel.StartIndexWhite, 6, true); // Move white checker from point 1 to point 7
            service.MoveChecker(boardModel, 5, 6, true); // Move white checker from point 6 to point 12
            service.MoveChecker(boardModel, 11, 2, true); // Move white checker from point 12 to point 14

            // creating double white checker on point 14
            service.MoveChecker(boardModel, homebarModel.StartIndexWhite, 6, true); // Move white checker from point 1 to point 6
            service.MoveChecker(boardModel, 5, 6, true); // Move white checker from point 6 to point 12
            service.MoveChecker(boardModel, 11, 3, true); // Move white checker from point 12 to point 15
            service.MoveChecker(boardModel, homebarModel.StartIndexWhite, 6, true); // Move white checker from point 1 to point 6
            service.MoveChecker(boardModel, 5, 6, true); // Move white checker from point 6 to point 12
            service.MoveChecker(boardModel, 11, 3, true); // Move white checker from point 12 to point 15

            // try to move from point 13 to 14 
            Assert.False(service.CanMoveChecker(boardModel, 12, 1, false));
            // try to move from point 13 to 15
            Assert.False(service.CanMoveChecker(boardModel, 12, 2, false));

            // creating single black checker on point 2
            service.MoveChecker(boardModel, homebarModel.StartIndexBlack, 6, false); // Move white checker from point 13 to point 18
            service.MoveChecker(boardModel, 17, 6, false); // Move white checker from point 18 to point 24
            service.MoveChecker(boardModel, 23, 2, false); // Move white checker from point 24 to point 2

            // creating double black checker on point 3
            service.MoveChecker(boardModel, homebarModel.StartIndexBlack, 6, false); // Move white checker from point 13 to point 18
            service.MoveChecker(boardModel, 17, 6, false); // Move white checker from point 18 to point 24
            service.MoveChecker(boardModel, 23, 3, false); // Move white checker from point 24 to point 3
            service.MoveChecker(boardModel, homebarModel.StartIndexBlack, 6, false); // Move white checker from point 13 to point 18
            service.MoveChecker(boardModel, 17, 6, false); // Move white checker from point 18 to point 24
            service.MoveChecker(boardModel, 23, 3, false); // Move white checker from point 24 to point 3

            // try to move from point 1 to 2 
            Assert.False(service.CanMoveChecker(boardModel, 0, 1, true));
            // try to move from point 1 to 3
            Assert.False(service.CanMoveChecker(boardModel, 0, 2, true));
        }

        #endregion

        #region Tavla

        [Fact]
        public void TavlaBoardServiceHasCorrectModus()
        {
            var service = BoardServiceFactory.Create(GameModus.Tavla);
            Assert.Equal(GameModus.Tavla, service.Modus);
            var boardModel = service.CreateBoard();
            Assert.Equal(GameModus.Tavla, boardModel.Modus);
        }

        [Fact]
        public void TavlaBoardIsCreatedProperly()
        {
            var service = BoardServiceFactory.Create(GameModus.Tavla);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.IsAssignableFrom<IBoardModel>(boardModel);
            Assert.Equal(24, boardModel.Fields.Length);
            Assert.Equal(new Range(18, 23), boardModel.HomeRangeWhite);
            Assert.Equal(new Range(5, 0), boardModel.HomeRangeBlack);
			Assert.Equal(new Range(0, 5), boardModel.StartRangeWhite);
			Assert.Equal(new Range(23, 18), boardModel.StartRangeBlack);
			Assert.Equal(0, boardModel.BearOffCountWhite);
            Assert.Equal(0, boardModel.BearOffCountBlack);
			Assert.Equal(15, boardModel.WinConditionCount);
			Assert.Equal(167, boardModel.PipCountWhite);
			Assert.Equal(167, boardModel.PipCountBlack);
			var homebarModel = boardModel as IHomeBarModel;
            Assert.NotNull(homebarModel);
            Assert.Equal(0, homebarModel.HomeBarCountWhite);
            Assert.Equal(0, homebarModel.HomeBarCountBlack);
            Assert.Equal(-1, homebarModel.StartIndexWhite);
            Assert.Equal(24, homebarModel.StartIndexBlack);
			Assert.True(homebarModel.CanSendToHomeBar);
			Assert.True(homebarModel.MustEnterFromHomebar);
			var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.Null(doublingCubeModel);
            var blockModel = boardModel as IPinModel;
            Assert.Null(blockModel);
        }

        [Fact]
        public void TavlaBoardStartValuesAreCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Tavla);
            Assert.NotNull(service);
            var boardModel = service.CreateBoard();
            Assert.NotNull(boardModel);
            Assert.Equal(-2, boardModel.Fields[0]); // 2 white pieces on point 1
            Assert.Equal(5, boardModel.Fields[5]); // 5 black pieces on point 6
            Assert.Equal(3, boardModel.Fields[7]); // 3 black pieces on point 8
            Assert.Equal(-5, boardModel.Fields[11]); // 5 white pieces on point 12
            Assert.Equal(5, boardModel.Fields[12]); // 5 black pieces on point 13
            Assert.Equal(-3, boardModel.Fields[16]); // 3 white pieces on point 17
            Assert.Equal(-5, boardModel.Fields[18]); // 5 white pieces on point 19
            Assert.Equal(2, boardModel.Fields[23]); // 2 black pieces on point 24
        }

        [Fact]
        public void TavlaBoardMovesCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Tavla);
            var boardModel = service.CreateBoard();

            service.MoveChecker(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            Assert.Equal(-1, boardModel.Fields[0]); // Field 1 should now have 1 white checker
            Assert.Equal(-1, boardModel.Fields[6]); // Field 7 should now have 1 white checker

            service.MoveChecker(boardModel, 23, 6, false); // Move black checker from point 24 to point 18
            Assert.Equal(1, boardModel.Fields[23]); // Field 24 should now have 1 black checker
            Assert.Equal(1, boardModel.Fields[17]); // Field 18 should now have 1 black checker
        }

        [Fact]
        public void TavlaBoardMovesToCorrect()
        {
            var service = BoardServiceFactory.Create(GameModus.Tavla);
            var boardModel = service.CreateBoard();

            service.MoveCheckerTo(boardModel, 0, 6, true); // Move white checker from point 1 to point 7
            Assert.Equal(-1, boardModel.Fields[0]); // Field 1 should now have 1 white checker
            Assert.Equal(-1, boardModel.Fields[6]); // Field 7 should now have 1 white checker

            service.MoveCheckerTo(boardModel, 23, 17, false); // Move black checker from point 24 to point 18
            Assert.Equal(1, boardModel.Fields[23]); // Field 24 should now have 1 black checker
            Assert.Equal(1, boardModel.Fields[17]); // Field 18 should now have 1 black checker
        }

        [Fact]
        public void TavlaBoardCanMovePiece()
        {
            var service = BoardServiceFactory.Create(GameModus.Tavla);
            var boardModel = service.CreateBoard();
            // White can move from point 1 to point 7
            Assert.True(service.CanMoveChecker(boardModel, 0, 6, true));
            // Black can move from point 24 to point 18
            Assert.True(service.CanMoveChecker(boardModel, 23, 6, false));
            // White cannot move from point 1 to point 6 (blocked)
            Assert.False(service.CanMoveChecker(boardModel, 0, 5, true));
            // Black cannot move from point 24 to point 19 (blocked)
            Assert.False(service.CanMoveChecker(boardModel, 23, 5, false));

            // create a single white checker on 1 and 2
            service.MoveChecker(boardModel, 0, 1, true);
            Assert.True(service.CanMoveChecker(boardModel, 5, 4, false));
            Assert.True(service.CanMoveChecker(boardModel, 5, 5, false));

            // create a single black checker on 24 and 23
            service.MoveChecker(boardModel, 23, 1, false);
            Assert.True(service.CanMoveChecker(boardModel, 18, 4, true));
            Assert.True(service.CanMoveChecker(boardModel, 18, 5, true));
        }

		#endregion Tavla

		#region BearOff

		[Theory]
		[InlineData(GameModus.Backgammon)]
		[InlineData(GameModus.Portes)]
		[InlineData(GameModus.Tavla)]
		[InlineData(GameModus.Plakoto)]
		public void StandardBearOffHomeRange(GameModus gameModus)
		{
			var service = BoardServiceFactory.Create(gameModus);
			var board = service.CreateBoard();
            board.SetFields(BoardMocks.StandardCanBearOffBoard);

			// can bear off black
			Assert.Equal(5, board.Fields[0]);
			Assert.True(board.CanBearOff(0, 1, false));
            service.MoveCheckerTo(board, 0, WellKnownBoardPositions.BearOffBlack, false);
            Assert.Equal(1, board.BearOffCountBlack);
            Assert.Equal(4, board.Fields[0]);

			Assert.Equal(5, board.Fields[1]);
			Assert.True(board.CanBearOff(1, 2, false));
			service.MoveCheckerTo(board, 1, WellKnownBoardPositions.BearOffBlack, false);
			Assert.Equal(2, board.BearOffCountBlack);
			Assert.Equal(4, board.Fields[1]);

			Assert.Equal(3, board.Fields[2]);
			Assert.True(board.CanBearOff(2, 3, false));
			service.MoveCheckerTo(board, 2, WellKnownBoardPositions.BearOffBlack, false);
			Assert.Equal(3, board.BearOffCountBlack);
			Assert.Equal(2, board.Fields[2]);

			Assert.Equal(1, board.Fields[3]);
			Assert.True(board.CanBearOff(3, 4, false));
			service.MoveCheckerTo(board, 3, WellKnownBoardPositions.BearOffBlack, false);
			Assert.Equal(4, board.BearOffCountBlack);
			Assert.Equal(0, board.Fields[3]);

			Assert.Equal(1, board.Fields[4]);
			Assert.True(board.CanBearOff(4, 5, false));
			service.MoveCheckerTo(board, 4, WellKnownBoardPositions.BearOffBlack, false);
			Assert.Equal(5, board.BearOffCountBlack);
			Assert.Equal(0, board.Fields[4]);

			Assert.Equal(1, board.Fields[5]);
			Assert.True(board.CanBearOff(5, 6, false));
			service.MoveCheckerTo(board, 5, WellKnownBoardPositions.BearOffBlack, false);
			Assert.Equal(6, board.BearOffCountBlack);
			Assert.Equal(0, board.Fields[5]);

			// can bear off white
			Assert.Equal(-1, board.Fields[23]);
			Assert.True(board.CanBearOff(23, 1, true));
			service.MoveCheckerTo(board, 23, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(1, board.BearOffCountWhite);
			Assert.Equal(0, board.Fields[23]);

			Assert.Equal(-2, board.Fields[22]);
			Assert.True(board.CanBearOff(22, 2, true));
			service.MoveCheckerTo(board, 22, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(2, board.BearOffCountWhite);
			Assert.Equal(-1, board.Fields[22]);

			Assert.Equal(-3, board.Fields[21]);
			Assert.True(board.CanBearOff(21, 3, true));
			service.MoveCheckerTo(board, 21, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(3, board.BearOffCountWhite);
			Assert.Equal(-2, board.Fields[21]);

			Assert.Equal(-3, board.Fields[20]);
			Assert.True(board.CanBearOff(20, 4, true));
			service.MoveCheckerTo(board, 20, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(4, board.BearOffCountWhite);
			Assert.Equal(-2, board.Fields[20]);

			Assert.Equal(-3, board.Fields[19]);
			Assert.True(board.CanBearOff(19, 5, true));
			service.MoveCheckerTo(board, 19, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(5, board.BearOffCountWhite);
			Assert.Equal(-2, board.Fields[19]);

			Assert.Equal(-3, board.Fields[18]);
			Assert.True(board.CanBearOff(18, 6, true));
			service.MoveCheckerTo(board, 18, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(6, board.BearOffCountWhite);
			Assert.Equal(-2, board.Fields[18]);

		}

		[Fact]
		public void FevgaBearOffHomeRange()
		{
			var service = BoardServiceFactory.Create(GameModus.Fevga);
			var board = service.CreateBoard();
			board.SetFields(BoardMocks.FevgaCanBearOffBoard);

			// can bear off black
			Assert.Equal(5, board.Fields[6]);
			Assert.True(board.CanBearOff(6, 6, false));
			service.MoveCheckerTo(board, 6, WellKnownBoardPositions.BearOffBlack, false);
			Assert.Equal(1, board.BearOffCountBlack);
			Assert.Equal(4, board.Fields[6]);

			Assert.Equal(5, board.Fields[7]);
			Assert.True(board.CanBearOff(7, 5, false));
			service.MoveCheckerTo(board, 7, WellKnownBoardPositions.BearOffBlack, false);
			Assert.Equal(2, board.BearOffCountBlack);
			Assert.Equal(4, board.Fields[7]);

			Assert.Equal(2, board.Fields[8]);
			Assert.True(board.CanBearOff(8, 4, false));
			service.MoveCheckerTo(board, 8, WellKnownBoardPositions.BearOffBlack, false);
			Assert.Equal(3, board.BearOffCountBlack);
			Assert.Equal(1, board.Fields[8]);

			Assert.Equal(1, board.Fields[9]);
			Assert.True(board.CanBearOff(9, 3, false));
			service.MoveCheckerTo(board, 9, WellKnownBoardPositions.BearOffBlack, false);
			Assert.Equal(4, board.BearOffCountBlack);
			Assert.Equal(0, board.Fields[9]);

			Assert.Equal(1, board.Fields[10]);
			Assert.True(board.CanBearOff(10, 2, false));
			service.MoveCheckerTo(board, 10, WellKnownBoardPositions.BearOffBlack, false);
			Assert.Equal(5, board.BearOffCountBlack);
			Assert.Equal(0, board.Fields[10]);

			Assert.Equal(1, board.Fields[11]);
			Assert.True(board.CanBearOff(11, 1, false));
			service.MoveCheckerTo(board, 11, WellKnownBoardPositions.BearOffBlack, false);
			Assert.Equal(6, board.BearOffCountBlack);
			Assert.Equal(0, board.Fields[11]);

			// can bear off white
			Assert.Equal(-1, board.Fields[23]);
			Assert.True(board.CanBearOff(23, 1, true));
			service.MoveCheckerTo(board, 23, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(1, board.BearOffCountWhite);
			Assert.Equal(0, board.Fields[23]);

			Assert.Equal(-2, board.Fields[22]);
			Assert.True(board.CanBearOff(22, 2, true));
			service.MoveCheckerTo(board, 22, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(2, board.BearOffCountWhite);
			Assert.Equal(-1, board.Fields[22]);

			Assert.Equal(-3, board.Fields[21]);
			Assert.True(board.CanBearOff(21, 3, true));
			service.MoveCheckerTo(board, 21, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(3, board.BearOffCountWhite);
			Assert.Equal(-2, board.Fields[21]);

			Assert.Equal(-3, board.Fields[20]);
			Assert.True(board.CanBearOff(20, 4, true));
			service.MoveCheckerTo(board, 20, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(4, board.BearOffCountWhite);
			Assert.Equal(-2, board.Fields[20]);

			Assert.Equal(-3, board.Fields[19]);
			Assert.True(board.CanBearOff(19, 5, true));
			service.MoveCheckerTo(board, 19, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(5, board.BearOffCountWhite);
			Assert.Equal(-2, board.Fields[19]);

			Assert.Equal(-3, board.Fields[18]);
			Assert.True(board.CanBearOff(18, 6, true));
			service.MoveCheckerTo(board, 18, WellKnownBoardPositions.BearOffWhite, true);
			Assert.Equal(6, board.BearOffCountWhite);
			Assert.Equal(-2, board.Fields[18]);
		}

		#endregion BearOff

		#region Undo Move

		[Theory]
		[InlineData(GameModus.Backgammon, true)]
		[InlineData(GameModus.Portes, true)]
		[InlineData(GameModus.Tavla, true)]
		[InlineData(GameModus.Plakoto, true)]
		[InlineData(GameModus.Fevga, true)]
		[InlineData(GameModus.Backgammon, false)]
		[InlineData(GameModus.Portes, false)]
		[InlineData(GameModus.Tavla, false)]
		[InlineData(GameModus.Plakoto, false)]
		[InlineData(GameModus.Fevga, false)]
		public void CanUndoMove(GameModus modus, bool isWhite)
        {
			var service = BoardServiceFactory.Create(modus);
			var board = service.CreateBoard();
            var startBoard = board.DeepClone();

            var legalMoveSequences = service.GetLegalMoveSequences(board, isWhite, 2, 3);
            var legalMoveSeq = legalMoveSequences.First();
            Assert.NotNull(legalMoveSeq);
            // make some moves
            foreach (var move in legalMoveSeq.Moves)
            {
                service.MoveCheckerTo(board, move.From, move.To, isWhite);
            }
            // undo all moves
            var undoStack = legalMoveSeq.Moves.ToList();
            undoStack.Reverse();
            foreach (var undo in undoStack)
            {
                service.UndoMove(board, undo, isWhite);
            }
            // initial game board should be recreated
            Assert.Equal(startBoard.Fields, board.Fields);
		}

		#endregion Undo Move
	}
}
