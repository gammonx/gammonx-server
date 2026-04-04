namespace GammonX.Server.Services
{
    /// <summary>
    /// Provides the capability to mmanage cancellation tokens application wide.
    /// </summary>
    public interface ICancellationTokenService
    {
        /// <summary>
        /// Stores a cancellation token source for a given key.
        /// </summary>
        /// <remarks>
        /// If a token source already exists for the given key, it is cancelled and overwritten.
        /// </remarks>
        /// <param name="key">Timer key.</param>
        /// <param name="cts">Token to cancel the timer.</param>
        void Store(CancellationTokenKey key, CancellationTokenSource cts);

        /// <summary>
        /// Tries to get a cancellation token source for a given key.
        /// </summary>
        /// <param name="key">Key to search for.</param>
        /// <param name="cts">Token result.</param>
        /// <returns>Returns true if one is found, otherwise null.</returns>
        bool TryGet(CancellationTokenKey key, out CancellationTokenSource? cts);

        /// <summary>
        /// Cancels the given token source by the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key to search for.</param>
        void Cancel(CancellationTokenKey key);

        /// <summary>
        /// Cancels all token sources associated with the given player id.
        /// </summary>
        /// <param name="playerId">Player id to search for.</param>
        /// <param name="category">Cancel token category to search for.</param>
        void CancelForPlayer(Guid playerId, CancellationTokenCategory category);

        /// <summary>
        /// Cancels all token sources associated with the given match id.
        /// </summary>
        /// <param name="matchid">Match id to search for.</param>
        /// <param name="category">Cancel token category to search for.</param>
        void CancelForMatch(Guid matchid, CancellationTokenCategory category);
    }

    public readonly struct CancellationTokenKey
    {
        public CancellationTokenCategory Category { get; init; }

        public Guid MatchId { get; init; }

        public Guid PlayerId { get; init; }
    }

    public enum CancellationTokenCategory
    {
        Turn = 0,
        Disconnect = 1,
    }
}
