namespace GammonX.Server.Queue
{
    /// <summary>
    /// Represents an exception that occurs when publishing messages to a work queue fails.
    /// </summary>
    public sealed class WorkQueuePublishException : Exception
    {
        public WorkQueuePublishException(string queueUrl, IEnumerable<string> failedEntryIds, Exception? innerException = null)
            : base($"Failed to publish one or more messages to queue '{queueUrl}'.", innerException)
        {
            QueueUrl = queueUrl;
            FailedEntryIds = failedEntryIds.Distinct(StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        /// Gets the queue url which the failed messages were attempted to be published to.
        /// </summary>
        public string QueueUrl { get; }

        /// <summary>
        /// Gets the entry ids of the messages that failed to be published.
        /// </summary>
        public IReadOnlyCollection<string> FailedEntryIds { get; }
    }
}