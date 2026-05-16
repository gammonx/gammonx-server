using Amazon.SQS;
using Amazon.SQS.Model;

using Newtonsoft.Json;

namespace GammonX.Server.Queue
{
    // <inheritdoc />
    public class SqsWorkQueue : IWorkQueue
    {
        private readonly IAmazonSQS _sqs;
        private readonly string _queueUrl;
        private readonly string _eventType;

        public SqsWorkQueue(IAmazonSQS sqs, string queueUrl, string eventType)
        {
            _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));
            _queueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
            _eventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
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

            await _sqs.SendMessageAsync(request, cancellationToken);
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

            var batchRequest = new SendMessageBatchRequest
            {
                QueueUrl = _queueUrl,
                Entries = entries
            };

            await _sqs.SendMessageBatchAsync(batchRequest, cancellationToken);
        }
    }
}
