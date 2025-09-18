using GammonX.Engine.Models;

using GammonX.Server.Models;

namespace GammonX.Server.Bot
{
	// <inheritdoc />
	public class SimpleBotService : IBotService
	{
		// <inheritdoc />
		public Task<MoveSequenceModel> GetNextMovesAsync(IMatchSessionModel matchSession, Guid playerId)
		{
			var activeSession = matchSession.GetGameSession(matchSession.GameRound);
			if (activeSession == null)
				throw new InvalidOperationException($"No game session exists for round {matchSession.GameRound}.");

			if (activeSession.MoveSequences.CanMove)
			{
				var result = activeSession.MoveSequences.FirstOrDefault() ?? new MoveSequenceModel();
				return Task.FromResult(result);
			}

			return Task.FromResult(new MoveSequenceModel());
		}
	}
}
