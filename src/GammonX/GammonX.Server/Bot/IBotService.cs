using GammonX.Server.Contracts;
using GammonX.Server.Models;

namespace GammonX.Server.Bot
{
	/// <summary>
	/// Provides the capablities to evaluate the next best move based on a given match/game state.
	/// </summary>
	public interface IBotService
	{
		/// <summary>
		/// Runs a game turn within the given match session for the bot player.
		/// </summary>
		/// <remarks>
		/// The bot always plays with the black checkers (<c>isWhite = true</c>).
		/// </remarks>
		/// <param name="matchSession">Match session to play the bot turn.</param>
		/// <param name="playerId">Id of the player determining whether black or white is playing.</param>
		/// <return>Returns a list of legal move based on the current match and game state.</return>
		Task<LegalMoveContract[]> GetNextMovesAsync(IMatchSessionModel matchSession, Guid playerId);
	}
}
