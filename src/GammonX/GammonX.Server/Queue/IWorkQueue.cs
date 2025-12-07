namespace GammonX.Server.Queue
{
    /// <summary>
    /// Provides the capability to enqueue work messages.
    /// </summary>
    public interface IWorkQueue
    {
        /// <summary>
        /// Enqueues the givne <paramref name="message"/> to the queue.
        /// </summary>
        /// <typeparam name="T">Type of message.</typeparam>
        /// <param name="message">Message to enqueue.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task to be awaited.</returns>
        Task EnqueueAsync<T>(T message, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues the given list of <paramref name="messages"/> to the queue.
        /// </summary>
        /// <remarks>
        /// Max. 10 messages per batch request. Max 256 KB per message body. A batch may partially fail.
        /// </remarks>
        /// <typeparam name="T">Type of message.</typeparam>
        /// <param name="message">Message to enqueue.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task to be awaited.</returns>
        Task EnqueueBatchAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken);
    }
}
