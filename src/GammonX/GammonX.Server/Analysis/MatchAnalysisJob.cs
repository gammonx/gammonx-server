namespace GammonX.Server.Analysis
{
	public class MatchAnalysisJob
	{
		/// <summary>
		/// Gets the id of the match to be analyzed.
		/// </summary>
		public Guid MatchId { get; }

		public MatchAnalysisJob(Guid matchId)
		{
			MatchId = matchId;
		}
	}
}
