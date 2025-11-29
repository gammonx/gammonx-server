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
			var turnCount = history.TurnCount(contract.PlayerId);
			var points = history.Points;
			var startedAt = history.StartedAt;
			var endedAt = history.EndedAt;
			var duration = history.Duration();
			var modus = history.Modus;
			var gameItem = new GameItem()
			{
				Id = contract.Id,
				PlayerId = contract.PlayerId,
				Length = turnCount,
				Modus = modus,
				Points = points,
				EndedAt = endedAt,
				StartedAt = startedAt,
				Result = contract.Result,
				DiceDoubles = diceDoubles,
				DoublingCubeValue = contract.DoublingCubeValue,
				Duration = duration,
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

		public static MatchItem ToMatch(this MatchRecordContract contract, IParsedMatchHistory history)
		{
			var avgDiceDoubles = history.AvgDoubleDiceCount(contract.PlayerId);
			var avgDuration = history.AvgDuration(contract.PlayerId);
			var avgPipesLeft = contract.AvgPipesLeft();
			var avgTurns = history.AvgTurnCount(contract.PlayerId);
			var gammons = contract.GammonCount();
			var backGammons = contract.BackgammonCount();
			var avgDoubles = history.AvgDoubleOfferCount(contract.PlayerId);
			var points = history.PointCount(contract.PlayerId);
			var matchItem = new MatchItem()
			{
				Id = contract.Id,
				PlayerId = contract.PlayerId,
				Length = history.Length,
				Modus = contract.Modus,
				Points = points,
				EndedAt = history.EndedAt,
				StartedAt = history.StartedAt,
				Result = contract.Result,
				Variant = contract.Variant,
				Type = contract.Type,
				Duration = history.EndedAt - history.StartedAt,
				AvgDoubleDices = avgDiceDoubles,
				AvgDuration = avgDuration,
				AvgPipesLeft = avgPipesLeft,
				AvgTurns = avgTurns,
				Gammons = gammons,
				BackGammons = backGammons,
				AvgDoubles = avgDoubles,
			};
			return matchItem;
		}

		public static MatchHistoryItem ToMatchHistory(this MatchRecordContract contract)
		{
			var gameHistoryItem = new MatchHistoryItem()
			{
				Id = contract.Id,
				Data = contract.MatchHistory,
				Format = contract.Format
			};
			return gameHistoryItem;
		}

		private static double AvgPipesLeft(this MatchRecordContract contract)
		{
			var lostGamesCount = contract.Games.Count(g => g.PipesLeft > 0);
			if (lostGamesCount > 0)
			{
				return contract.Games.Sum(g => g.PipesLeft) / lostGamesCount;
			}
			return 0.0;
		}

		private static int GammonCount(this MatchRecordContract contract)
		{
			return contract.Games.Count(g => g.Result == Models.Enums.GameResult.Gammon);
		}

		private static int BackgammonCount(this MatchRecordContract contract)
		{
			return contract.Games.Count(g => g.Result == Models.Enums.GameResult.Backgammon);
		}
	}
}
