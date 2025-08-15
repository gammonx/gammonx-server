namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	public class MatchLobby
	{
		public MatchLobby(Guid matchId, WellKnownMatchVariant variant, Player player1)
		{
			MatchId = matchId;
			Player1 = player1;
			Variant = variant;
		}

		/// <summary>
		/// 
		/// </summary>
		public Guid MatchId { get; private set; }
		
		/// <summary>
		/// 
		/// </summary>
		public WellKnownMatchVariant Variant { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		public string GroupName => $"match_{MatchId}";

		/// <summary>
		/// 
		/// </summary>
		public Player Player1 { get; private set; }
		
		/// <summary>
		/// 
		/// </summary>
		public Player? Player2 { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="player2"></param>
		public void Join(Player player2)
		{
			Player2 = player2;
		}
	}
}
