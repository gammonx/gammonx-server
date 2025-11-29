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
		OpponentFound = 1
	}
}
