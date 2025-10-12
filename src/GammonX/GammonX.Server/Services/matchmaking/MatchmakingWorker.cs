﻿using GammonX.Server.Models;
using GammonX.Server.Services;
using Serilog;

namespace GammonX.Server.Services.matchmaking
{
	// <inheritdoc />
	public sealed class RankedMatchmakingWorker : MatchmakingWorker
	{
		public RankedMatchmakingWorker(IServiceProvider serviceProvider)
			: base(serviceProvider, WellKnownMatchModus.Ranked)
		{
		}
	}

	// <inheritdoc />
	public sealed class NormalMatchmakingWorker : MatchmakingWorker
	{
		public NormalMatchmakingWorker(IServiceProvider serviceProvider) 
			: base(serviceProvider, WellKnownMatchModus.Normal)
		{
		}
	}

	// <inheritdoc />
	public abstract class MatchmakingWorker : BackgroundService
	{
		private readonly IMatchmakingService _matchmakingService;
		private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);
		private readonly WellKnownMatchModus _modus;

		public MatchmakingWorker(IServiceProvider serviceProvider, WellKnownMatchModus matchModus)
		{
			_modus = matchModus;
			_matchmakingService = serviceProvider.GetRequiredKeyedService<IMatchmakingService>(matchModus);
		}

		// <inheritdoc />
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			Log.Logger.Information("Matchmaking Worker for '{matchModus}' started", _modus);
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await _matchmakingService.MatchQueuedPlayersAsync();
				}
				catch (Exception ex)
				{
					Log.Logger.Information("An error occurred while try to match players: {errorMessage}", ex.Message);
					continue;
				}
				await Task.Delay(_interval, stoppingToken);
			}
			Log.Logger.Information("Matchmaking Worker for '{matchModus}' stopped", _modus);
		}
	}
}
