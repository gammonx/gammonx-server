namespace GammonX.Server.Models
{
	/// <summary>
	/// Representst the status of a queue entry.
	/// </summary>
	public enum QueueEntryStatus
	{
		/// <summary>
		/// The queue entry is still in the queue and waits for a match lobby match/creation.
		/// </summary>
		WaitingForOpponent = 0,
		/// <summary>
		/// The queue entry left the queue. A match lobby was created and is waiting to be used.
		/// </summary>
		OpponentFound = 1,
		/// <summary>
		/// The queue entry left the queue without starting any match lobby.
		/// </summary>
		Discarded = 2
	}

	/// <summary>
	/// Describes which action is expected from the player until the deadline expires.
	/// </summary>
	public enum TurnDeadlineAction
	{
		/// <summary>
		/// Join an unstarted match (signalR socket) based on an already constructed match lobby.
		/// </summary>
		Join,
		StartMatch,
		/// <summary>
		/// Accept the next game within a running match session.
		/// </summary>
		AcceptNextGame,
		/// <summary>
		/// Play a turn (roll, move, endturn, offer double cube).
		/// </summary>
		PlayTurn,
		/// <summary>
		/// Respond to a pending doubling cube offer.
		/// </summary>
		RespondToCube
	}
}
