using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	public class PlayerModel
	{
		public PlayerModel(Guid id, string connectionId)
		{
			Id = id;
			ConnectionId = connectionId;
		}

		/// <summary>
		/// 
		/// </summary>
		public string ConnectionId { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public Guid Id { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public bool MatchAccepted { get; private set; } = false;

		/// <summary>
		/// 
		/// </summary>
		public int Score { get; private set; } = 0;

		/// <summary>
		/// 
		/// </summary>
		public void AcceptMatch()
		{
			MatchAccepted = true;
		}

		/// <summary>
		/// 
		/// </summary>
		public PlayerContract ToContract()
		{
			return new PlayerContract(Id, Score);
		}
	}
}
