using GammonX.Models.Enums;

namespace GammonX.Models.History.MAT
{
	// <inheritdoc />
	public class MATGameHistory : IParsedGameHistory
	{
		public Guid Id { get; set; } = Guid.Empty;

		public Guid Player1Id { get; set; } = Guid.Empty;

		public Guid Player2Id { get; set; } = Guid.Empty;

		// <inheritdoc />
		public GameModus Modus { get; set; } = GameModus.Unknown;

		// <inheritdoc />
		public Guid Winner { get; set; } = Guid.Empty;

		// <inheritdoc />
		public int Points { get; set; } = 0;

		// <inheritdoc />
		public DateTime StartedAt { get; set; }

		// <inheritdoc />
		public DateTime EndedAt { get; set; }

		public List<IMatEvent> Events { get; set; } = [];

		// <inheritdoc />
		public HistoryFormat Format => HistoryFormat.MAT;

		// <inheritdoc />
		public int DoubleDiceCount(Guid playerId)
		{
			int doubleDiceCount = Events
				.OfType<MatRollEvent>()
				.Count(e => 
					e.PlayerId == playerId &&
					e.Dice.Length == 4 &&
					e.Dice.Distinct().Count() == 1);
			return doubleDiceCount;
		}

		// <inheritdoc />
		public TimeSpan Duration()
		{
			return EndedAt - StartedAt;
		}

		// <inheritdoc />
		public int TurnCount(Guid playerId)
		{
			int turnCount = Events
				.OfType<MatRollEvent>()
				.Count(e => e.PlayerId == playerId);
			return turnCount;
		}
	}

    /// <summary>
    /// Marker interface for a MAT game event.
    /// </summary>
    public interface IMatEvent 
	{
        /// <summary>
        /// Gets or sets the id of the player who performed this event.
        /// </summary>
        Guid PlayerId { get; set; }
    }

    // <inheritdoc />
    public class MatRollEvent : IMatEvent
	{
        // <inheritdoc />
        public Guid PlayerId { get; set; } = Guid.Empty;

		public int[] Dice { get; set; } = [];
	}

    // <inheritdoc />
    public class MatMoveEvent : IMatEvent
	{
        // <inheritdoc />
        public Guid PlayerId { get; set; } = Guid.Empty;

		public int? From { get; set; } = null;

		public int? To { get; set; } = null;
	}
}
