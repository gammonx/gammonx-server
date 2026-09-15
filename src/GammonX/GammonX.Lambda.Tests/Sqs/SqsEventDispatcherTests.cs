using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;

using GammonX.Lambda.Handlers;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace GammonX.Lambda.Tests;

public class SqsEventDispatcherTests
{
    [Fact]
    public async Task SharedSqsBatchRoutesMessagesByEventTypeAndAggregatesFailures()
    {
        var matchHandler = new RecordingSqsHandler();
        var statsHandler = new RecordingSqsHandler(["stats-2"]);
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ISqsLambdaHandler>(LambdaFunctions.MatchCompletedFunc, matchHandler);
        services.AddKeyedSingleton<ISqsLambdaHandler>(LambdaFunctions.PlayerStatsUpdatedFunc, statsHandler);

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                CreateMessage("match-1", LambdaFunctions.MatchCompletedFunc),
                CreateMessage("stats-1", LambdaFunctions.PlayerStatsUpdatedFunc),
                CreateMessage("stats-2", LambdaFunctions.PlayerStatsUpdatedFunc),
                CreateMessage("missing-event-type", null)
            ]
        };

        var response = await Program.HandleSqsEventAsync(
            new TestLambdaContext { Logger = new TestLambdaLogger() },
            services.BuildServiceProvider(),
            sqsEvent);

        Assert.Equal(["match-1"], matchHandler.MessageIds);
        Assert.Equal(["stats-1", "stats-2"], statsHandler.MessageIds);
        Assert.Equal(
            ["stats-2", "missing-event-type"],
            response.BatchItemFailures.Select(failure => failure.ItemIdentifier));
    }

    private static SQSEvent.SQSMessage CreateMessage(string messageId, string? eventType)
    {
        var message = new SQSEvent.SQSMessage
        {
            MessageId = messageId,
            Body = messageId,
            MessageAttributes = []
        };

        if (eventType != null)
        {
            message.MessageAttributes["EVENT_TYPE"] = new()
            {
                DataType = "String",
                StringValue = eventType
            };
        }

        return message;
    }

    private sealed class RecordingSqsHandler : ISqsLambdaHandler
    {
        private readonly HashSet<string> _failedMessageIds;

        public RecordingSqsHandler(IEnumerable<string>? failedMessageIds = null)
        {
            _failedMessageIds = failedMessageIds?.ToHashSet(StringComparer.Ordinal) ?? [];
        }

        public List<string> MessageIds { get; } = [];

        public Task<SQSBatchResponse> HandleAsync(SQSEvent @event, ILambdaContext context)
        {
            MessageIds.AddRange(@event.Records.Select(record => record.MessageId));
            return Task.FromResult(
                new SQSBatchResponse(
                    @event.Records
                        .Where(record => _failedMessageIds.Contains(record.MessageId))
                        .Select(record => new SQSBatchResponse.BatchItemFailure
                        {
                            ItemIdentifier = record.MessageId
                        })
                        .ToList()));
        }
    }
}