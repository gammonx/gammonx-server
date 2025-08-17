using GammonX.Engine.Models;

using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	/// <summary>
	/// 
	/// </summary>
	public static class GameSessionFactory
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="matchId"></param>
		/// <param name="modus"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static IGameSessionModel Create(Guid matchId, GameModus modus)
		{
			switch (modus)
			{
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
