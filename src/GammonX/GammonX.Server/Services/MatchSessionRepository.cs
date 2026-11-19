using GammonX.Server.Models;
using GammonX.Server.Contracts;
using GammonX.Server.Diagnostics;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
    /// <summary>
    /// Stores match snapshots and provides exclusive leases for changing them.
    /// </summary>
    public sealed class MatchSessionRepository
    {
        /// <summary>
        /// Returns the given match session by its match id.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, IMatchSessionModel> _sessions = new();

        internal int Count => _sessions.Count;

        private readonly IMatchSessionFactory _matchSessionFactory;

        /// <summary>
        /// Returns the given match lock by its match id.
        /// </summary>
        private readonly Dictionary<Guid, MatchLock> _matchLocks = new();

        /// <summary>
		/// Creates a repository using the supplied match factory
		/// .</summary>
        public MatchSessionRepository(IMatchSessionFactory matchSessionFactory)
        {
            _matchSessionFactory = matchSessionFactory;
        }

        /// <summary>
        /// Acquires exclusive access to a match, including creation and removal.
        /// </summary>
        /// <remarks>
        /// The lease owns its working snapshot. Pass the lease to helpers instead of acquiring it again;
        /// acquisition is not reentrant. Do not share a lease or its mutable snapshot between operations.
        /// </remarks>
        /// <param name="matchId">The match to synchronize.</param>
        /// <returns>A lease that releases access when disposed.</returns>
        public async Task<MatchSessionLease> AcquireStateAsync(Guid matchId)
        {
            using var measurement = ServerMetrics.Measure(ServerMetrics.LeaseWait);
            MatchLock matchLock;

            lock (_matchLocks)
            {
                if (!_matchLocks.TryGetValue(matchId, out matchLock!))
                {
                    matchLock = new MatchLock();
                    _matchLocks.Add(matchId, matchLock);
                }

                // Include waiters so removing a session cannot split its lock identity.
                matchLock.Users++;
            }

            // We lock the semaphore until the returned lease is disposed
            await matchLock.Semaphore.WaitAsync();

            return new MatchSessionLease(this, matchId, matchLock);
        }

        /// <summary>
		/// Returns a detached snapshot for observation, or null if the match is absent.
		/// </summary>
        /// <remarks>
		/// This snapshot cannot be committed. Use a lease to read state for an update.
		/// </remarks>
        public IMatchSessionModel? Get(Guid matchId)
        {
            return _sessions.TryGetValue(matchId, out var session)
                ? (IMatchSessionModel)session.Clone()
                : null;
        }

        /// <summary>
		/// Returns a detached snapshot of a players match, or null if none exists.
		/// </summary>
        /// <remarks>
		/// This snapshot cannot be committed. Use a lease to read state for an update.
		/// </remarks>
        public IMatchSessionModel? GetPlayersMatch(Guid playerId)
        {
            var session = _sessions.Values.FirstOrDefault(value =>
                value.Player1.Id == playerId || value.Player2.Id == playerId);
            return (IMatchSessionModel?)session?.Clone();
        }

        internal sealed class MatchLock
        {
            public readonly SemaphoreSlim Semaphore = new(1, 1);

            public int Users;
        }

        /// <summary>
        /// Exclusive access to one matchs working snapshot and persistence operations.
        /// </summary>
        public sealed class MatchSessionLease : IAsyncDisposable
        {
            private readonly MatchSessionRepository _repository;

            private readonly Guid _matchId;

            private readonly MatchLock _matchLock;

            private readonly Lock _gate = new();

            private IMatchSessionModel? _session;

            /// <summary>
            /// Indicates if the given session was already read from the repository.
            /// </summary>
            private bool _loaded;

            private bool _disposed;

            private readonly ServerMetrics.DurationScope _holdMeasurement;

            internal MatchSessionLease(MatchSessionRepository repository, Guid matchId, MatchLock matchLock)
            {
                _repository = repository;
                _matchId = matchId;
                _matchLock = matchLock;
                _holdMeasurement = ServerMetrics.Measure(ServerMetrics.LeaseHold);
            }

            /// <summary>
			/// Returns this leases working snapshot, or null if no match exists.
			/// </summary>
            /// <remarks>
			/// Repeated reads return the same working snapshot until removal or creation.
			/// </remarks>
			/// <returns>The match session instance.</returns>
            public IMatchSessionModel? Get()
            {
                lock (_gate)
                {
                    ThrowIfDisposed();
                    if (!_loaded)
                    {
                        _session = _repository.Get(_matchId);
                        _loaded = true;
                    }
                    return _session;
                }
            }

            /// <summary>
            /// Returns the seated player associated with a connection without loading a working copy.
            /// </summary>
            public Guid GetCallingPlayerId(string connectionId)
            {
                lock (_gate)
                {
                    var match = GetMatchSession();
                    if (match != null && match.Player1.ConnectionId == connectionId)
                    {
                        return match.Player1.Id;
                    }
                    if (match != null && match.Player2.ConnectionId == connectionId)
                    {
                        return match.Player2.Id;
                    }
                    throw new InvalidOperationException("The calling player is not part of the match session.");
                }
            }

            /// <summary>
            /// Returns the opponent of a seated player without loading a working copy.
            /// </summary>
            public Guid GetOtherPlayerId(Guid callingPlayerId)
            {
                lock (_gate)
                {
                    var match = GetMatchSession();
                    if (match != null && match.Player1.Id.Equals(callingPlayerId))
                    {
                        return match.Player2.Id;
                    }
                    if (match != null && match.Player2.Id.Equals(callingPlayerId))
                    {
                        return match.Player1.Id;
                    }

                    throw new InvalidOperationException("The calling player is not part of the match session.");
                }
            }

            /// <summary>
            /// Returns whether the given player occupies a bot seat.
            /// </summary>
            public bool IsBotPlayer(Guid playerId)
            {
                lock (_gate)
                {
                    var match = GetMatchSession() ?? throw new InvalidOperationException("match cannot be null");
                    return (match.Player1.Id == playerId && match.Player1.IsBot)
                        || (match.Player2.Id == playerId && match.Player2.IsBot);
                }
            }

            /// <summary>
            /// Returns the connection of a seated player without loading a working copy.
            /// </summary>
            public string GetPlayerConnectionId(Guid playerId)
            {
                lock (_gate)
                {
                    var match = GetMatchSession() ?? throw new InvalidOperationException("match cannot be null");

                    if (match.Player1.Id.Equals(playerId))
                    {
                        return match.Player1.ConnectionId;
                    }
                    if (match.Player2.Id.Equals(playerId))
                    {
                        return match.Player2.ConnectionId;
                    }

                    throw new InvalidOperationException("The calling player is not part of the match session.");
                }
            }

            /// <summary>
            /// Returns whether a seated player plays white.
            /// </summary>
            public bool IsWhite(Guid callingPlayerId)
            {
                lock (_gate)
                {
                    var match = GetMatchSession() ?? throw new InvalidOperationException("match cannot be null");
                    if (match.Player1.Id.Equals(callingPlayerId))
                    {
                        return true;
                    }
                    if (match.Player2.Id.Equals(callingPlayerId))
                    {
                        return false;
                    }

                    throw new InvalidOperationException("Player is not part of this match session.");
                }
            }

            /// <summary>
            /// Builds detached game responses without loading a mutable match copy.
            /// </summary>
            /// <param name="callingPlayerId">The calling player id.</param>
            internal EventGameStatePayload? ReadGameState(Guid callingPlayerId)
            {
                lock (_gate)
                {
                    var match = GetMatchSession();
                    return match?.GetGameState(callingPlayerId);
                }
            }

            /// <summary>
            /// Builds detached match responses without loading a mutable match copy.
            /// </summary>
            /// <param name="callingPlayerId">The calling player id.</param>
            internal EventMatchStatePayload? ReadMatchState(Guid callingPlayerId)
            {
                lock (_gate)
                {
                    var match = GetMatchSession();
                    return match?.ToPayload(callingPlayerId);
                }
            }

            /// <summary>
			/// Creates the leased match and returns its working snapshot.
			/// </summary>
			/// <param name="queueKey">The queue key to search for.</param>
			/// <returns>The match session instance.</returns>
            /// <exception cref="InvalidOperationException">The match already exists.</exception>
            public IMatchSessionModel Create(QueueKey queueKey)
            {
                lock (_gate)
                {
                    ThrowIfDisposed();
                    if (_repository._sessions.ContainsKey(_matchId))
                    {
                        throw new InvalidOperationException("Match session already exists.");
                    }

                    var session = _repository._matchSessionFactory.Create(_matchId, queueKey);
                    // We store a separate snapshot so uncommitted caller changes remain isolated.
                    if (!_repository._sessions.TryAdd(_matchId, (IMatchSessionModel)session.Clone()))
                    {
                        throw new InvalidOperationException("Match session already exists.");
                    }
                    _session = session;
                    _loaded = true;
                    return session;
                }
            }

            /// <summary>
			/// Returns the working snapshot, creating the leased match if necessary.
			/// </summary>
			/// <param name="queueKey">The queue key to search for.</param>
			/// <returns>The match session instance.</returns>
            public IMatchSessionModel GetOrCreate(QueueKey queueKey)
            {
                lock (_gate)
                {
                    ThrowIfDisposed();
                    return Get() ?? Create(queueKey);
                }
            }

            /// <summary>
			/// Commits this leases working snapshot, if one exists.
			/// </summary>
            /// <returns>False if there is no working match or it has been removed.</returns>
            public bool Commit()
            {
                lock (_gate)
                {
                    ThrowIfDisposed();
                    if (_session == null || !_repository._sessions.TryGetValue(_matchId, out var current))
                    {
                        return false;
                    }

                    var snapshot = (IMatchSessionModel)_session.Clone();
                    return _repository._sessions.TryUpdate(_matchId, snapshot, current);
                }
            }

            /// <summary>
			/// Removes the leased match and discards this leases working snapshot.
			/// </summary>
            /// <returns>Whether the match existed.</returns>
            public bool TryRemove()
            {
                lock (_gate)
                {
                    ThrowIfDisposed();
                    var removed = _repository._sessions.TryRemove(_matchId, out _);
                    _session = null;
                    _loaded = true;
                    return removed;
                }
            }

            /// <summary>
			/// Removes the leased match.
			/// </summary>
            /// <exception cref="KeyNotFoundException">No match exists.</exception>
            public void Remove()
            {
                if (!TryRemove())
                {
                    throw new KeyNotFoundException($"No match session found for matchId: '{_matchId}'");
                }
            }

            /// <summary>
			/// Releases exclusive access. Further lease operations are rejected.
			/// </summary>
            public ValueTask DisposeAsync()
            {
                lock (_gate)
                {
                    if (!_disposed)
                    {
                        _disposed = true;
                        _session = null;

                        lock (_repository._matchLocks)
                        {
                            // we release the match session lease
                            _matchLock.Semaphore.Release();
                            if (--_matchLock.Users == 0)
                            {
                                _repository._matchLocks.Remove(_matchId);
                                _matchLock.Semaphore.Dispose();
                            }
                        }
                        _holdMeasurement.Dispose();
                    }
                }
                return ValueTask.CompletedTask;
            }

            private void ThrowIfDisposed()
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
            }

            private IMatchSessionModel? GetMatchSession()
            {
                ThrowIfDisposed();
                var match = _loaded ? _session : _repository._sessions.GetValueOrDefault(_matchId);
                return match;
            }
        }
    }
}
