using GammonX.Engine.Models;

using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	public static class MatchSessionFactory
	{
		public static IMatchSessionModel Create(Guid id, WellKnownMatchVariant variant)
		{
			switch (variant)
			{
				case WellKnownMatchVariant.Backgammon:
					return new MatchSession(id, variant, [GameModus.Backgammon]);
				case WellKnownMatchVariant.Tavla:
					return new MatchSession(id, variant, [GameModus.Tavla]);
				case WellKnownMatchVariant.Tavli:
					return new MatchSession(id, variant, [GameModus.Portes, GameModus.Plakoto, GameModus.Fevga]);
				default:
					throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown match variant.");
			}
		}
	}
}
