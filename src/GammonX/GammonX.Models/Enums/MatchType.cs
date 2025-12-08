namespace GammonX.Models.Enums
{
	/// <summary>
	/// Each well known match type describes whats the winning condition.
	/// </summary>
	public enum MatchType
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
		CashGame = 2,
		/// <summary>
		/// Default value if the match type is unknown.
		/// </summary>
		Unknown = 99
	}
}
