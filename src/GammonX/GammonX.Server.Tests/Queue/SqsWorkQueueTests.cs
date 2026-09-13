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

        [Fact]
        public async Task BatchRetriesTransientFailuresAndOnlyResendsFailedEntries()
        {
            var requests = new List<SendMessageBatchRequest>();
            var responseNumber = 0;
            var sqs = new Mock<IAmazonSQS>();
            sqs
                .Setup(value => value.SendMessageBatchAsync(It.IsAny<SendMessageBatchRequest>(), It.IsAny<CancellationToken>()))
                .Callback<SendMessageBatchRequest, CancellationToken>((request, _) => requests.Add(request))
                .ReturnsAsync(() => responseNumber++ == 0
                    ? new SendMessageBatchResponse
                    {
                        Failed =
                        [
                            new BatchResultErrorEntry { Id = "1", SenderFault = false }
                        ]
                    }
                    : new SendMessageBatchResponse());

            var queue = new SqsWorkQueue(
                sqs.Object,
                "https://sqs.test/GAME_COMPLETED_QUEUE",
                "GAME_COMPLETED",
                maxAttempts: 2,
                retryBaseDelay: TimeSpan.Zero);

            await queue.EnqueueBatchAsync(new[] { "first", "second" }, CancellationToken.None);

            Assert.Equal(2, requests.Count);
            Assert.Equal(2, requests[0].Entries.Count);
            Assert.Collection(requests[1].Entries, entry => Assert.Equal("1", entry.Id));
        }

        [Fact]
        public async Task BatchThrowsForPermanentFailures()
        {
            var sqs = new Mock<IAmazonSQS>();
            sqs
                .Setup(value => value.SendMessageBatchAsync(It.IsAny<SendMessageBatchRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendMessageBatchResponse
                {
                    Failed =
                    [
                        new BatchResultErrorEntry { Id = "0", SenderFault = true }
                    ]
                });

            var queue = new SqsWorkQueue(
                sqs.Object,
                "https://sqs.test/GAME_COMPLETED_QUEUE",
                "GAME_COMPLETED",
                retryBaseDelay: TimeSpan.Zero);

            var exception = await Assert.ThrowsAsync<WorkQueuePublishException>(() =>
                queue.EnqueueBatchAsync(new[] { "message" }, CancellationToken.None));

            Assert.Equal("https://sqs.test/GAME_COMPLETED_QUEUE", exception.QueueUrl);
            Assert.Contains("0", exception.FailedEntryIds);
        }
    }
}