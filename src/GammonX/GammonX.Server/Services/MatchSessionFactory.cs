using GammonX.Engine.Models;

using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Provides a factory to create match sessions based on the match id and game modus.
	/// </summary>
	public interface IMatchSessionFactory
	{
		/// <summary>
		/// Create a new match session model based on the match id and match variant.
		/// </summary>
		/// <param name="id">Match id.</param>
		/// <param name="variant">match variant.</param>
		/// <returns>A match session</returns>
		IMatchSessionModel Create(Guid id, WellKnownMatchVariant variant);
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
		public IMatchSessionModel Create(Guid id, WellKnownMatchVariant variant)
		{
			switch (variant)
			{
				case WellKnownMatchVariant.Backgammon:
					return new BackgammonMatchSession(id, variant, [GameModus.Backgammon], _gameSessionFactory);
				case WellKnownMatchVariant.Tavla:
					return new TavlaMatchSession(id, variant, [GameModus.Tavla], _gameSessionFactory);
				case WellKnownMatchVariant.Tavli:
					return new TavliMatchSession(id, variant, [GameModus.Portes, GameModus.Plakoto, GameModus.Fevga], _gameSessionFactory);
				default:
					throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown match variant.");
			}
		}
	}
}
