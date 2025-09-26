namespace GammonX.Server.Bot
{
	public class GetEvalParameter
	{
		/// <summary>
		/// Gets or sets a list of <index, checker count> representing the board state.
		/// Black checkers move from index 24 to 1, white checkers move from index 1 to 24.
		/// </summary>
		public required IReadOnlyDictionary<int, int> Points { get; init; }
	}
}
