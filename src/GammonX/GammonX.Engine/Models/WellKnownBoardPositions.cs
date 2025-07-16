namespace GammonX.Engine.Models
{
	/// <summary>
	/// Contains well-known board positions for different game modes.
	/// </summary>
	internal static class WellKnownBoardPositions
	{
		/// <summary>
		/// Gets the well known <c>to</c> value for bearing off white legal move tuples.
		/// </summary>
		public static int BearOffWhite = -100;

		/// <summary>
		/// Gets the well known <c>to</c> value for bearing off black legal move tuples.
		/// </summary>
		public static int BearOffBlack = 100;

		/// <summary>
		/// Gets the well known <c>from</c> value when playing white checkers from the home bar.
		/// </summary>
		public static int HomeBarWhite = -1;

		/// <summary>
		/// Gets the well known <c>from</c> value when playing black checkers from the home bar.
		/// </summary>
		public static int HomeBarBlack = 24;
	}
}
