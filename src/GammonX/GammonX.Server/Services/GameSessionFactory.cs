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
				// TODO :: do we need specific game session for each game variant?
				case GameModus.Portes:
					return new GameSessionImpl(matchId);
				case GameModus.Plakoto:
					throw new ArgumentOutOfRangeException(nameof(modus), modus, "Unknown game variant.");
				case GameModus.Fevga:
					throw new ArgumentOutOfRangeException(nameof(modus), modus, "Unknown game variant.");
				case GameModus.Backgammon:
					throw new ArgumentOutOfRangeException(nameof(modus), modus, "Unknown game variant.");
				case GameModus.Tavla:
					throw new ArgumentOutOfRangeException(nameof(modus), modus, "Unknown game variant.");
				default:
					throw new ArgumentOutOfRangeException(nameof(modus), modus, "Unknown game variant.");
			}
		}
	}
}
