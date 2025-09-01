namespace GammonX.Server.Models
{
	/// <summary>
	/// Each match variant has a n amount of sub games based on the rules of the game.
	/// </summary>
	/// <seealso cref="Engine.Models.GameModus"/>
	public enum WellKnownMatchVariant
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
	}

	/// <summary>
	/// Each well known queue type describes which match making queue the given player joins.
	/// </summary>
	public enum WellKnownMatchType
	{
		/// <summary>
		/// Joins the normal queue resulting in matches without any rating.
		/// </summary>
		Normal = 0,
		/// <summary>
		/// Joins the ranked queue resulting in matches which are rated.
		/// </summary>
		Ranked = 1,
		/// <summary>
		/// Join the bot queue resulting in matches against a bot/ai.
		/// </summary>
		Bot = 2,
	}
}
