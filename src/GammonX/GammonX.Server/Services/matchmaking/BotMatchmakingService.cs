using GammonX.Models.Enums;

using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	// <inheritdoc />
	internal sealed class BotMatchmakingService : MatchmakingServiceBaseImpl
	{
		// <inheritdoc />
        public override MatchModus Modus => MatchModus.Bot;

		public BotMatchmakingService(PlayerConnectionRepository playerConnectionRepository)
			: base(playerConnectionRepository) 
		{
			// pass
		}

		// <inheritdoc />
		public override Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey)
		{
			if (queueKey.MatchModus != MatchModus.Bot)
			{
				throw new InvalidOperationException("match modus must be of type normal in order to join this queue");
			}

			if (queueKey.BotLevel == BotLevel.Unknown || !Enum.IsDefined(queueKey.BotLevel))
			{
				throw new InvalidOperationException("bot level must be specified in order to join this queue");
			}

			var queueEntry = new QueueEntry(Guid.NewGuid(), playerId, queueKey, DateTime.UtcNow, 0);
			var matchId = Guid.NewGuid();
			var playerConnection = _playerConnectionRepository.GetOrCreate(playerId);

            var matchLobby = new MatchLobby(matchId, queueKey, playerConnection);

            AddMatchLobby(matchLobby, queueEntry);
            return Task.FromResult(queueEntry);
		}

		// <inheritdoc />
		public override Task MatchQueuedPlayersAsync()
		{
			return Task.CompletedTask;
		}
	}
}
