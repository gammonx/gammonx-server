namespace GammonX.Server.Models
{
    /// <summary>
    /// Provides the capabilities to acquire exclusive access to a shared state asynchronously.
    /// </summary>
    public interface IAsyncStateMutex
    {
        /// <summary>
        /// Acquires exclusive access to the state.
        /// </summary>
        Task<IAsyncDisposable> AcquireStateAsync();
    }
}
