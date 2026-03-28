using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	// <inheritdoc />
	internal class UnknownMatchmakingService : MatchmakingServiceBaseImpl
	{
		public UnknownMatchmakingService(PlayerConnectionRepository playerConnectionRepository) : base(playerConnectionRepository) { }

		// <inheritdoc />
		public override Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey)
		{
			throw new InvalidOperationException("Cannot join a match for a unknown modus.");
		}

		// <inheritdoc />
		public override Task MatchQueuedPlayersAsync()
		{
			throw new InvalidOperationException("Cannot create a match for a unknown modus.");
		}
	}
}
