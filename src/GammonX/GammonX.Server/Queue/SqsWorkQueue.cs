using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Runtime;

using System.Net;

using Newtonsoft.Json;

namespace GammonX.Server.Queue
{
    // <inheritdoc />
    public class SqsWorkQueue : IWorkQueue
    {
        private const int DefaultMaxAttempts = 3;
        private const int DefaultRetryBaseDelayMilliseconds = 100;

        private readonly IAmazonSQS _sqs;

        private readonly string _queueUrl;

        private readonly string _eventType;

        private readonly int _maxAttempts;

        private readonly TimeSpan _retryBaseDelay;

        public SqsWorkQueue(
            IAmazonSQS sqs,
            string queueUrl,
            string eventType,
            int maxAttempts = DefaultMaxAttempts,
            TimeSpan? retryBaseDelay = null)
        {
            _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));
            _queueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
            _eventType = eventType ?? throw new ArgumentNullException(nameof(eventType));

            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "At least one send attempt is required.");
            }

            _maxAttempts = maxAttempts;
            _retryBaseDelay = retryBaseDelay ?? TimeSpan.FromMilliseconds(DefaultRetryBaseDelayMilliseconds);

            if (_retryBaseDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(retryBaseDelay), "The retry delay cannot be negative.");
            }
        }

        private Dictionary<string, MessageAttributeValue> EventTypeAttribute() =>
            new()
            {
                ["EVENT_TYPE"] = new MessageAttributeValue { DataType = "String", StringValue = _eventType }
            };

        // <inheritdoc />
        public async Task EnqueueAsync<T>(T message, CancellationToken cancellationToken)
        {
            var request = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = JsonConvert.SerializeObject(message),
                MessageAttributes = EventTypeAttribute()
            };

            await ExecuteWithRetryAsync(() => _sqs.SendMessageAsync(request, cancellationToken), cancellationToken);
        }

        // <inheritdoc />
        public async Task EnqueueBatchAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken)
        {
            var attrs = EventTypeAttribute();
            var entries = messages
                .Select((msg, index) => new SendMessageBatchRequestEntry
                {
                    Id = index.ToString(),
                    MessageBody = JsonConvert.SerializeObject(msg),
                    MessageAttributes = attrs
                })
                .ToList();

            await SendBatchWithRetryAsync(entries, cancellationToken);
        }

        // <inheritdoc />
        public async Task EnqueueFifoBatchAsync<T>(IEnumerable<FifoWorkMessage<T>> messages, CancellationToken cancellationToken)
        {
            var attrs = EventTypeAttribute();
            var entries = messages
                .Select((message, index) => new SendMessageBatchRequestEntry
                {
                    Id = index.ToString(),
                    MessageBody = JsonConvert.SerializeObject(message.Message),
                    MessageAttributes = attrs,
                    MessageGroupId = message.GroupId,
                    MessageDeduplicationId = message.DeduplicationId
                })
                .ToList();

            await SendBatchWithRetryAsync(entries, cancellationToken);
        }

        private async Task SendBatchWithRetryAsync(
            IReadOnlyCollection<SendMessageBatchRequestEntry> entries,
            CancellationToken cancellationToken)
        {
            var pendingEntries = entries.ToList();
            var permanentFailureIds = new HashSet<string>(StringComparer.Ordinal);

            for (var attempt = 1; ; attempt++)
            {
                SendMessageBatchResponse response;
                try
                {
                    response = await _sqs.SendMessageBatchAsync(new SendMessageBatchRequest
                    {
                        QueueUrl = _queueUrl,
                        Entries = pendingEntries
                    }, cancellationToken);
                }
                catch (Exception ex) when (IsTransient(ex) && attempt < _maxAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                    continue;
                }

                var failures = response.Failed ?? [];

                if (failures.Count == 0)
                {
                    if (permanentFailureIds.Count == 0)
                    {
                        return;
                    }

                    throw new WorkQueuePublishException(_queueUrl, permanentFailureIds);
                }

                var pendingById = pendingEntries.ToDictionary(entry => entry.Id, StringComparer.Ordinal);
                var retryEntries = new List<SendMessageBatchRequestEntry>();
                
                foreach (var failure in failures)
                {
                    var failureId = failure.Id ?? string.Empty;
                    if (failure.SenderFault == true)
                    {
                        permanentFailureIds.Add(failureId);
                    }
                    else if (!pendingById.TryGetValue(failureId, out var failedEntry))
                    {
                        permanentFailureIds.Add(failureId);
                    }
                    else
                    {
                        retryEntries.Add(failedEntry);
                    }
                }

                if (retryEntries.Count == 0)
                {
                    throw new WorkQueuePublishException(_queueUrl, permanentFailureIds.Concat(failures.Select(failure => failure.Id ?? string.Empty)));
                }

                if (attempt >= _maxAttempts)
                {
                    throw new WorkQueuePublishException(
                        _queueUrl,
                        permanentFailureIds.Concat(retryEntries.Select(entry => entry.Id)));
                }

                pendingEntries = retryEntries;
                await DelayBeforeRetryAsync(attempt, cancellationToken);
            }
        }

        private async Task ExecuteWithRetryAsync(Func<Task> operation, CancellationToken cancellationToken)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await operation();
                    return;
                }
                catch (Exception ex) when (IsTransient(ex) && attempt < _maxAttempts)
                {
                    await DelayBeforeRetryAsync(attempt, cancellationToken);
                }
            }
        }

        private async Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
        {
            if (_retryBaseDelay == TimeSpan.Zero)
            {
                return;
            }

            var delay = TimeSpan.FromMilliseconds(_retryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
            await Task.Delay(delay, cancellationToken);
        }

        private static bool IsTransient(Exception exception)
        {
            if (exception is TimeoutException or IOException)
            {
                return true;
            }

            if (exception is not AmazonServiceException serviceException)
            {
                return false;
            }

            var statusCode = (int)serviceException.StatusCode;
            return statusCode == (int)HttpStatusCode.RequestTimeout
                || statusCode == (int)HttpStatusCode.TooManyRequests
                || statusCode >= 500
                || serviceException.ErrorCode is "RequestThrottled" or "ThrottlingException";
        }
    }
}
