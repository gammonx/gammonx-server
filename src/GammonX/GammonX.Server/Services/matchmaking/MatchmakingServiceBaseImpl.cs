using GammonX.Server.Models;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
	// <inheritdoc />
	internal abstract class MatchmakingServiceBaseImpl : IMatchmakingService
	{
		/// <summary>
		/// Gets queue entries grouped by the queue entry id.
		/// </summary>
		protected readonly ConcurrentDictionary<Guid, QueueEntry> _queue = new();

		/// <summary>
		/// Gets queue id queue grouped by different queue keys.
		/// </summary>
		protected readonly ConcurrentDictionary<QueueKey, ConcurrentQueue<Guid>> _modeQueues = new();

		/// <summary>
		/// Gets match lobbies grouped by queue entries. A match lobby is created as soon as two players were matched
		/// </summary>
		protected readonly ConcurrentDictionary<QueueEntry, MatchLobby> _matchLobbies = new();

		// <inheritdoc />
		public abstract Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey);

		// <inheritdoc />
		public abstract Task MatchQueuedPlayersAsync();

		// <inheritdoc />
		public virtual bool TryFindQueueEntry(Guid queueId, out QueueEntry? entry)
		{
			var queueEntry = _queue.GetValueOrDefault(queueId);
			if (queueEntry == null)
			{
				entry = null;
				return false;
			}
			entry = queueEntry;
			return true;
		}

		// <inheritdoc />
		public virtual bool TryFindMatchLobby(Guid matchOrQueueId, out MatchLobby? matchLobby)
		{
			var kvp = _matchLobbies.FirstOrDefault(q => q.Value.MatchId == matchOrQueueId || q.Key.Id == matchOrQueueId);
			var queueEntry = kvp.Key;
			if (queueEntry == default)
			{
				matchLobby = null;
				return false;
			}
			matchLobby = kvp.Value;
			return true;
		}

		// <inheritdoc />
		public virtual bool TryRemoveMatchLobby(Guid matchId)
		{
			var queueEntries = _matchLobbies.Where(q => q.Value.MatchId == matchId);
			var found = queueEntries.Any();

			foreach (var entry in queueEntries)
			{
				_matchLobbies.TryRemove(entry);
			}

			return found;
		}
	}
}
