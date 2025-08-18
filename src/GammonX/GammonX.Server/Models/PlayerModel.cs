using GammonX.Server.Contracts;

namespace GammonX.Server.Models
{
	public class PlayerModel
	{
		public PlayerModel(Guid id, string connectionId)
		{
			Id = id;
			ConnectionId = connectionId;
		}

		/// <summary>
		/// Gets the web socket connection id of the player.
		/// </summary>
		public string ConnectionId { get; private set; }

		public Guid Id { get; private set; }

		public bool MatchAccepted { get; private set; } = false;

		/// <summary>
		/// Gets the score of the player in the current match session.
		/// </summary>
		public int Score { get; private set; } = 0;

		/// <summary>
		/// Accepts the match for this player.
		/// </summary>
		public void AcceptMatch()
		{
			MatchAccepted = true;
		}

		public PlayerContract ToContract()
		{
			return new PlayerContract(Id, Score);
		}
	}
}
