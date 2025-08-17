using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	/// <summary>
	/// 
	/// </summary>
	public class MatchSessionRepository
	{
		private readonly Dictionary<Guid, IMatchSessionModel> _sessions = new();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matchId"></param>
		/// <param name="variant"></param>
		/// <returns></returns>
		public IMatchSessionModel Create(Guid matchId, WellKnownMatchVariant variant)
		{
			var matchSession = MatchSessionFactory.Create(matchId, variant);
			_sessions.Add(matchId, matchSession);
			return matchSession;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matchId"></param>
		/// <returns></returns>
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matchId"></param>
		/// <param name="variant"></param>
		/// <returns></returns>
		public IMatchSessionModel GetOrCreate(Guid matchId, WellKnownMatchVariant variant)
		{
			var session = Get(matchId);
			if (session == null)
			{
				session = Create(matchId, variant);
			}
			return session;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="matchId"></param>
		/// <exception cref="KeyNotFoundException"></exception>
		public void Remove(Guid matchId)
		{
			if (!_sessions.Remove(matchId))
			{
				throw new KeyNotFoundException($"No match session found for matchId: {matchId}");
			}
		}
	}
}
