using GammonX.Server.Contracts;
using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Provides the capablities to evaluate the next best move based on a given match/game state.
	/// </summary>
	public interface IBotService
	{
		/// <summary>
		/// Runs a game turn within the given match session.
		/// </summary>
		/// <param name="matchSession">Match session to play the bot turn.</param>
		/// <return>Returns the legal move based on the current match and game state.</return>
		LegalMoveContract? GetNextMove(IMatchSessionModel matchSession);
	}
}
