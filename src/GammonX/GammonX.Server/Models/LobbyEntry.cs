namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	public class LobbyEntry
	{
		public LobbyEntry(Guid id) 
		{
			Id = id;
		}

		/// <summary>
		/// 
		/// </summary>
		public string? ConnectionId { get; private set; }
		
		/// <summary>
		/// 
		/// </summary>
		public Guid Id { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="connectionId"></param>
		public void SetConnectionId(string? connectionId) 
		{
			ConnectionId = connectionId;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public PlayerModel ToPlayer()
		{
			ArgumentNullException.ThrowIfNull(ConnectionId, nameof(ConnectionId));
			return new PlayerModel(Id, ConnectionId);
		}
	}
}
