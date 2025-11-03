using GammonX.Server.Models;

using System.Collections.Concurrent;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Single match session repository that manages match sessions by their IDs.
	/// </summary>
	public class MatchSessionRepository
	{
		private readonly ConcurrentDictionary<Guid, IMatchSessionModel> _sessions = new();
		private readonly IMatchSessionFactory _matchSessionFactory;

		public MatchSessionRepository(IMatchSessionFactory matchSessionFactory)
		{
			_matchSessionFactory = matchSessionFactory;
		}

		public IMatchSessionModel Create(Guid matchId, QueueKey queueKey)
		{
			var matchSession = _matchSessionFactory.Create(matchId, queueKey);
			if (_sessions.TryAdd(matchId, matchSession))
			{
				return matchSession;
			}
			throw new KeyNotFoundException($"An erro occurred while creating a match session.");
		}

		public IMatchSessionModel? Get(Guid matchId)
		{
			if (_sessions.TryGetValue(matchId, out var session))
			{
				return session;
			}
			else
			{
				return null;
			}
		}

		public IMatchSessionModel GetOrCreate(Guid matchId, QueueKey queueKey)
		{
			var session = Get(matchId);
			if (session == null)
			{
				session = Create(matchId, queueKey);
			}
			return session;
		}

		public void Remove(Guid matchId)
		{
			if (!_sessions.TryRemove(matchId, out var _))
			{
				throw new KeyNotFoundException($"No match session found for matchId: {matchId}");
			}
		}
	}
}
