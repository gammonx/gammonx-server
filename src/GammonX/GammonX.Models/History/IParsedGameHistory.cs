using GammonX.Models.Enums;

namespace GammonX.Models.History
{
	/// <summary>
	/// Marker interface for a parsed game history using a specific format.
	/// </summary>
	public interface IParsedGameHistory
	{
		/// <summary>
		/// Gets the format of this game history.
		/// </summary>
		HistoryFormat Format { get; }

		/// <summary>
		/// Gets the player id of the game winner.
		/// </summary>
		Guid Winner { get; }

		/// <summary>
		/// Gets the amount of points awared to the winner.
		/// </summary>
		int Points { get; }

		/// <summary>
		/// Gets the date time when the game started.
		/// </summary>
		DateTime StartedAt { get; }

		/// <summary>
		/// Gets the date time when the game has ended.
		/// </summary>
		DateTime EndedAt { get; }

		/// <summary>
		/// Gets the modus of the played game.
		/// </summary>
		GameModus Modus { get; }

		/// <summary>
		/// Calculates the amount of double dices for the given player.
		/// </summary>
		/// <param name="playerId">Player id.</param>
		/// <returns>Returns amount of double dices</returns>
		int DoubleDiceCount(Guid playerId);

		/// <summary>
		/// Calculates the amount of turns for the given player.
		/// </summary>
		/// <param name="playerId">Player id.</param>
		/// <returns>Returns the turn count.</returns>
		int TurnCount(Guid playerId);

		/// <summary>
		/// Calculates the game duration.
		/// </summary>
		/// <returns>Returns the game duration.</returns>
		TimeSpan Duration();
	}
}
