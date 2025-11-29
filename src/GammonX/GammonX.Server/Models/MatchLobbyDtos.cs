using GammonX.Models.Enums;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Models
{
	/// <summary>
	/// REST Join request for a match lobby.
	/// </summary>
	/// <param name="PlayerId">Player who joins the matchmaking process.</param>
	/// <param name="MatchVariant">Match queue to join.</param>
	/// <param name="MatchModus">Match modus to join.</param>
	/// <param name="MatchType">Match type to play.</param>
	public record JoinRequest(Guid PlayerId, MatchVariant MatchVariant, MatchModus MatchModus, MatchType MatchType);

	/// <summary>
	/// REST Status match lobby request.
	/// </summary>
	/// <param name="PlayerId">Player who requests the match lobby status.</param>
	/// <param name="MatchModus">Match modus which determines the queue type.</param>
	public record StatusRequest(Guid PlayerId, MatchModus MatchModus);

	/// <summary>
	/// Internal queue entry for a player in the matchmaking queue.
	/// </summary>
	/// <param name="Id">Id of the queue entry.</param>
	/// <param name="PlayerId">Player of the queue entry.</param>
	/// <param name="QueueKey">Constructed key of the queue entry.</param>
	/// <param name="EnqueuedAt">Time of enqueuing.</param>
	/// <param name="CurrentRating">Ranked rating for the given variant.</param>
	public record QueueEntry(Guid Id, Guid PlayerId, QueueKey QueueKey, DateTime EnqueuedAt, double CurrentRating);

	/// <summary>
	/// Unique ID of a queue entry.
	/// </summary>
	/// <param name="MatchVariant">Match variant.</param>
	/// <param name="MatchModus">Match type.</param>
	/// <param name="MatchType">Match type to play.</param>
	public record QueueKey(MatchVariant MatchVariant, MatchModus MatchModus, MatchType MatchType);
}
