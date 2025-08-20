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

		public bool NextGameAccepted { get; private set; } = false;

		/// <summary>
		/// Gets the score of the player in the current match session.
		/// </summary>
		public int Score { get; internal set; } = 0;

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

		public PlayerContract ToContract()
		{
			return new PlayerContract()
			{
				Id = Id,
				Score = Score
			};
		}
	}
}
