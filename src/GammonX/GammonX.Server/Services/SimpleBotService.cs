using GammonX.Server.Contracts;
using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	// <inheritdoc />
	public class SimpleBotService : IBotService
	{
		// <inheritdoc />
		public LegalMoveContract? GetNextMove(IMatchSessionModel matchSession)
		{
			var gameRound = matchSession.GameRound;
			var gameSession = matchSession.GetGameSession(gameRound);
			return gameSession?.LegalMovesModel?.LegalMoves?.FirstOrDefault(lm => !lm.Used);
		}
	}
}
