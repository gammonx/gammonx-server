namespace GammonX.Server.Bot
{
	public class GetMoveParameter
	{
		public required int DiceRoll1 { get; set; }

		public required int DiceRoll2 { get; set; }

		/// <summary>
		/// Number of points the player on turn (mostly the bot with black checkers) needs to win.
		/// </summary>
		public required int XPointsAway { get; set; }

		/// <summary>
		/// Number of points the opponent on turn needs to win.
		/// </summary>
		public required int OPointsAway { get; set; }

		/// <summary>
		/// Gets or sets a list of <index, checker count> representing the board state.
		/// Black checkers move from index 24 to 1, white checkers move from index 1 to 24.
		/// </summary>
		public required IReadOnlyDictionary<int, int> Points { get; init; }
	}
}
