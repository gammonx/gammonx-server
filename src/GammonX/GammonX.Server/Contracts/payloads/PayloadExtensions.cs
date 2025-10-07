using GammonX.Server.Models;

namespace GammonX.Server.Contracts
{
	public static class PayloadExtensions
	{
		public static RequestQueueEntryPayload ToPayload(this QueueEntry entry)
		{
			return new RequestQueueEntryPayload { QueueId = entry.Id,  MatchId = null, Status = MatchLobbyStatus.WaitingForOpponent };
		}

		public static RequestQueueEntryPayload ToPayload(this MatchLobby lobby)
		{
			return new RequestQueueEntryPayload { QueueId = null, MatchId = lobby.MatchId, Status = MatchLobbyStatus.OpponentFound };
		}
	}
}
