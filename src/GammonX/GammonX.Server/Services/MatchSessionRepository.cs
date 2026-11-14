using GammonX.Server.Models;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
    /// <summary>
    /// Stores match snapshots and provides exclusive leases for changing them.
    /// </summary>
    public sealed class MatchSessionRepository
    {
        private readonly ConcurrentDictionary<Guid, IMatchSessionModel> _sessions = new();
        private readonly IMatchSessionFactory _matchSessionFactory;
        private readonly Dictionary<Guid, MatchLock> _matchLocks = new();

        /// <summary>Creates a repository using the supplied match factory.</summary>
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

            await matchLock.Semaphore.WaitAsync();
            return new MatchSessionLease(this, matchId, matchLock);
        }

        /// <summary>Returns a detached snapshot for observation, or null if the match is absent.</summary>
        /// <remarks>This snapshot cannot be committed. Use a lease to read state for an update.</remarks>
        public IMatchSessionModel? Get(Guid matchId)
        {
            return _sessions.TryGetValue(matchId, out var session)
                ? (IMatchSessionModel)session.Clone()
                : null;
        }

        /// <summary>Returns a detached snapshot of a player's match, or null if none exists.</summary>
        /// <remarks>This snapshot cannot be committed. Use a lease to read state for an update.</remarks>
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

            private readonly object _gate = new();

            private IMatchSessionModel? _session;

            private bool _loaded;

            private bool _disposed;

            internal MatchSessionLease(MatchSessionRepository repository, Guid matchId, MatchLock matchLock)
            {
                _repository = repository;
                _matchId = matchId;
                _matchLock = matchLock;
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
                    // Store a separate snapshot so uncommitted caller changes remain isolated.
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
			/// Commits this lease's working snapshot, if one exists.
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
			/// Removes the leased match and discards this lease's working snapshot.
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
                            _matchLock.Semaphore.Release();
                            if (--_matchLock.Users == 0)
                            {
                                _repository._matchLocks.Remove(_matchId);
                                _matchLock.Semaphore.Dispose();
                            }
                        }
                    }
                }
                return ValueTask.CompletedTask;
            }

            private void ThrowIfDisposed()
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
            }
        }
    }
}
