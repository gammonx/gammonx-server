namespace GammonX.Server.Models
{
	/// <summary>
	/// REST Join request for a match lobby.
	/// </summary>
	/// <param name="PlayerId">Player who joins the matchmaking process.</param>
	/// <param name="MatchVariant">Match queue to join.</param>
	/// <param name="MatchModus">Match modus to join.</param>
	/// <param name="MatchType">Match type to play.</param>
	public record JoinRequest(Guid PlayerId, WellKnownMatchVariant MatchVariant, WellKnownMatchModus MatchModus, WellKnownMatchType MatchType);

	/// <summary>
	/// Internal queue entry for a player in the matchmaking queue.
	/// </summary>
	/// <param name="Player">Player of the queue entry.</param>
	/// <param name="QueueKey">Constructed key of the queue entry.</param>
	/// <param name="EnqueuedAt">Time of enqueuing.</param>
	public record QueueEntry(LobbyEntry Player, QueueKey QueueKey, DateTime EnqueuedAt);

	/// <summary>
	/// Unique ID of a queue entry.
	/// </summary>
	/// <param name="MatchVariant">Match variant.</param>
	/// <param name="MatchModus">Match type.</param>
	/// <param name="MatchType">Match type to play.</param>
	public record QueueKey(WellKnownMatchVariant MatchVariant, WellKnownMatchModus MatchModus, WellKnownMatchType MatchType);
}
