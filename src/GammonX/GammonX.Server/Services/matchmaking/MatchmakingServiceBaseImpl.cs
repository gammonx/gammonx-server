using GammonX.Models.Enums;

using GammonX.Server.Models;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
	// <inheritdoc />
	internal abstract class MatchmakingServiceBaseImpl : IMatchmakingService
	{
		// TODO: extract the different queue dictionaries into dynamo db to make game server stateless
		
		public abstract MatchModus Modus { get; }

		protected readonly PlayerConnectionRepository _playerConnectionRepository;

		/// <summary>
		/// Gets queue entries grouped by the queue entry id.
		/// </summary>
		protected readonly ConcurrentDictionary<Guid, QueueEntry> _queueEntries = new();

		/// <summary>
		/// Gets queue entry id queue grouped by different queue keys.
		/// </summary>
		protected readonly ConcurrentDictionary<QueueKey, ConcurrentQueue<Guid>> _modeQueues = new();

		/// <summary>
		/// Publish and remove all aliases under one gate so readers cannot observe partial registration.
		/// </summary>
		private readonly Lock _lobbyGate = new();

		/// <summary>
		/// Contains match lobbies grouped by their match id and/or their queue id.
		/// May contain a matchlobby up to three times.
		/// </summary>
		private readonly Dictionary<Guid, (MatchLobby Lobby, Guid[] QueueIds)> _lobbiesById = new();

		// <inheritdoc />
		public abstract Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey);

		// <inheritdoc />
		public Task LeaveQueueAsync(QueueEntry entry)
		{
			LeaveQueue(entry);
			return Task.FromResult(0);
		}

		public MatchmakingServiceBaseImpl(PlayerConnectionRepository playerConnectionRepository)
		{
			_playerConnectionRepository = playerConnectionRepository;

		}

		// <inheritdoc />
		public void TouchQueueEntry(Guid queueEntryId)
		{
			if (_queueEntries.TryGetValue(queueEntryId, out var entry))
			{
				entry.LastSeenUtc = DateTime.UtcNow;
			}
		}

		// <inheritdoc />
		public void CleanupExpiredQueueEntries(TimeSpan timeout)
		{
			var now = DateTime.UtcNow;
			foreach (var (queueId, entry) in _queueEntries)
			{
				if (now - entry.LastSeenUtc <= timeout)
					continue;

				// double-check at removal time
				if (_queueEntries.TryGetValue(queueId, out var current) && now - current.LastSeenUtc > timeout)
				{
					_queueEntries.TryRemove(queueId, out _);
				}
			}
		}

		// <inheritdoc />
		public abstract Task MatchQueuedPlayersAsync();

		// <inheritdoc />
		public virtual bool TryFindQueueEntry(Guid queueId, out QueueEntry? entry)
		{
			var queueEntry = _queueEntries.GetValueOrDefault(queueId);

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
            lock (_lobbyGate)
            {
                if (_lobbiesById.TryGetValue(matchOrQueueId, out var registration))
                {
                    matchLobby = registration.Lobby;
                    return true;
                }

                matchLobby = null;
                return false;
            }
		}

		// <inheritdoc />
		public virtual bool TryRemoveMatchLobby(Guid matchId)
		{
            lock (_lobbyGate)
            {
                if (!_lobbiesById.TryGetValue(matchId, out var registration) || registration.Lobby.MatchId != matchId)
                {
                    return false;
                }

                _lobbiesById.Remove(matchId);

                foreach (var queueId in registration.QueueIds)
                {
                    _lobbiesById.Remove(queueId);
                }

                return true;
            }
		}

		// <inheritdoc />
		public virtual MatchLobby[] GetMatchLobbies()
		{
            lock (_lobbyGate)
            {
                return _lobbiesById.Values.Select(registration => registration.Lobby).DistinctBy(lobby => lobby.MatchId).ToArray();
            }
		}

        protected void AddMatchLobby(MatchLobby lobby, params QueueEntry[] entries)
        {
            var queueIds = entries.Select(entry => entry.Id).ToArray();
            lock (_lobbyGate)
            {
                if (
					queueIds.Length == 0 || 
					_lobbiesById.ContainsKey(lobby.MatchId) || 
					queueIds.Contains(lobby.MatchId) ||
                    queueIds.Any(_lobbiesById.ContainsKey) ||
					queueIds.Distinct().Count() != queueIds.Length)
                {
                    throw new InvalidOperationException("The match lobby or one of its queue entries is already registered.");
                }

                var registration = (lobby, queueIds);
				// we once add the matchlobby by its match id
                _lobbiesById.Add(lobby.MatchId, registration);
                
				// we add the matchlobby by its two queue ids
				foreach (var queueId in queueIds)
                {
                    _lobbiesById.Add(queueId, registration);
                }
            }
        }

		// <inheritdoc />
		public virtual QueueEntry[] GetQueueEntries()
		{
			return _queueEntries.Values.ToArray();
		}

		protected virtual internal void Enqueue(QueueEntry entry)
		{
			_queueEntries[entry.Id] = entry;
			var modeQueue = _modeQueues.GetOrAdd(entry.QueueKey, _ => new ConcurrentQueue<Guid>());
			modeQueue.Enqueue(entry.Id);
		}

		protected virtual internal void LeaveQueue(QueueEntry entry)
		{
			_queueEntries.TryRemove(entry.Id, out _);
			// the mode queue is automatically cleanup while matching opponents for match lobbies
		}
	}
}
