using GammonX.Server.Models;

using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Services
{
	public static class MatchTypeExtensions
	{
		public static Func<IMatchSessionModel, bool> GetMatchOverFunc(this MatchType type)
		{
			return type switch
			{
				MatchType.FivePointGame => (m) => m.Player1.Points >= 5 || m.Player2.Points >= 5,
				MatchType.SevenPointGame => (m) => m.Player1.Points >= 7 || m.Player2.Points >= 7,
				MatchType.CashGame => (m) => m.GetGameSessions().All(gs => gs?.Phase == GamePhase.GameOver),
				_ => throw new NotSupportedException($"The given match type '{type}' is not supported."),
			};
		}

		public static int GetMaxPoints(this MatchType type)
		{
			return type switch
			{
				MatchType.FivePointGame => 5,
				MatchType.SevenPointGame => 7,
				MatchType.CashGame => 0,
				_ => throw new NotSupportedException($"The given match type '{type}' is not supported."),
			};
		}
	}
}
