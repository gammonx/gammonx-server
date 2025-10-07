using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Provides the capability to join players into a match lobby.
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
		/// Checks if some of the players in the queue can be matched together into a match lobby.
		/// </summary>
		/// <returns>A task to be awaited.</returns>
		Task MatchQueuedPlayersAsync();
	}
}
