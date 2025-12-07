using GammonX.Models.Enums;

namespace GammonX.Models.History.MAT
{
	// <inheritdoc />
	public class MATMatchHistory : IParsedMatchHistory
	{
		// <inheritdoc />
		public HistoryFormat Format => HistoryFormat.MAT;

        // <inheritdoc />
        public Guid Id { get; set; } = Guid.Empty;

        // <inheritdoc />
        public string Name { get; set; } = string.Empty;

        // <inheritdoc />
        public Guid Player1Id { get; set; } = Guid.Empty;

        // <inheritdoc />
        public Guid Player2Id { get; set; } = Guid.Empty;

        // <inheritdoc />
        public DateTime StartedAt { get; set; }

        // <inheritdoc />
        public DateTime EndedAt { get; set; }

		// <inheritdoc />
		public int Length { get; set; } = 0;

        // <inheritdoc />
        public List<IParsedGameHistory> Games { get; set; } = new();

		// <inheritdoc />
		public int PointCount(Guid playerId)
		{
			var wonGames = Games.Where(g => g.Winner == playerId);
			if (wonGames.Any())
			{
                return wonGames.Sum(wg => wg.Points);
            }
			return 0;
		}

		// <inheritdoc />
		public double AvgDoubleDiceCount(Guid playerId)
		{
			var doubleDiceAmount = Games.Sum(g => g.DoubleDiceCount(playerId));
			if (doubleDiceAmount > 0)
			{
				return (double)doubleDiceAmount / Games.Count;
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
		public TimeSpan AvgDuration()
		{
			if (Games.Count != 0)
			{
                var timeSpans = Games.Select(g => g.Duration());
                var avgDuration = new TimeSpan(Convert.ToInt64(timeSpans.Average(ts => ts.Ticks)));
                return avgDuration;
            }
			return TimeSpan.Zero;
		}

		// <inheritdoc />
		public int AvgTurnCount(Guid playerId)
		{
			if (Games.Count != 0)
			{
                var turnCount = Games.Sum(g => g.TurnCount(playerId));
                return turnCount / Games.Count;
            }
			return 0;
		}
	}
}
