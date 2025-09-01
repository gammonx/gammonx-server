using GammonX.Engine.Services;

using GammonX.Server.Models;
using GammonX.Server.Services;

namespace GammonX.Server.Tests.Utils
{
	internal static class SessionUtils
	{
		public static void InjectDiceServiceMock(this IGameSessionModel model, IDiceService diceService)
		{
			if (model is GameSessionImpl impl)
			{
				impl.SetDiceService(diceService);
			}
		}

		public static IMatchSessionModel CreateMatchSessionWithPlayers(WellKnownMatchVariant variant, IMatchSessionFactory factory)
		{
			var result = CreateMatchSession(variant, factory);
			var session = result.Session as IMatchSessionModel;
			Assert.NotNull(session);
			var player1 = CreateLobbyEntry();
			var player2 = CreateLobbyEntry();
			session.JoinSession(player1);
			session.JoinSession(player2);
			return session;
		}

		public static dynamic CreateMatchSession(WellKnownMatchVariant variant, IMatchSessionFactory factory)
		{
			var matchId = Guid.NewGuid();
			var queueKey = new QueueKey(variant, WellKnownMatchType.Normal);
			var session = factory.Create(matchId, queueKey);
			return new { MatchId = matchId, Session = session };
		}

		public static LobbyEntry CreateLobbyEntry()
		{
			var entry = new LobbyEntry(Guid.NewGuid());
			entry.SetConnectionId(Guid.NewGuid().ToString());
			return entry;
		}
	}
}
