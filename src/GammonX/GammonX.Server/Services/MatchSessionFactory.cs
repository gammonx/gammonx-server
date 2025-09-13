using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Provides a factory to create match sessions based on the match id and game modus.
	/// </summary>
	public interface IMatchSessionFactory
	{
		/// <summary>
		/// Create a new match session model based on the match id and queue key.
		/// </summary>
		/// <param name="id">Match id.</param>
		/// <param name="queueKey">Queue key.</param>
		/// <returns>A match session</returns>
		IMatchSessionModel Create(Guid id, QueueKey queueKey);
	}

	// <inheritdoc />
	public class MatchSessionFactory : IMatchSessionFactory
	{
		private IGameSessionFactory _gameSessionFactory;

		public MatchSessionFactory(IGameSessionFactory gameSessionFactory)
		{
			_gameSessionFactory = gameSessionFactory;
		}

		// <inheritdoc />
		public IMatchSessionModel Create(Guid id, QueueKey queueKey)
		{
			switch (queueKey.MatchVariant)
			{
				case WellKnownMatchVariant.Backgammon:
					return new BackgammonMatchSession(id, queueKey, _gameSessionFactory);
				case WellKnownMatchVariant.Tavla:
					return new TavlaMatchSession(id, queueKey, _gameSessionFactory);
				case WellKnownMatchVariant.Tavli:
					return new TavliMatchSession(id, queueKey, _gameSessionFactory);
				default:
					throw new ArgumentOutOfRangeException(nameof(queueKey.MatchVariant), "Unknown match variant.");
			}
		}
	}
}
