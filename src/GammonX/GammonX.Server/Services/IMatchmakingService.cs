using GammonX.Server.Models;

namespace GammonX.Server.Services
{
	/// <summary>
	/// Provides the capability to join players into a match lobby.
	/// </summary>
	public interface IMatchmakingService
	{
		/// <summary>
		/// Tries to find a match lobby by its ID. Returns null if not found.
		/// </summary>
		/// <param name="matchId">Match id to search for.</param>
		/// <param name="matchLobby">Result.</param>
		/// <returns>Boolean indicating if a match lobby was found.</returns>
		bool TryFindMatchLobby(Guid matchId, out MatchLobby? matchLobby);

		/// <summary>
		/// Removes a match lobby with the given <paramref name="matchId"/>.
		/// </summary>
		/// <param name="matchId">Id of the match lobby.</param>
		/// <returns>Returns <c>true</c> if the match lobby was removed. Otherwise returns <c>false</c>.</returns>
		bool TryRemoveMatchLobby(Guid matchId);

		/// <summary>
		/// Join the given <paramref name="newPlayer"/> to the matchmaking queue.
		/// </summary>
		/// <param name="newPlayer">Player to join the queue.</param>
		/// <returns>A guid of the created match lobby.</returns>
		Guid JoinQueue(LobbyEntry newPlayer, WellKnownMatchVariant matchVariant);
	}
}
