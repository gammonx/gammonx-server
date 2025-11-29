using GammonX.DynamoDb.Items;

using GammonX.Models.Contracts;
using GammonX.Models.History;

namespace GammonX.Lambda.Extensions
{
	public static class ContractExtensions
	{
		public static GameItem ToGame(this GameRecordContract contract, IParsedGameHistory history)
		{
			var diceDoubles = history.DoubleDiceCount(contract.PlayerId);
			var gameItem = new GameItem()
			{
				Id = contract.Id,
				PlayerId = contract.PlayerId,
				Length = contract.Length,
				Modus = contract.Modus,
				Points = contract.Points,
				EndedAt = contract.EndedAt,
				StartedAt = contract.StartedAt,
				Result = contract.Result,
				DiceDoubles = diceDoubles,
				DoublingCubeValue = contract.DoublingCubeValue,
				Duration = contract.EndedAt - contract.StartedAt,
				PipesLeft = contract.PipesLeft,
			};
			return gameItem;
		}

		public static GameHistoryItem ToGameHistory(this GameRecordContract contract)
		{
			var gameHistoryItem = new GameHistoryItem()
			{
				Id = contract.Id,
				Data = contract.GameHistory,
				Format = contract.Format
			};
			return gameHistoryItem;
		}
	}
}
