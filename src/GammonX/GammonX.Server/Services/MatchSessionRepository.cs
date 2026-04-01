using GammonX.Server.Models;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Single match session repository that manages match sessions by their IDs.
	/// </summary>
	public sealed class MatchSessionRepository
	{
        private readonly ConcurrentDictionary<Guid, Lazy<IMatchSessionModel>> _sessions = new();
        private readonly IMatchSessionFactory _matchSessionFactory;

		public MatchSessionRepository(IMatchSessionFactory matchSessionFactory)
		{
			_matchSessionFactory = matchSessionFactory;
		}

		public IMatchSessionModel Create(Guid matchId, QueueKey queueKey)
		{
            var lazy = new Lazy<IMatchSessionModel>(() => _matchSessionFactory.Create(matchId, queueKey));

            if (_sessions.TryAdd(matchId, lazy))
			{
				return lazy.Value;
			}
			throw new InvalidOperationException("match session already exists");

        }

		public IMatchSessionModel? Get(Guid matchId)
		{
			if (_sessions.TryGetValue(matchId, out var lazy))
			{
				return lazy.Value;
			}
			else
			{
				return null;
			}
		}

		public IMatchSessionModel? GetPlayersMatch(Guid playerId)
		{
			var playersMatch = _sessions.Values
				.Select(lazy => lazy.Value)
				.FirstOrDefault(matchSession =>
					matchSession.Player1.Id == playerId ||
					matchSession.Player2.Id == playerId);
			return playersMatch;
        }

		public IMatchSessionModel GetOrCreate(Guid matchId, QueueKey queueKey)
		{
            var lazy = _sessions.GetOrAdd(
                matchId,
                _ => new Lazy<IMatchSessionModel>(
                    () => _matchSessionFactory.Create(matchId, queueKey),
                    LazyThreadSafetyMode.ExecutionAndPublication));

            return lazy.Value;
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
