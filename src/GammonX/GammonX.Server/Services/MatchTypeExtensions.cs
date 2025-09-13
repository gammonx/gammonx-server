using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	public static class MatchTypeExtensions
	{
		public static Func<IMatchSessionModel, bool> GetMatchOverFunc(this WellKnownMatchType type)
		{
			return type switch
			{
				WellKnownMatchType.FivePointGame => (m) => m.Player1.Points >= 5 || m.Player2.Points >= 5,
				WellKnownMatchType.SevenPointGame => (m) => m.Player1.Points >= 7 || m.Player2.Points >= 7,
				WellKnownMatchType.CashGame => (m) => m.GetGameSessions().All(gs => gs?.Phase == GamePhase.GameOver),
				_ => throw new NotSupportedException($"The given match type '{type}' is not supported."),
			};
		}

		public static int GetMaxPoints(this WellKnownMatchType type)
		{
			return type switch
			{
				WellKnownMatchType.FivePointGame => 5,
				WellKnownMatchType.SevenPointGame => 7,
				WellKnownMatchType.CashGame => 0,
				_ => throw new NotSupportedException($"The given match type '{type}' is not supported."),
			};
		}
	}
}
