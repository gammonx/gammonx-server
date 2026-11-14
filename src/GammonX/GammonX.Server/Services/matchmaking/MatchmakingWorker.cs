using GammonX.Models.Enums;

using Serilog;

namespace GammonX.Server.Services
{
    // <inheritdoc />
    public sealed class BotMatchmakingWorker(IServiceProvider serviceProvider) 
	: MatchmakingWorker(serviceProvider, MatchModus.Bot)
	{
		// pass
	}

	// <inheritdoc />
	public sealed class RankedMatchmakingWorker : MatchmakingWorker
	{
		public RankedMatchmakingWorker(IServiceProvider serviceProvider)
			: base(serviceProvider, MatchModus.Ranked)
		{
			// pass
		}
	}

	// <inheritdoc />
	public sealed class NormalMatchmakingWorker : MatchmakingWorker
	{
		public NormalMatchmakingWorker(IServiceProvider serviceProvider) 
			: base(serviceProvider, MatchModus.Normal)
		{
			// pass
		}
	}

	// <inheritdoc />
	public abstract class MatchmakingWorker : BackgroundService
	{
		private readonly IMatchmakingService _matchmakingService;

        private readonly MatchLobbyCleanupService _lobbyCleanup;

		private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);
		
		private readonly MatchModus _modus;

		public MatchmakingWorker(IServiceProvider serviceProvider, MatchModus matchModus)
		{
			_modus = matchModus;
			_matchmakingService = serviceProvider.GetRequiredKeyedService<IMatchmakingService>(matchModus);
            _lobbyCleanup = serviceProvider.GetRequiredService<MatchLobbyCleanupService>();
		}

		// <inheritdoc />
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			Log.Logger.Information("Matchmaking Worker for '{matchModus}' started", _modus);
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					_matchmakingService.CleanupExpiredQueueEntries(TimeSpan.FromSeconds(30));
                    await _lobbyCleanup.CleanupExpiredAsync(_matchmakingService, DateTime.UtcNow);
                    await _matchmakingService.MatchQueuedPlayersAsync();
				}
				catch (Exception ex)
				{
					Log.Logger.Information("An error occurred while try to match players: {errorMessage}", ex.Message);
				}
				await Task.Delay(_interval, stoppingToken);
			}
			Log.Logger.Information("Matchmaking Worker for '{matchModus}' stopped", _modus);
		}
	}
}
