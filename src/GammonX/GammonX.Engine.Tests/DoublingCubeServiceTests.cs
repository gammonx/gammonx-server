using GammonX.Engine.Models;
using GammonX.Engine.Services;

namespace GammonX.Engine.Tests
{
    public class DoublingCubeServiceTests
    {
        #region Simple Interface Tests

        [Theory]
        [InlineData(GameModus.Backgammon)]
        public void HasDoublingCubeCapability(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var boardModel = service.CreateBoard();
            var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.NotNull(doublingCubeModel);
        }

        [Theory]
        [InlineData(GameModus.Portes)]
        [InlineData(GameModus.Tavla)]
        [InlineData(GameModus.Fevga)]
        [InlineData(GameModus.Plakoto)]
        public void HasNoDoublingCubeCapability(GameModus gameModus)
        {
            var service = BoardServiceFactory.Create(gameModus);
            var boardModel = service.CreateBoard();
            var doublingCubeModel = boardModel as IDoublingCubeModel;
            Assert.Null(doublingCubeModel);
        }

		[Theory]
		[InlineData(GameModus.Backgammon)]
		public void CanOfferDoublingCube(GameModus gameModus)
		{
			var service = BoardServiceFactory.Create(gameModus);
			var boardModel = service.CreateBoard();
			var doublingCubeModel = boardModel as IDoublingCubeModel;
			Assert.NotNull(doublingCubeModel);

            // at the start both players can offer
            Assert.Equal(1, doublingCubeModel.DoublingCubeValue);
            Assert.False(doublingCubeModel.DoublingCubeOwner);
            Assert.True(doublingCubeModel.CanOfferDoublingCube(true));
			// opponent can offer
			var inverted = boardModel.InvertBoard() as IDoublingCubeModel;
            Assert.NotNull(inverted);
			Assert.Equal(1, inverted.DoublingCubeValue);
			Assert.True(inverted.DoublingCubeOwner);
			Assert.True(inverted.CanOfferDoublingCube(true));
			inverted.AcceptDoublingCubeOffer(true);
			Assert.False(inverted.CanOfferDoublingCube(true));
			Assert.True(inverted.CanOfferDoublingCube(false));
			// current player can accept
			doublingCubeModel = ((IBoardModel)inverted).InvertBoard() as IDoublingCubeModel;
			Assert.NotNull(doublingCubeModel);
			Assert.Throws<InvalidOperationException>(() => doublingCubeModel.AcceptDoublingCubeOffer(true));
			Assert.Equal(2, doublingCubeModel.DoublingCubeValue);
			Assert.True(doublingCubeModel.DoublingCubeOwner);
			Assert.True(doublingCubeModel.CanOfferDoublingCube(true));
			Assert.Throws<InvalidOperationException>(() => doublingCubeModel.AcceptDoublingCubeOffer(true));

            // double up to 64
            inverted = ((IBoardModel)doublingCubeModel).InvertBoard() as IDoublingCubeModel;
			Assert.NotNull(inverted);
            inverted.AcceptDoublingCubeOffer(true);
			Assert.Equal(4, inverted.DoublingCubeValue);
			Assert.True(inverted.DoublingCubeOwner);
			Assert.True(inverted.CanOfferDoublingCube(true));

			doublingCubeModel = ((IBoardModel)inverted).InvertBoard() as IDoublingCubeModel;
            Assert.NotNull(doublingCubeModel);
			doublingCubeModel.AcceptDoublingCubeOffer(true);
			Assert.Equal(8, doublingCubeModel.DoublingCubeValue);
			Assert.True(doublingCubeModel.DoublingCubeOwner);
			Assert.True(doublingCubeModel.CanOfferDoublingCube(true));

			inverted = ((IBoardModel)doublingCubeModel).InvertBoard() as IDoublingCubeModel;
			Assert.NotNull(inverted);
			inverted.AcceptDoublingCubeOffer(true);
			Assert.Equal(16, inverted.DoublingCubeValue);
			Assert.True(inverted.DoublingCubeOwner);
			Assert.True(inverted.CanOfferDoublingCube(true));

			doublingCubeModel = ((IBoardModel)inverted).InvertBoard() as IDoublingCubeModel;
			Assert.NotNull(doublingCubeModel);
			doublingCubeModel.AcceptDoublingCubeOffer(true);
			Assert.Equal(32, doublingCubeModel.DoublingCubeValue);
			Assert.True(doublingCubeModel.DoublingCubeOwner);
			Assert.True(doublingCubeModel.CanOfferDoublingCube(true));

			inverted = ((IBoardModel)doublingCubeModel).InvertBoard() as IDoublingCubeModel;
			Assert.NotNull(inverted);
			inverted.AcceptDoublingCubeOffer(true);
			Assert.Equal(64, inverted.DoublingCubeValue);
			Assert.True(inverted.DoublingCubeOwner);
			Assert.False(inverted.CanOfferDoublingCube(true));

			// max is reached
			doublingCubeModel = ((IBoardModel)inverted).InvertBoard() as IDoublingCubeModel;
			Assert.NotNull(doublingCubeModel);
			Assert.Throws<InvalidOperationException>(() => doublingCubeModel.AcceptDoublingCubeOffer(true));
		}

		#endregion Simple Interface Tests
	}
}
