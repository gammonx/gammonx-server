using GammonX.Engine.Models;

using GammonX.Server.Contracts;

namespace GammonX.Server.Services
{
	public static class BoardModelBroker
	{
		/// <summary>
		/// Constructs a board state contract from the given model.
		/// </summary>
		/// <param name="model">Model to construct from.</param>
		/// <param name="inverted">Boolean indicating if some of the board values has to be inverted.</param>
		/// <returns>A board state contract.</returns>
		public static BoardStateContract ToContract(this IBoardModel model, bool inverted)
		{
			var boardState = new BoardStateContract();

			if (inverted)
			{
				model = model.InvertBoard();
			}

			boardState.Fields = model.Fields;
			boardState.BearOffCountWhite = model.BearOffCountWhite;
			boardState.BearOffCountBlack = model.BearOffCountBlack;
			boardState.PipCountWhite = model.PipCountWhite;
			boardState.PipCountBlack = model.PipCountBlack;

			if (model is IPinModel pinModel)
			{
				boardState.PinnedFields = pinModel.PinnedFields;
			}
			else
			{
				boardState.PinnedFields = null;
			}

			if (model is IHomeBarModel homeBarModel)
			{
				boardState.HomeBarCountWhite = homeBarModel.HomeBarCountWhite;
				boardState.HomeBarCountBlack = homeBarModel.HomeBarCountBlack;
			}

			if (model is IDoublingCubeModel doublingCubeModel)
			{
				boardState.DoublingCubeValue = doublingCubeModel.DoublingCubeValue;
				boardState.DoublingCubeOwner = doublingCubeModel.DoublingCubeOwner;
			}

			return boardState;
		}
	}
}
