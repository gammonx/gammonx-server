using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Single match session repository that manages match sessions by their IDs.
	/// </summary>
	public class MatchSessionRepository
	{
		private readonly Dictionary<Guid, IMatchSessionModel> _sessions = new();

		public IMatchSessionModel Create(Guid matchId, WellKnownMatchVariant variant)
		{
			var matchSession = MatchSessionFactory.Create(matchId, variant);
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

		public IMatchSessionModel GetOrCreate(Guid matchId, WellKnownMatchVariant variant)
		{
			var session = Get(matchId);
			if (session == null)
			{
				session = Create(matchId, variant);
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
