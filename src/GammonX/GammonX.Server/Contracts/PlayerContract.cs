namespace GammonX.Server.Contracts
{
	/// <summary>
	/// 
	/// </summary>
	public class PlayerContract
	{
		public PlayerContract(Guid id, int score)
		{
			Id = id;
			Score = score;
		}

		/// <summary>
		/// 
		/// </summary>
		public Guid Id { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public int Score { get; private set; } = 0;
	}
}
