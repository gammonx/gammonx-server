using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	/// <summary>
	/// 
	/// </summary>
	public static class MatchSessionFactory
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="variant"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
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
