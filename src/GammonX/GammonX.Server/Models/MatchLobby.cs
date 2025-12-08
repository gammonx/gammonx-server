using GammonX.Models.Enums;

namespace GammonX.Server.Models
{
	public class MatchLobby
	{
		public MatchLobby(Guid matchId, QueueKey queueKey, LobbyEntry player1)
		{
			MatchId = matchId;
			Player1 = player1;
			QueueKey = queueKey;
			if (queueKey.MatchModus == MatchModus.Bot)
			{
				// a match lobby against a bot is instantly ready
				Status = QueueEntryStatus.OpponentFound;
			}
			else
			{
				Status = QueueEntryStatus.WaitingForOpponent;
			}
		}

		/// <summary>
		/// Gets the uniquely identifier of a given match and its lobby.
		/// </summary>
		public Guid MatchId { get; private set; }
		
		/// <summary>
		/// Gets the queue key determining the variant, type and modus of the given match.
		/// </summary>
		public QueueKey QueueKey { get; private set; }

		/// <summary>
		/// Gets the status of the given match lobby.
		/// </summary>
		public QueueEntryStatus Status { get; private set; }

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
			if (QueueKey.MatchModus == MatchModus.Bot)
			{
				throw new InvalidOperationException("Second player cannot join a bot game");
			}

			Player2 = player2;
			Status = QueueEntryStatus.OpponentFound;
		}
	}
}
