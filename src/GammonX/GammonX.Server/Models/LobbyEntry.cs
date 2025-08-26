namespace GammonX.Server.Models
{
	public class LobbyEntry
	{
		public LobbyEntry(Guid playerId) 
		{
			PlayerId = playerId;
		}

		/// <summary>
		/// Gets the web socket connection id of the player in the lobby.
		/// </summary>
		public string? ConnectionId { get; private set; }
		
		/// <summary>
		/// Gets the id of the player.
		/// </summary>
		public Guid PlayerId { get; private set; }

		/// <summary>
		/// Sets the web socket connection id for this lobby entry.
		/// </summary>
		/// <param name="connectionId">The web socket connection id.</param>
		public void SetConnectionId(string? connectionId) 
		{
			ConnectionId = connectionId;
		}

		public PlayerModel ToPlayer()
		{
			ArgumentNullException.ThrowIfNull(ConnectionId, nameof(ConnectionId));
			return new PlayerModel(PlayerId, ConnectionId);
		}
	}
}
