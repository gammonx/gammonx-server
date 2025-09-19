using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Single match session repository that manages match sessions by their IDs.
	/// </summary>
	public class MatchSessionRepository
	{
		private readonly Dictionary<Guid, IMatchSessionModel> _sessions = new();
		private readonly IMatchSessionFactory _matchSessionFactory;

		public MatchSessionRepository(IMatchSessionFactory matchSessionFactory)
		{
			_matchSessionFactory = matchSessionFactory;
		}

		public IMatchSessionModel Create(Guid matchId, QueueKey queueKey)
		{
			var matchSession = _matchSessionFactory.Create(matchId, queueKey);
			_sessions.Add(matchId, matchSession);
			return matchSession;
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
			if (!_sessions.Remove(matchId))
			{
				throw new KeyNotFoundException($"No match session found for matchId: {matchId}");
			}
		}
	}
}
