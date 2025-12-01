using GammonX.Models.Enums;

namespace GammonX.Models.History
{
	/// <summary>
	/// Marker interface for a parsed match history using a specific format.
	/// </summary>
	public interface IParsedMatchHistory
	{
		/// <summary>
		/// Gets the format of this game history.
		/// </summary>
		HistoryFormat Format { get; }

		/// <summary>
		/// Gets or sets the match id.
		/// </summary>
		Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the match.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets or sets the id of the player with the white checkers.
        /// </summary>
        Guid Player1Id { get; set; }

        /// <summary>
        /// Gets or sets the id of the player with the black checkers.
        /// </summary>
        Guid Player2Id { get; set; }

        /// <summary>
        /// Gets or sets the amount of games played in that match.
        /// </summary>
        int Length { get; set; }

        /// <summary>
        /// Gets or sets the list of games played in that match.
        /// </summary>
        List<IParsedGameHistory> Games { get; set; }

        /// <summary>
        /// Gets the date time when the game started.
        /// </summary>
        DateTime StartedAt { get; }

		/// <summary>
		/// Gets the date time when the game has ended.
		/// </summary>
		DateTime EndedAt { get; }

		/// <summary>
		/// Gets the amount of points awarded to the given player.
		/// </summary>
		/// <param name="playerId">Player id.</param>
		/// <returns>Returns game points.</returns>
		int PointCount(Guid playerId);

		/// <summary>
		/// Calculates the average double dices for the given player.
		/// </summary>
		/// <param name="playerId">Player id.</param>
		/// <returns>Returns amount of double dices</returns>
		double AvgDoubleDiceCount(Guid playerId);

		/// <summary>
		/// Calculates the average game duration for the given player.
		/// </summary>
		/// <returns>Returns the average game duration.</returns>
		TimeSpan AvgDuration();

		/// <summary>
		/// Calculates the average turns that were played for the games.
		/// </summary>
		/// <param name="playerId">Player id.</param>
		/// <returns>Average turns played per game.</returns>
		int AvgTurnCount(Guid playerId);

		/// <summary>
		/// Calculates the average amount of offered doubling cubes by the related player.
		/// </summary>
		/// <param name="playerId">Player id.</param>
		/// <returns>Returns amount of doubling cubes offered.</returns>
		double AvgDoubleOfferCount(Guid playerId);
	}
}
