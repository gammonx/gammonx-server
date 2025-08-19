using GammonX.Engine.Models;

using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	public static class GameSessionFactory
	{
		public static IGameSessionModel Create(Guid matchId, GameModus modus)
		{
			switch (modus)
			{
				case GameModus.Portes:
					return new GameSessionImpl(matchId, modus);
				case GameModus.Plakoto:
					return new GameSessionImpl(matchId, modus);
				case GameModus.Fevga:
					return new GameSessionImpl(matchId, modus);
				case GameModus.Backgammon:
					return new GameSessionImpl(matchId, modus);
				case GameModus.Tavla:
					return new GameSessionImpl(matchId, modus);
				default:
					throw new ArgumentOutOfRangeException(nameof(modus), modus, "Unknown game variant.");
			}
		}
	}
}
