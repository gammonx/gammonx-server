namespace GammonX.Server.Models
{
	public class MatchPlayerModel
	{
		public MatchPlayerModel(Guid id, string connectionId)
		{
			Id = id;
			ConnectionId = connectionId;
		}

		/// <summary>
		/// Gets the web socket connection id of the player.
		/// </summary>
		public string ConnectionId { get; private set; }

		/// <summary>
		/// Gets the player id.
		/// </summary>
		public Guid Id { get; private set; }

		/// <summary>
		/// Gets the starting dice roll value of the player.
		/// </summary>
		public int? StartDiceRoll { get; private set; } = null;

        /// <summary>
        /// Gets a boolean indicaitng if the the given player already accepted the next game.
        /// </summary>
        public bool NextGameAccepted { get; private set; } = false;

		/// <summary>
		/// Gets the score of the player in the current match session.
		/// </summary>
		public int Points { get; internal set; } = 0;

		/// <summary>
		/// Gets a boolean indicating if the given player is a bot.
		/// </summary>
		public bool IsBot => ConnectionId.Equals(Guid.Empty.ToString());

		/// <summary>
		/// Gets a boolean indicating if the given player is claimed by a client.
		/// </summary>
		public bool Claimed => Id != Guid.Empty;

		/// <summary>
		/// Accepts the next game for this player.
		/// </summary>
		public void AcceptNextGame()
		{
			NextGameAccepted = true;
		}

		/// <summary>
		/// Resets the next game accepted state of this player.
		/// </summary>
		public void ActiveGameOver()
		{
			NextGameAccepted = false;
		}

        /// <summary>
        /// Sets the starting dice roll value of the player.
        /// </summary>
        /// <param name="roll">Dice roll value.</param>
        public void SetStartDiceRoll(int? roll)
		{
			StartDiceRoll = roll;
        }
    }
}
