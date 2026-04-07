using GammonX.Server.Models;

namespace GammonX.Server.Services
{
    /// <summary>
    /// Provides the capability to join players into a match lobby.
    /// Goals of this concurrency robust matchmaking service:
	/// - No player is matched twice
	/// - No player disappears
	/// - All matches are valid
	/// - Thread interleavings do not corrupt state
	/// Key idea is to run <see cref="MatchQueuedPlayersAsync"/> in parallel from multiple threads against the same queues.
	/// </summary>
	public interface IMatchmakingService
	{
		/// <summary>
		/// Tries to find a queue entry by its id. Returns null if not found.
		/// </summary>
		/// <param name="queueId">Queue id to find.</param>
		/// <param name="entry">Result</param>
		/// <returns>Boolean indicating if a queue entry was found.</returns>
		bool TryFindQueueEntry(Guid queueId, out QueueEntry? entry);

		/// <summary>
		/// Tries to find a match lobby by its ID. Returns null if not found.
		/// </summary>
		/// <param name="matchOrQueueId">Match id/Queue id to search for.</param>
		/// <param name="matchLobby">Result.</param>
		/// <returns>Boolean indicating if a match lobby was found.</returns>
		bool TryFindMatchLobby(Guid matchOrQueueId, out MatchLobby? matchLobby);

		/// <summary>
		/// Removes a match lobby with the given <paramref name="matchId"/>.
		/// </summary>
		/// <param name="matchId">Id of the match lobby.</param>
		/// <returns>Returns <c>true</c> if the match lobby was removed. Otherwise returns <c>false</c>.</returns>
		bool TryRemoveMatchLobby(Guid matchId);

		/// <summary>
		/// Joins the given <paramref name="playerId"/> to the matchmaking queue.
		/// </summary>
		/// <param name="playerId">Player to join the queue.</param>
		/// <param name="queueKey">Key of the joined queue.</param>
		/// <returns>A guid of the created match lobby.</returns>
		Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey);

        /// <summary>
        /// The given <paramref name="entry"/> leaves the matchmaking queue.
        /// </summary>
        /// <param name="entry">Queue entry who wants to leave.</param>
        /// <returns>A task to be awaited.</returns>
        Task LeaveQueueAsync(QueueEntry entry);

        /// <summary>
        /// Updates the <see cref="QueueEntry.LastSeenUtc"/> time stamp to the current time in utc.
        /// </summary>
        void TouchQueueEntry(Guid queueId);

        /// <summary>
        /// Cleans up expired queue entries which have not been polled within the given <paramref name="timeout"/>.
        /// </summary>
        /// <param name="timeout">Timeout after which a queue entry is considered expired.</param>
        void CleanupExpiredQueueEntries(TimeSpan timeout);

        /// <summary>
        /// Checks if some of the players in the queue can be matched together into a match lobby.
        /// </summary>
        /// <returns>A task to be awaited.</returns>
        Task MatchQueuedPlayersAsync();

		/// <summary>
		/// Gets all match lobbies.
		/// </summary>
		/// <returns>Returns an array of all match lobbies.</returns>
		MatchLobby[] GetMatchLobbies();

		/// <summary>
		/// Gets all queue entries.
		/// </summary>
		/// <returns>Returns an array of all queue entries.</returns>
		QueueEntry[] GetQueueEntries();
    }
}
