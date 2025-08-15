namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="ClientId"></param>
	/// <param name="MatchVariant"></param>
	public record JoinRequest(Guid ClientId, WellKnownMatchVariant MatchVariant);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="Player"></param>
	/// <param name="MatchVariant"></param>
	/// <param name="EnqueuedAt"></param>
	public record QueueEntry(Player Player, WellKnownMatchVariant MatchVariant, DateTime EnqueuedAt);
}
