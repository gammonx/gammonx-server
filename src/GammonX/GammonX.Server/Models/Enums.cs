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
	/// Each well known match modus describes which match making queue the given player joins.
	/// </summary>
	public enum WellKnownMatchModus
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

	/// <summary>
	/// Each well known match type describes whats the winning condition.
	/// </summary>
	public enum WellKnownMatchType
	{
		/// <summary>
		/// The given match variant is played until one player reaches 5 points.
		/// </summary>
		FivePointGame = 0,
		/// <summary>
		/// The given match variant is played until one player reaches 7 points.
		/// </summary>
		SevenPointGame = 1,
		/// <summary>
		/// The given match variants game rounds are played each a single time.
		/// </summary>
		CashGame = 2
	}

	/// <summary>
	/// Representst the status of a queue entry.
	/// </summary>
	public enum QueueEntryStatus
	{
		/// <summary>
		/// The queue entry is still in the queue and waits for a match lobby match/creation.
		/// </summary>
		WaitingForOpponent = 0,
		/// <summary>
		/// The queue entry left the queue. A match lobby was created and is waiting to be used.
		/// </summary>
		OpponentFound = 1
	}
}
