namespace GammonX.Models.Enums
{
	/// <summary>
	/// Each well known match modus describes which match making queue the given player joins.
	/// </summary>
	public enum MatchModus
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
		/// <summary>
		/// Default value if the match modus is unknown.
		/// </summary>
		Unknown = 99
	}
}
