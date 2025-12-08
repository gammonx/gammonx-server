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

        public SqsWorkQueue(IAmazonSQS sqs, string queueUrl)
        {
            _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));
            _queueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
        }

        // <inheritdoc />
        public async Task EnqueueAsync<T>(T message, CancellationToken cancellationToken)
        {
            var body = JsonConvert.SerializeObject(message);

            var request = new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = body
            };

            await _sqs.SendMessageAsync(request, cancellationToken);
        }

        // <inheritdoc />
        public async Task EnqueueBatchAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken)
        {
            var entries = messages
                .Select((msg, index) => new SendMessageBatchRequestEntry
                {
                    Id = index.ToString(),
                    MessageBody = JsonConvert.SerializeObject(msg)
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
