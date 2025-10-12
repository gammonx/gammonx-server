namespace GammonX.Server.Analysis
{
	/// <summary>
	/// Provides the capabilities to enqueue and dequeue match analysis jobs.
	/// </summary>
	public interface IMatchAnalysisQueue
	{
		/// <summary>
		/// Enqeues the given <paramref name="job"/> to the analysis queue.
		/// </summary>
		/// <param name="job">Job to analyze.</param>
		/// <returns>Returns a value task.</returns>
		ValueTask EnqueueAsync(MatchAnalysisJob job);

		/// <summary>
		/// Dequeues the next job in the list to be analyzed.
		/// </summary>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>Returns a value task containing the match analysis job.</returns>
		ValueTask<MatchAnalysisJob?> DequeueAsync(CancellationToken cancellationToken);
	}
}
