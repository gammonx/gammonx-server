using Amazon.SQS;
using Amazon.SQS.Model;

using GammonX.Server.Queue;

using Moq;

namespace GammonX.Server.Tests.Queue
{
    public class SqsWorkQueueTests
    {
        [Fact]
        public async Task FifoBatchCopiesOrderingAndDeduplicationMetadata()
        {
            SendMessageBatchRequest? capturedRequest = null;
            var sqs = new Mock<IAmazonSQS>();
            sqs
                .Setup(value => value.SendMessageBatchAsync(It.IsAny<SendMessageBatchRequest>(), It.IsAny<CancellationToken>()))
                .Callback<SendMessageBatchRequest, CancellationToken>((request, _) => capturedRequest = request)
                .ReturnsAsync(new SendMessageBatchResponse());
            
            var queue = new SqsWorkQueue(sqs.Object, "https://sqs.test/STATS_UPDATED_QUEUE.fifo", "STATS_UPDATED");
            
            var messages = new[]
            {
                new FifoWorkMessage<string>("first", "player-1", "match-1:player-1"),
                new FifoWorkMessage<string>("second", "player-2", "match-1:player-2")
            };

            await queue.EnqueueFifoBatchAsync(messages, CancellationToken.None);

            Assert.NotNull(capturedRequest);
            Assert.Equal("https://sqs.test/STATS_UPDATED_QUEUE.fifo", capturedRequest.QueueUrl);
            Assert.Collection(
                capturedRequest.Entries,
                entry =>
                {
                    Assert.Equal("player-1", entry.MessageGroupId);
                    Assert.Equal("match-1:player-1", entry.MessageDeduplicationId);
                },
                entry =>
                {
                    Assert.Equal("player-2", entry.MessageGroupId);
                    Assert.Equal("match-1:player-2", entry.MessageDeduplicationId);
                });
        }
    }
}