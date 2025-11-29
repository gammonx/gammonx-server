namespace GammonX.Models.History.MAT
{
	// <inheritdoc />
	public class MatGameHistory : IParsedGameHistory
	{
		// TODO: add unit tests

		public string PlayerWhiteId { get; set; } = "";

		public string PlayerBlackId { get; set; } = "";
		
		public List<MatHeaderEntry> Headers { get; set; } = [];

		public List<IMatEvent> Events { get; set; } = [];

		// <inheritdoc />
		public int DoubleDiceCount(Guid playerId)
		{
			int doubleDiceCount = Events
				.OfType<MatRollEvent>()
				.Count(e => 
					Guid.Parse(e.Player) == playerId &&
					e.Dice.Length == 4 &&
					e.Dice.Distinct().Count() == 1);
			return doubleDiceCount;
		}
	}

	public class MatHeaderEntry
	{
		public string Key { get; set; } = "";

		public string Value { get; set; } = "";
	}

	public interface IMatEvent { }

	public class MatRollEvent : IMatEvent
	{
		public string Player { get; set; } = "";

		public int[] Dice { get; set; } = [];
	}

	public class MatMoveEvent : IMatEvent
	{
		public string Player { get; set; } = "";

		public string From { get; set; } = "";

		public string To { get; set; } = "";
	}
}
