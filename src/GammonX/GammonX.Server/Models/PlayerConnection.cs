namespace GammonX.Server.Models
{
    /// <summary>
    /// Represents a players connection in a signalR match lobby hub.
    /// </summary>
    public sealed class PlayerConnection
	{
		public PlayerConnection(Guid playerId) 
		{
			Id = playerId;
			LastSeenUtc = DateTime.UtcNow;
        }

		/// <summary>
		/// Gets the web socket connection id of the player in the lobby.
		/// </summary>
		public string? ConnectionId { get; private set; }
		
		/// <summary>
		/// Gets the player id of the connection.
		/// </summary>
		public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets the date time when this player connection was last seen (utc).
        /// </summary>
        public DateTime LastSeenUtc { get; set; }

		/// <summary>
		/// Sets the web socket connection id for this player connection.
		/// </summary>
		/// <param name="connectionId">The web socket connection id.</param>
		public void SetConnectionId(string? connectionId) 
		{
			ConnectionId = connectionId;
		}

		public MatchPlayerModel ToMatchPlayer()
		{
			ArgumentNullException.ThrowIfNull(ConnectionId, nameof(ConnectionId));
			return new MatchPlayerModel(this);
		}
	}
}
