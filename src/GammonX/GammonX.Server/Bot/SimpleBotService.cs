using GammonX.Engine.Models;

using GammonX.Server.Models;

namespace GammonX.Server.Bot
{
	// <inheritdoc />
	public class SimpleBotService : IBotService
	{
		// <inheritdoc />
		public Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(true);
		}

		// <inheritdoc />
		public Task<MoveSequenceModel> GetNextMovesAsync(
			IMatchSessionModel matchSession,
			Guid playerId,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var activeSession = matchSession.GetGameSession(matchSession.GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

			var moves = activeSession.GetLegalMoveSequences(matchSession.Player1.Id == playerId);
			return Task.FromResult(moves.FirstOrDefault() ?? new MoveSequenceModel());
		}

		// <inheritdoc />
		public Task<bool> ShouldTakeDouble(
			IMatchSessionModel matchSession,
			Guid playerId,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(false);
		}

		// <inheritdoc />
		public Task<bool> ShouldOfferDouble(
			IMatchSessionModel matchSession,
			Guid playerId,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(false);
		}
	}
}
