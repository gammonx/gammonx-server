using GammonX.Engine.Models;

using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	// TODO :: allow the creation of match session with points (e.g. first to reach 5 or 7)

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
					return new BackgammonMatchSession(id, queueKey.MatchVariant, queueKey.QueueType, [GameModus.Backgammon], _gameSessionFactory);
				case WellKnownMatchVariant.Tavla:
					return new TavlaMatchSession(id, queueKey.MatchVariant, queueKey.QueueType, [GameModus.Tavla], _gameSessionFactory);
				case WellKnownMatchVariant.Tavli:
					return new TavliMatchSession(id, queueKey.MatchVariant, queueKey.QueueType, [GameModus.Portes, GameModus.Plakoto, GameModus.Fevga], _gameSessionFactory);
				default:
					throw new ArgumentOutOfRangeException(nameof(queueKey.MatchVariant), "Unknown match variant.");
			}
		}
	}
}
