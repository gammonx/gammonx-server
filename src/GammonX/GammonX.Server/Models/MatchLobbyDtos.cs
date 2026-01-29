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
	/// Unique ID of a queue entry.
	/// </summary>
	/// <param name="MatchVariant">Match variant.</param>
	/// <param name="MatchModus">Match type.</param>
	/// <param name="MatchType">Match type to play.</param>
	public record QueueKey(MatchVariant MatchVariant, MatchModus MatchModus, MatchType MatchType);

    /// <summary>
    /// Internal queue entry for a player in the matchmaking queue.
    /// </summary>
    public sealed class QueueEntry
    {
        // volatile-like semantics via Interlocked
        private long _lastSeenTicks;

        /// <summary>
        /// Gets the id of the queue entry.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the player id of the enqueued player.
        /// </summary>
        public Guid PlayerId { get; }

        /// <summary>
        /// Gets the queue key defining the variant, mode and type of the queue entry.
        /// </summary>
        public QueueKey QueueKey { get; }

        /// <summary>
        /// Gets the date time in utc when the entry was enqueued.
        /// </summary>
        public DateTime EnqueuedAtUtc { get; internal set; }

        /// <summary>
        /// Gets the current ranked rating of the player for the given variant.
        /// Only relevant for ranked queues. In other cases, this value is 0.
        /// </summary>
        public double CurrentRating { get; } = 0;

        /// <summary>
        /// Gets the date time in utc when the entry was last seen (polled by the client).
        /// If the last seen time is too old, the entry can be considered stale and removed from the queue.
        /// </summary>
        public DateTime LastSeenUtc
        {
            get => new(Interlocked.Read(ref _lastSeenTicks), DateTimeKind.Utc);
            set => Interlocked.Exchange(ref _lastSeenTicks, value.Ticks);
        }

        /// <summary>
        /// Creates a new queue entry.
        /// </summary>
        /// <param name="id">Id of the queue entry.</param>
        /// <param name="playerId">Player of the queue entry.</param>
        /// <param name="queueKey">Constructed key of the queue entry.</param>
        /// <param name="enqueuedAtUtc">Time of enqueuing.</param>
        /// <param name="currentRating">Ranked rating for the given variant.</param>
        public QueueEntry(Guid id, Guid playerId, QueueKey queueKey, DateTime enqueuedAtUtc, double currentRating)
        {
            Id = id;
            PlayerId = playerId;
            QueueKey = queueKey;
            EnqueuedAtUtc = enqueuedAtUtc;
            CurrentRating = currentRating;
            LastSeenUtc = DateTime.UtcNow;
        }
    }
}
