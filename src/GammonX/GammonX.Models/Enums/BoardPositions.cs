namespace GammonX.Models.Enums
{
	/// <summary>
	/// Contains well-known board positions for different game modes.
	/// </summary>
	public static class BoardPositions
	{
		/// <summary>
		/// Gets the well known <c>to</c> value for bearing off white legal move tuples.
		/// </summary>
		public const int BearOffWhite = -100;

		/// <summary>
		/// Gets the well known <c>to</c> value for bearing off black legal move tuples.
		/// </summary>
		public const int BearOffBlack = 100;

		/// <summary>
		/// Gets the well known <c>from</c> value when playing white checkers from the home bar.
		/// </summary>
		public const int HomeBarWhite = -1;

		/// <summary>
		/// Gets the well known <c>from</c> value when playing black checkers from the home bar.
		/// </summary>
		public const int HomeBarBlack = 24;
	}
}
