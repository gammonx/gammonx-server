using GammonX.Models.Enums;

using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	// <inheritdoc />
	internal class BotMatchmakingService : MatchmakingServiceBaseImpl
	{
		// <inheritdoc />
		public override Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey)
		{
			if (queueKey.MatchModus != MatchModus.Bot)
			{
				throw new InvalidOperationException("match modus must be of type normal in order to join this queue");
			}

			var queueEntry = new QueueEntry(Guid.NewGuid(), playerId, queueKey, DateTime.Now, 0);
			var matchId = Guid.NewGuid();
			var matchLobby = new MatchLobby(matchId, queueKey, new LobbyEntry(playerId));
			if (_matchLobbies.TryAdd(queueEntry, matchLobby))
			{
				return Task.FromResult(queueEntry);
			}
			throw new InvalidOperationException("An error occurred while creating the match lobby");
		}

		// <inheritdoc />
		public override Task MatchQueuedPlayersAsync()
		{
			return Task.CompletedTask;
		}
	}
}
