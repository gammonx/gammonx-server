using GammonX.Server.EntityFramework.Entities;
using GammonX.Server.EntityFramework.Services;
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
		private readonly IPlayerService _playerService;
		private readonly MatchSessionRepository _matchRepository;

		public MatchAnalysisService(IPlayerService playerService, MatchSessionRepository matchRepository)
		{
			_playerService = playerService;
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

			var player1 = await _playerService.GetFull(matchHistory.Player1, cancellationToken);
			var player2 = await _playerService.GetFull(matchHistory.Player2, cancellationToken);
			if (player1 == null || player2 == null)
			{
				throw new InvalidOperationException($"An error occurred while analyzing match with ID '{match.Id}'. One of the player profiles were not found");
			}

			await AnalyzeAndStoreStatsAsync(match, player1, player2, cancellationToken);
			await AnalyzeAndStoreStatsAsync(match, player2, player1, cancellationToken);

			// TODO
			// won matches
			// lost matches
			// won games
			// lost games
			// stats for modus/type/variant
			// rating for modus/type/variant
		}

		private async Task AnalyzeAndStoreStatsAsync(IMatchSessionModel match, Player player, Player opponent, CancellationToken cancellationToken)
		{
			await _playerService.UpdateAsync(player, cancellationToken);
		}
	}
}
