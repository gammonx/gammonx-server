using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Models.Enums;

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
            Assert.Null(doublingCubeModel.DoublingCubeOwner);
            Assert.True(doublingCubeModel.CanOfferDoublingCube(true));
            Assert.True(doublingCubeModel.CanOfferDoublingCube(false));
            // black offered and white accepted
            doublingCubeModel.AcceptDoublingCubeOffer(true);
			Assert.True(doublingCubeModel.CanOfferDoublingCube(true));
			Assert.False(doublingCubeModel.CanOfferDoublingCube(false));
            // white cannot accept another one, but black can
            Assert.Throws<InvalidOperationException>(() => doublingCubeModel.AcceptDoublingCubeOffer(true));
			Assert.Equal(2, doublingCubeModel.DoublingCubeValue);
			Assert.True(doublingCubeModel.DoublingCubeOwner);

            // double up to 64
            doublingCubeModel.AcceptDoublingCubeOffer(false);
			Assert.Equal(4, doublingCubeModel.DoublingCubeValue);
			Assert.False(doublingCubeModel.DoublingCubeOwner);
			Assert.True(doublingCubeModel.CanOfferDoublingCube(false));
            Assert.False(doublingCubeModel.CanOfferDoublingCube(true));

            doublingCubeModel.AcceptDoublingCubeOffer(true);
			Assert.Equal(8, doublingCubeModel.DoublingCubeValue);
			Assert.True(doublingCubeModel.DoublingCubeOwner);
			Assert.True(doublingCubeModel.CanOfferDoublingCube(true));
            Assert.False(doublingCubeModel.CanOfferDoublingCube(false));

            doublingCubeModel.AcceptDoublingCubeOffer(false);
			Assert.Equal(16, doublingCubeModel.DoublingCubeValue);
			Assert.False(doublingCubeModel.DoublingCubeOwner);
			Assert.True(doublingCubeModel.CanOfferDoublingCube(false));
            Assert.False(doublingCubeModel.CanOfferDoublingCube(true));

            doublingCubeModel.AcceptDoublingCubeOffer(true);
			Assert.Equal(32, doublingCubeModel.DoublingCubeValue);
			Assert.True(doublingCubeModel.DoublingCubeOwner);
			Assert.True(doublingCubeModel.CanOfferDoublingCube(true));
            Assert.False(doublingCubeModel.CanOfferDoublingCube(false));

            doublingCubeModel.AcceptDoublingCubeOffer(false);
			Assert.Equal(64, doublingCubeModel.DoublingCubeValue);
			Assert.False(doublingCubeModel.DoublingCubeOwner);

            // max is reached
            Assert.False(doublingCubeModel.CanOfferDoublingCube(true));
            Assert.False(doublingCubeModel.CanOfferDoublingCube(false));
            Assert.Throws<InvalidOperationException>(() => doublingCubeModel.AcceptDoublingCubeOffer(true));
            Assert.Throws<InvalidOperationException>(() => doublingCubeModel.AcceptDoublingCubeOffer(false));
        }

		#endregion Simple Interface Tests
	}
}
