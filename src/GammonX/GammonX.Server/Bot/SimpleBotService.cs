using GammonX.Server.Contracts;
using GammonX.Server.Models;

namespace GammonX.Server.Bot
{
	// <inheritdoc />
	public class SimpleBotService : IBotService
	{
		// <inheritdoc />
		public Task<LegalMoveContract[]> GetNextMovesAsync(IMatchSessionModel matchSession, Guid playerId)
		{
			var activeSession = matchSession.GetGameSession(matchSession.GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

			var anyMove = activeSession?.LegalMovesModel?.LegalMoves?.FirstOrDefault(lm => !lm.Used);
			if (anyMove != null)
			{
				return Task.FromResult(new LegalMoveContract[] { anyMove });
			}

			return Task.FromResult(Array.Empty<LegalMoveContract>());
		}
	}
}
