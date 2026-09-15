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
        var invocationOrder = new List<string>();
        var matchHandler = new RecordingSqsHandler("match", invocationOrder);
        var gameHandler = new RecordingSqsHandler("game", invocationOrder);
        var statsHandler = new RecordingSqsHandler("stats", invocationOrder, ["stats-2"]);
        var ratingHandler = new RecordingSqsHandler("rating", invocationOrder);
        var services = new ServiceCollection();
        services.AddKeyedSingleton<ISqsLambdaHandler>(LambdaFunctions.MatchCompletedFunc, matchHandler);
        services.AddKeyedSingleton<ISqsLambdaHandler>(LambdaFunctions.GameCompletedFunc, gameHandler);
        services.AddKeyedSingleton<ISqsLambdaHandler>(LambdaFunctions.PlayerStatsUpdatedFunc, statsHandler);
        services.AddKeyedSingleton<ISqsLambdaHandler>(LambdaFunctions.PlayerRatingUpdatedFunc, ratingHandler);

        var sqsEvent = new SQSEvent
        {
            Records =
            [
                CreateMessage("stats-1", LambdaFunctions.PlayerStatsUpdatedFunc),
                CreateMessage("stats-2", LambdaFunctions.PlayerStatsUpdatedFunc),
                CreateMessage("rating-1", LambdaFunctions.PlayerRatingUpdatedFunc),
                CreateMessage("game-1", LambdaFunctions.GameCompletedFunc),
                CreateMessage("match-1", LambdaFunctions.MatchCompletedFunc),
                CreateMessage("missing-event-type", null)
            ]
        };

        var response = await Program.HandleSqsEventAsync(
            new TestLambdaContext { Logger = new TestLambdaLogger() },
            services.BuildServiceProvider(),
            sqsEvent);

        Assert.Equal(["match-1"], matchHandler.MessageIds);
        Assert.Equal(["game-1"], gameHandler.MessageIds);
        Assert.Equal(["stats-1", "stats-2"], statsHandler.MessageIds);
        Assert.Equal(["rating-1"], ratingHandler.MessageIds);
        Assert.Equal(["match", "game", "stats", "rating"], invocationOrder);
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
        private readonly string _name;
        private readonly List<string> _invocationOrder;
        private readonly HashSet<string> _failedMessageIds;

        public RecordingSqsHandler(
            string name,
            List<string> invocationOrder,
            IEnumerable<string>? failedMessageIds = null)
        {
            _name = name;
            _invocationOrder = invocationOrder;
            _failedMessageIds = failedMessageIds?.ToHashSet(StringComparer.Ordinal) ?? [];
        }

        public List<string> MessageIds { get; } = [];

        public Task<SQSBatchResponse> HandleAsync(SQSEvent @event, ILambdaContext context)
        {
            _invocationOrder.Add(_name);
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