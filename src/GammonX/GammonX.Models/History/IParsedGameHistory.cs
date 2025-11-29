namespace GammonX.Models.History
{
	/// <summary>
	/// Marker interface for a parsed game history using a specific format.
	/// </summary>
	public interface IParsedGameHistory
	{
		/// <summary>
		/// Calculates the amount of double dices for the given player.
		/// </summary>
		/// <param name="playerId">Player id.</param>
		/// <returns>Returns amount of double dices</returns>
		int DoubleDiceCount(Guid playerId);
	}
}
