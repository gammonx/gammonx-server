namespace GammonX.Server.Contracts
{
	public class PlayerContract
	{
		public PlayerContract(Guid id, int score)
		{
			Id = id;
			Score = score;
		}

		public Guid Id { get; set; }

		/// <summary>
		/// Gets or sets the match score for this player.
		/// </summary>
		public int Score { get; set; } = 0;
	}
}
