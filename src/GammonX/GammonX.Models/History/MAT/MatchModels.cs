using GammonX.Models.Enums;

namespace GammonX.Models.History.MAT
{
	// <inheritdoc />
	public class MATMatchHistory : IParsedMatchHistory
	{
		// TODO: add unit tests

		// <inheritdoc />
		public HistoryFormat Format => HistoryFormat.MAT;

		public Guid MatchId { get; set; } = Guid.Empty;

		public string Name { get; set; } = string.Empty;

		public Guid Player1Id { get; set; } = Guid.Empty;

		public Guid Player2Id { get; set; } = Guid.Empty;

		public DateTime StartedAt { get; set; }

		public DateTime EndedAt { get; set; }

		// <inheritdoc />
		public int Length { get; set; } = 0;

		public List<IParsedGameHistory> Games { get; set; } = new();

		// <inheritdoc />
		public int PointCount(Guid playerId)
		{
			var wonGames = Games.Where(g => g.Winner == playerId);
			return wonGames.Sum(wg => wg.Points);
		}

		// <inheritdoc />
		public double AvgDoubleDiceCount(Guid playerId)
		{
			var doubleDiceAmount = Games.Sum(g => g.DoubleDiceCount(playerId));
			if (doubleDiceAmount > 0)
			{
				return doubleDiceAmount / Games.Count;
			}
			return 0.0;
		}

		// <inheritdoc />
		public double AvgDoubleOfferCount(Guid playerId)
		{
			// TODO: write as history event
			return 0.0;
		}

		// <inheritdoc />
		public TimeSpan AvgDuration(Guid playerId)
		{
			var timeSpans = Games.Select(g => g.Duration());
			var avgDuration = new TimeSpan(Convert.ToInt64(timeSpans.Average(ts => ts.Ticks)));
			return avgDuration;
		}

		// <inheritdoc />
		public int AvgTurnCount(Guid playerId)
		{
			var turnCount = Games.Sum(g => g.TurnCount(playerId));
			return turnCount / Games.Count;
		}
	}
}
