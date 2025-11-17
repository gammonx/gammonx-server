using GammonX.Server.Models;
using GammonX.Server.Services;

namespace GammonX.Server.Analysis
{
	/// <summary>
	/// Provides the capabilities to analyze and store match related data for the given players.
	/// </summary>
	public interface IMatchAnalysisService
	{
		/// <summary>
		/// Analyzes the match with the given <paramref name="matchId"/> and stores its data for
		/// the participating players.
		/// </summary>
		/// <param name="matchId">Id of the match.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		/// <returns>A task to be awaited.</returns>
		Task AnalyzeAndStoreAsync(Guid matchId, CancellationToken cancellationToken);
	}

	// <inheritdoc />
	public class MatchAnalysisService : IMatchAnalysisService
	{
		private readonly MatchSessionRepository _matchRepository;

		public MatchAnalysisService(MatchSessionRepository matchRepository)
		{
			_matchRepository = matchRepository;
		}

		// <inheritdoc />
		public async Task AnalyzeAndStoreAsync(Guid matchId, CancellationToken cancellationToken)
		{
			var match = _matchRepository.Get(matchId);
			_matchRepository.Remove(matchId);

			if (match == null)
			{
				throw new InvalidOperationException($"Match with the id '{matchId}' does not exist anymore.");
			}

			var matchHistory = match.GetHistory();
			var gameHistories = matchHistory.Games;
			// TODO: trigger AWS queue for stat calculation
			await AnalyzeAndStoreStatsAsync(match, cancellationToken);
			await AnalyzeAndStoreStatsAsync(match, cancellationToken);

		}

		private static Task AnalyzeAndStoreStatsAsync(IMatchSessionModel match, CancellationToken cancellationToken)
		{
			// TODO
			// won matches
			// lost matches
			// won games
			// lost games
			// stats for modus/type/variant
			// rating for modus/type/variant
			//await _playerService.UpdateAsync(player, cancellationToken);
			return Task.CompletedTask;
		}
	}
}
