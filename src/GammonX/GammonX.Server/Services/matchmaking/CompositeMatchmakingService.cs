using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	// <inheritdoc />
	internal class CompositeMatchmakingService : MatchmakingServiceBaseImpl
	{
		private readonly List<IMatchmakingService> _services;
		private readonly IServiceProvider? _serviceProvider;

		public CompositeMatchmakingService(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider;
			_services = new List<IMatchmakingService>();
			foreach (var matchModus in Enum.GetValues(typeof(WellKnownMatchModus)))
			{
				var service = serviceProvider.GetRequiredKeyedService<IMatchmakingService>(matchModus);
				_services.Add(service);
			}
		}

		internal CompositeMatchmakingService()
		{
			_serviceProvider = null;
			_services = new List<IMatchmakingService>();
		}

		// <inheritdoc />
		public override async Task<QueueEntry> JoinQueueAsync(Guid playerId, QueueKey queueKey)
		{
			var modus = queueKey.MatchModus;
			if (_serviceProvider != null)
			{
				var service = _serviceProvider.GetRequiredKeyedService<IMatchmakingService>(modus);
				return await service.JoinQueueAsync(playerId, queueKey);
			}
			throw new InvalidOperationException("Use public constructor!");

		}

		// <inheritdoc />
		public override async Task MatchQueuedPlayersAsync()
		{
			foreach (var service in _services)
			{
				await service.MatchQueuedPlayersAsync();
			}
		}

		// <inheritdoc />
		public override bool TryFindMatchLobby(Guid matchId, out MatchLobby? matchLobby)
		{
			foreach (var service in _services)
			{
				if (service.TryFindMatchLobby(matchId, out matchLobby))
				{
					return true;
				}
			}
			matchLobby = null;
			return false;
		}

		// <inheritdoc />
		public override bool TryRemoveMatchLobby(Guid matchId)
		{
			foreach (var service in _services)
			{
				if (service.TryRemoveMatchLobby(matchId))
				{
					return true;
				}
			}
			return false;
		}

		internal void SetServices(params IMatchmakingService[] services)
		{
			_services.Clear();
			_services.AddRange(services);
		}
	}
}
