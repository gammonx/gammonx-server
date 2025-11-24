namespace GammonX.Models.Enums
{
	/// <summary>
	/// Each match variant has a n amount of sub games based on the rules of the game.
	/// </summary>
	public enum MatchVariant
	{
		/// <summary>
		/// Includes a single game of backgammon
		/// </summary>
		Backgammon = 0,
		/// <summary>
		/// Includes a single game of tavla
		/// </summary>
		Tavla = 1,
		/// <summary>
		/// Includes 3 sub games (portes, plakoto, fevga).
		/// </summary>
		Tavli = 2,
		/// <summary>
		/// Default value if the match variant is unknown.
		/// </summary>
		Unknown = 99
	}
}
