using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Services;

using MatchType = GammonX.Models.Enums.MatchType;

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

		public static IMatchSessionModel CreateMatchSessionWithPlayers(MatchVariant variant, IMatchSessionFactory factory)
		{
			var result = CreateHeadToHeadMatchSession(variant, factory);
			var session = result.Item2;
			Assert.NotNull(session);
			var player1 = CreateLobbyEntry();
			var player2 = CreateLobbyEntry();
			session.JoinSession(player1);
			session.JoinSession(player2);
			return session;
		}

		public static IMatchSessionModel CreateMatchSessionWithBot(MatchVariant variant, MatchType type, IMatchSessionFactory factory)
		{
			var result = CreateHeadToBotMatchSession(variant, type, factory);
			var session = result.Item2;
			Assert.NotNull(session);
			var player1 = CreateLobbyEntry();
			var botPlayer = new LobbyEntry(Guid.NewGuid());
			botPlayer.SetConnectionId(Guid.Empty.ToString());
			session.JoinSession(player1);
			session.JoinSession(botPlayer);
			return session;
		}

		public static IMatchSessionModel CreateMatchSessionWithTwoBots(MatchVariant variant, MatchType type, IMatchSessionFactory factory)
		{
			var result = CreateHeadToBotMatchSession(variant, type, factory);
			var session = result.Item2;
			Assert.NotNull(session);
			var botPlayer2 = new LobbyEntry(Guid.NewGuid());
			var botPlayer1 = new LobbyEntry(Guid.NewGuid());
			botPlayer1.SetConnectionId(Guid.Empty.ToString());
			botPlayer2.SetConnectionId(Guid.Empty.ToString());
			session.JoinSession(botPlayer2);
			session.JoinSession(botPlayer1);
			return session;
		}

		public static (Guid, IMatchSessionModel) CreateHeadToHeadMatchSession(MatchVariant variant, IMatchSessionFactory factory)
		{
			var matchId = Guid.NewGuid();
			var queueKey = new QueueKey(variant, MatchModus.Normal, MatchType.CashGame);
			var session = factory.Create(matchId, queueKey);
			return ( matchId, session );
		}

		public static (Guid, IMatchSessionModel) CreateHeadToBotMatchSession(MatchVariant variant, MatchType type, IMatchSessionFactory factory)
		{
			var matchId = Guid.NewGuid();
			var queueKey = new QueueKey(variant, MatchModus.Bot, type);
			var session = factory.Create(matchId, queueKey);
			return (matchId, session);
		}

		public static LobbyEntry CreateLobbyEntry()
		{
			var entry = new LobbyEntry(Guid.NewGuid());
			entry.SetConnectionId(Guid.NewGuid().ToString());
			return entry;
		}
	}
}
