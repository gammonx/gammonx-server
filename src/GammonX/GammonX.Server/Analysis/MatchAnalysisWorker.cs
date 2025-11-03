using Serilog;

namespace GammonX.Server.Analysis
{
	// <inheritdoc />
	public class MatchAnalysisWorker : BackgroundService
	{
		private readonly IMatchAnalysisQueue _queue;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly TimeSpan _interval = TimeSpan.FromSeconds(0.5);

		public MatchAnalysisWorker(IMatchAnalysisQueue queue, IServiceScopeFactory scopeFactory)
		{
			_queue = queue;
			_scopeFactory = scopeFactory;
		}

		// <inheritdoc />
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			Log.Logger.Information("Matchmaking analysis worker started.");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var job = await _queue.DequeueAsync(stoppingToken);
					if (job == null)
						continue;

					using var scope = _scopeFactory.CreateScope();
					var analysisService = scope.ServiceProvider.GetRequiredService<IMatchAnalysisService>();
					await analysisService.AnalyzeAndStoreAsync(job.MatchId, stoppingToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					Log.Logger.Information("An error occurred while analyzing a match: {errorMessage}", ex.Message);
					continue;
				}
				await Task.Delay(_interval, stoppingToken);
			}

			Log.Logger.Information("Matchmaking analysis worker stopped.");
		}
	}
}
