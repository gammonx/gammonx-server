namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	public class Player
	{
		public Player(Guid id) 
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
	}
}
