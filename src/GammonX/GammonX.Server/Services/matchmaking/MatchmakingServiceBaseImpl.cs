using GammonX.Models.Enums;

using GammonX.Server.Bot;
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
		/// Serializes lobby publication, queue cancellation and expiry for this mode.
		/// </summary>
		private readonly Lock _lobbyGate = new();

        private int _lobbyCount;

        internal int LobbyCount
        {
            get
            {
                lock (_lobbyGate)
                {
                    return _lobbyCount;
                }
            }
        }

		/// <summary>
		/// Contains match lobbies grouped by their match id and/or their queue id.
		/// May contain a matchlobby up to three times.
		/// </summary>
		private readonly Dictionary<Guid, (MatchLobby Lobby, Guid[] QueueIds)> _lobbiesById = new();

		// <inheritdoc />
		public abstract Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey);

		// <inheritdoc />
		public virtual Task<bool> LeaveQueueAsync(QueueEntry entry)
		{
			lock (_lobbyGate)
			{
				return Task.FromResult(_queueEntries.TryRemove(entry.Id, out _));
			}
		}

		public MatchmakingServiceBaseImpl(PlayerConnectionRepository playerConnectionRepository)
		{
			_playerConnectionRepository = playerConnectionRepository;

		}

		// <inheritdoc />
		public void TouchQueueEntry(Guid queueEntryId)
		{
			lock (_lobbyGate)
			{
				if (_queueEntries.TryGetValue(queueEntryId, out var entry))
				{
					entry.LastSeenUtc = DateTime.UtcNow;
				}
			}
		}

		// <inheritdoc />
		public void CleanupExpiredQueueEntries(TimeSpan timeout)
		{
			var now = DateTime.UtcNow;
			foreach (var (queueId, entry) in _queueEntries)
			{
				if (now - entry.LastSeenUtc <= timeout)
				{
					continue;
				}

				lock (_lobbyGate)
				{
					if (now - entry.LastSeenUtc > timeout)
					{
						_queueEntries.TryRemove(queueId, out _);
					}
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
                _lobbyCount--;

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
                _lobbyCount++;
                
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

        protected void MatchQueuedEntries(QueueKey lobbyKey, QueueEntry first, QueueEntry? second = null)
        {
			// We expect the second entry to be null in caase of a bot game
            QueueEntry[] entries = second == null ? [first] : [first, second];
            lock (_lobbyGate)
            {
                try
                {
                    // Cancellation or expiry can win after the matcher dequeues the IDs.
                    if (entries.Any(entry => !_queueEntries.ContainsKey(entry.Id)))
                    {
                        return;
                    }

					var matchId = Guid.NewGuid();
					var firstPlayer = _playerConnectionRepository.GetOrCreate(first.PlayerId);

                    var lobby = new MatchLobby(matchId, lobbyKey, firstPlayer);
                    
					if (second != null)
                    {
						var secondPlayer = _playerConnectionRepository.GetOrCreate(second.PlayerId);
                        lobby.Join(secondPlayer);
                    }
                    else
                    {
						var botPlayerId = WellKnownBotPlayers.GetBotPlayerId(lobbyKey.BotLevel);
                        var bot = _playerConnectionRepository.GetOrCreate(botPlayerId);
                        bot.SetConnectionId(WellKnownBotPlayers.ConnectionId);
                        lobby.Join(bot);
                    }

                    AddMatchLobby(lobby, entries);

					// We remove the queue entries only after the matchlobby was published
                    foreach (var entry in entries)
                    {
                        _queueEntries.TryRemove(entry.Id, out _);
                    }
                }
                finally
                {
                    // A failed pairing must return every still-pending ID to its original queue.
                    foreach (var entry in entries)
                    {
                        if (_queueEntries.ContainsKey(entry.Id))
                        {
                            _modeQueues[entry.QueueKey].Enqueue(entry.Id);
                        }
                    }
                }
            }
        }
	}
}
