using GammonX.Server.Models;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Single match session repository that manages match sessions by their IDs.
	/// TODO: extract repository into dynamo db in order to make game server stateless
	/// TODO: repository reads need to be concurrency save and atmoic
	/// TODO: provide an interface for the repository
	/// </summary>
	public sealed class MatchSessionRepository
	{
        private readonly ConcurrentDictionary<Guid, Lazy<IMatchSessionModel>> _sessions = new();
        private readonly IMatchSessionFactory _matchSessionFactory;

		public MatchSessionRepository(IMatchSessionFactory matchSessionFactory)
		{
			_matchSessionFactory = matchSessionFactory;
		}

		/// <summary>
		/// Commits the given match session to the repository.
		/// </summary>
		/// <param name="matchSession">The match session to commit.</param>
		/// <returns>Boolean indicating if the commit was successful.</returns>
		/// <exception cref="ArgumentNullException">Thrown if the provided match session is null.</exception>
		public bool Commit(IMatchSessionModel matchSession)
		{
			if (matchSession == null)
			{
				throw new ArgumentNullException(nameof(matchSession));
			}

			if (!_sessions.ContainsKey(matchSession.Id))
			{
				return false;
			}

			var snapshot = (IMatchSessionModel)matchSession.Clone();
			_sessions[matchSession.Id] = new Lazy<IMatchSessionModel>(() => snapshot);
			return true;
		}

		/// <summary>
		/// Creates a new match session with the given match ID and queue key.
		/// </summary>
		/// <remarks>
		/// The returned match session instance is a clone. Use <see cref="Commit(IMatchSessionModel)"/> to persist changes.
		/// </remarks>
		/// <param name="matchId">The ID of the match to create.</param>
		/// <param name="queueKey">The queue key associated with the match.</param>
		/// <returns>The created match session.</returns>
		/// <exception cref="InvalidOperationException"></exception>
		public IMatchSessionModel Create(Guid matchId, QueueKey queueKey)
		{
            var lazy = new Lazy<IMatchSessionModel>(() => _matchSessionFactory.Create(matchId, queueKey));

            if (_sessions.TryAdd(matchId, lazy))
			{
				return (IMatchSessionModel)lazy.Value.Clone();
			}
			throw new InvalidOperationException("match session already exists");

        }

		/// <summary>
		/// Retrieves the match session with the given match ID.
		/// </summary>
		/// <remarks>
		/// The returned match session instance is a clone. Use <see cref="Commit(IMatchSessionModel)"/> to persist changes.
		/// </remarks>
		/// <param name="matchId">The ID of the match to retrieve.</param>
		/// <returns>The match session if found; otherwise, null.</returns>
		public IMatchSessionModel? Get(Guid matchId)
		{
			if (_sessions.TryGetValue(matchId, out var lazy))
			{
				return (IMatchSessionModel)lazy.Value.Clone();
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Retrieves the match session for the given player ID, if any.
		/// </summary>
		/// <remarks>
		/// The returned match session instance is a clone. Use <see cref="Commit(IMatchSessionModel)"/> to persist changes.
		/// </remarks>
		/// <param name="playerId">The ID of the player whose match session is to be retrieved.</param>
		/// <returns>The match session if found; otherwise, null.</returns>
		public IMatchSessionModel? GetPlayersMatch(Guid playerId)
		{
			var playersMatch = _sessions.Values
				.Select(lazy => lazy.Value)
				.FirstOrDefault(matchSession =>
					matchSession.Player1.Id == playerId ||
					matchSession.Player2.Id == playerId);
			return (IMatchSessionModel?)playersMatch?.Clone();
        }

		/// <summary>
		/// Retrieves the match session for the given match ID, creating it if it does not exist.
		/// </summary>
		/// <param name="matchId">The ID of the match to retrieve or create.</param>
		/// <param name="queueKey">The queue key associated with the match.</param>
		/// <returns>The match session.</returns>
		public IMatchSessionModel GetOrCreate(Guid matchId, QueueKey queueKey)
		{
            var lazy = _sessions.GetOrAdd(
                matchId,
                _ => new Lazy<IMatchSessionModel>(
                    () => _matchSessionFactory.Create(matchId, queueKey),
                    LazyThreadSafetyMode.ExecutionAndPublication));

            return (IMatchSessionModel)lazy.Value.Clone();
        }

		public bool TryRemove(Guid matchId)
		{
			return _sessions.TryRemove(matchId, out var _);
        }

		public void Remove(Guid matchId)
		{
			if (!_sessions.TryRemove(matchId, out var _))
			{
				throw new KeyNotFoundException($"No match session found for matchId: '{matchId}'");
			}
		}
	}
}
