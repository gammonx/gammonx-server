namespace GammonX.Server.Models
{
	/// <summary>
	/// REST Join request for a match lobby.
	/// </summary>
	/// <param name="PlayerId">Player who joins the matchmaking process.</param>
	/// <param name="MatchVariant">Match queue to join.</param>
	public record JoinRequest(Guid PlayerId, WellKnownMatchVariant MatchVariant);

	/// <summary>
	/// Internal queue entry for a player in the matchmaking queue.
	/// </summary>
	/// <param name="Player">Player of the queue entry.</param>
	/// <param name="MatchVariant">Match variant.</param>
	/// <param name="EnqueuedAt">Time of enqueuing.</param>
	public record QueueEntry(LobbyEntry Player, WellKnownMatchVariant MatchVariant, DateTime EnqueuedAt);
}
