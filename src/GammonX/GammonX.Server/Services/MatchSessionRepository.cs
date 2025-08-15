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
		/// <exception cref="KeyNotFoundException"></exception>
		public IMatchSessionModel Get(Guid matchId)
		{
			if (_sessions.TryGetValue(matchId, out var session))
			{
				return session;
			}
			else
			{
				throw new KeyNotFoundException($"No match session found for matchId: {matchId}");
			}
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
