using GammonX.Server.Models;

namespace GammonX.Server.EntityFramework.Entities
{
	/// <summary>
	/// Provies all information about player stats for the given match variant, type and modus.
	/// </summary>
	public class PlayerStats
	{
		/// <summary>
		/// Gets or sets the id of the given player stats.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the id of the player this stats entry belongs to.. Act as a foreign key.
		/// </summary>
		public Guid PlayerId { get; set; }

		/// <summary>
		/// Gets or sets the player this stats entry belongs to.
		/// </summary>
		public Player Player { get; set; } = null!;

		/// <summary>
		/// Gets or sets the match variant this stats entry belongs to.
		/// </summary>
		public WellKnownMatchVariant Variant { get; set; }

		/// <summary>
		/// Gets or sets the match type this stats entry belongs to.
		/// </summary>
		public WellKnownMatchType Type { get; set; }

		/// <summary>
		/// Gets or sets the match modus this stats entry belongs to.
		/// </summary>
		public WellKnownMatchModus Modus { get; set; }

		/// <summary>
		/// Gets or sets the total amount of matches played for the given variant, type and modus.
		/// </summary>
		public int MatchesPlayed { get; set; }

		/// <summary>
		/// Gets or sets the total wins for the given variant, type and modus.
		/// </summary>
		public int MatchesWon { get; set; }

		/// <summary>
		/// Gets or sets the total losses for the given variant, type and modus.
		/// </summary>
		public int MatchesLost { get; set; }

		/// <summary>
		/// Gets or sets the current win loss ratio for the given variant, type and modus.
		/// </summary>
		public double WinRate { get; set; }

		/// <summary>
		/// Gets or sets the active win streak for the given variant, type and modus.
		/// </summary>
		public int WinStreak { get; set; }

		/// <summary>
		/// Gets or sets the longest win streak for the given variant, type and modus.
		/// </summary>
		public int LongestWinStreak { get; set; }

		/// <summary>
		/// Gets the total play time in matches in milliseconds.
		/// </summary>
		public long TotalPlayTimeInMs { get; set; }

		/// <summary>
		/// Gets or sets the average match duration in milliseconds.
		/// </summary>
		public long AverageMatchLengthInMs { get; set; }

		/// <summary>
		/// Gets or sets the date time of the last match played.
		/// </summary>
		public DateTime LastMatch { get; set; }

		/// <summary>
		/// Gets or sets the amount of matches in the last 7 days.
		/// </summary>
		public int MatchesLast7Days { get; set; }

		/// <summary>
		/// Gets or sets the amount of matches in the last 30 days.
		/// </summary>
		public int MatchesLast30Days { get; set; }
	}
}
