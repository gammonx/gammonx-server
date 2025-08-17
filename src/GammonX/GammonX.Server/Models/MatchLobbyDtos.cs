namespace GammonX.Server.Models
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="PlayerId"></param>
	/// <param name="MatchVariant"></param>
	public record JoinRequest(Guid PlayerId, WellKnownMatchVariant MatchVariant);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="Player"></param>
	/// <param name="MatchVariant"></param>
	/// <param name="EnqueuedAt"></param>
	public record QueueEntry(LobbyEntry Player, WellKnownMatchVariant MatchVariant, DateTime EnqueuedAt);
}
