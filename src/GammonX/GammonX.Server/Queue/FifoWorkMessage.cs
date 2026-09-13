namespace GammonX.Server.Queue
{
    /// <summary>
    /// Represents a message in a FIFO (first in, first out) work queue with a group ID and deduplication ID.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <param name="Message">The message content.</param>
    /// <param name="GroupId">The group ID for the message.</param>
    /// <param name="DeduplicationId">The deduplication ID for the message.</param>
    public sealed record FifoWorkMessage<T>(T Message, string GroupId, string DeduplicationId);
}