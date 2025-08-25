using GammonX.Engine.Models;
using GammonX.Engine.Services;

using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Provides a factory to create game sessions based on the match id and game modus.
	/// </summary>
	public interface IGameSessionFactory
	{
		/// <summary>
		/// Create a new game session model based on the match id and game modus.
		/// </summary>
		/// <param name="matchId">Match id.</param>
		/// <param name="modus">Game modus.</param>
		/// <returns>A game session</returns>
		IGameSessionModel Create(Guid matchId, GameModus modus);
	}

	// <inheritdoc />
	public class GameSessionFactory : IGameSessionFactory
	{
		private IDiceServiceFactory _diceServiceFactory;

		public GameSessionFactory(IDiceServiceFactory diceServiceFactory)
		{
			_diceServiceFactory = diceServiceFactory;
		}

		// <inheritdoc />
		public IGameSessionModel Create(Guid matchId, GameModus modus)
		{
			var diceService = _diceServiceFactory.Create();
			var boardService = BoardServiceFactory.Create(modus);

			switch (modus)
			{
				case GameModus.Portes:
					return new GameSessionImpl(matchId, modus, boardService, diceService);
				case GameModus.Plakoto:
					return new GameSessionImpl(matchId, modus, boardService, diceService);
				case GameModus.Fevga:
					return new GameSessionImpl(matchId, modus, boardService, diceService);
				case GameModus.Backgammon:
					return new GameSessionImpl(matchId, modus, boardService, diceService);
				case GameModus.Tavla:
					return new GameSessionImpl(matchId, modus, boardService, diceService);
				default:
					throw new ArgumentOutOfRangeException(nameof(modus), modus, "Unknown game variant.");
			}
		}
	}
}
