using GammonX.Models.Enums;

using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	// <inheritdoc />
	internal class CompositeMatchmakingService : MatchmakingServiceBaseImpl
	{
		private readonly Dictionary<MatchModus, IMatchmakingService> _services;
		private readonly IServiceProvider? _serviceProvider;

		public CompositeMatchmakingService(IServiceProvider serviceProvider, PlayerConnectionRepository playerConnectionRepository)
			: base(playerConnectionRepository)
		{
			_serviceProvider = serviceProvider;
			_services = new Dictionary<MatchModus, IMatchmakingService>();
			foreach (var matchModus in Enum.GetValues(typeof(MatchModus)))
			{
				var service = serviceProvider.GetRequiredKeyedService<IMatchmakingService>(matchModus);
				_services.Add((MatchModus)matchModus, service);
			}
		}

		internal CompositeMatchmakingService(PlayerConnectionRepository playerConnectionRepository)
			: base(playerConnectionRepository)
		{
			_serviceProvider = null;
            _services = new Dictionary<MatchModus, IMatchmakingService>();
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
			else if (_services.TryGetValue(queueKey.MatchModus, out var service))
			{
                return await service.JoinQueueAsync(playerId, queueKey);
            }
			throw new InvalidOperationException("Use public constructor!");

		}

		// <inheritdoc />
		public override async Task MatchQueuedPlayersAsync()
		{
			foreach (var service in _services)
			{
				await service.Value.MatchQueuedPlayersAsync();
			}
		}

		// <inheritdoc />
		public override bool TryFindMatchLobby(Guid matchId, out MatchLobby? matchLobby)
		{
			foreach (var service in _services)
			{
				if (service.Value.TryFindMatchLobby(matchId, out matchLobby))
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
				if (service.Value.TryRemoveMatchLobby(matchId))
				{
					return true;
				}
			}
			return false;
		}

        // <inheritdoc />
        public override MatchLobby[] GetMatchLobbies()
        {
            var lobbies = new List<MatchLobby>();
            foreach (var service in _services)
            {
                if (service.Value is IMatchmakingService impl)
                {
                    lobbies.AddRange(impl.GetMatchLobbies());
                }
            }
            return lobbies.ToArray();
        }

        // <inheritdoc />
        public override QueueEntry[] GetQueueEntries()
		{
			var entries = new List<QueueEntry>();
			foreach (var service in _services)
			{
				if (service.Value is IMatchmakingService impl)
				{
					entries.AddRange(impl.GetQueueEntries());
                }
			}
			return entries.ToArray();
        }

        protected internal override void Enqueue(QueueEntry entry)
        {
            if (_services.TryGetValue(entry.QueueKey.MatchModus, out var service) && service is MatchmakingServiceBaseImpl impl)
			{
				impl.Enqueue(entry);
            }
        }

        internal void AddService(MatchModus modus, IMatchmakingService service)
		{
			_services.Add(modus, service);
        }
    }
}
