using System.Collections.Concurrent;

namespace GammonX.Server.Analysis
{
	// <inheritdoc />
	public class MatchAnalysisQueue : IMatchAnalysisQueue
	{
		private readonly ConcurrentQueue<MatchAnalysisJob> _jobs = new();
		private readonly SemaphoreSlim _signal = new(0);

		// <inheritdoc />
		public ValueTask EnqueueAsync(MatchAnalysisJob job)
		{
			_jobs.Enqueue(job);
			_signal.Release();
			return ValueTask.CompletedTask;
		}

		// <inheritdoc />
		public async ValueTask<MatchAnalysisJob?> DequeueAsync(CancellationToken cancellationToken)
		{
			await _signal.WaitAsync(cancellationToken);
			_jobs.TryDequeue(out var job);
			return job;
		}
	}
}
