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
					throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown match variant.");
				case WellKnownMatchVariant.Tavla:
					throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown match variant.");
				case WellKnownMatchVariant.Tavli:
					return new TavliMatchSession(id);
				default:
					throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown match variant.");
			}
		}
	}
}
