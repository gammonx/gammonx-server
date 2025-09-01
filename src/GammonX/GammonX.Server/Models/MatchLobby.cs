namespace GammonX.Server.Models
{
	public class MatchLobby
	{
		public MatchLobby(Guid matchId, QueueKey queueKey, LobbyEntry player1)
		{
			MatchId = matchId;
			Player1 = player1;
			QueueKey = queueKey;
		}

		public Guid MatchId { get; private set; }
		
		public QueueKey QueueKey { get; private set; }

		/// <summary>
		/// Gets the web socket group name for this match lobby.
		/// </summary>
		public string GroupName => $"match_{MatchId}";

		/// <summary>
		/// Gets the first player to join the match lobby.
		/// </summary>
		public LobbyEntry Player1 { get; private set; }

		/// <summary>
		/// Gets the second player to join the match lobby, if any.
		/// </summary>
		public LobbyEntry? Player2 { get; private set; }

		/// <summary>
		/// Joins a second player to the match lobby.
		/// </summary>
		/// <param name="player2">Second player.</param>
		public void Join(LobbyEntry player2)
		{
			if (QueueKey.QueueType == WellKnownMatchType.Bot)
			{
				throw new InvalidOperationException("Second player cannot join a bot game");
			}

			Player2 = player2;
		}
	}
}
