using Amazon.Runtime;
using Amazon.SQS;

using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Queue;
using GammonX.Server.Services;

using Microsoft.Extensions.DependencyInjection;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests.Queue
{
    public class WorkerQueueServiceTests
    {
        private IServiceProvider _serviceProvider;

        public WorkerQueueServiceTests()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IAmazonSQS>(sp =>
            {
                // local docker instance
                var accessKeyId = "local";
                var secretAccessKey = "local";
                var credentials = new BasicAWSCredentials(accessKeyId, secretAccessKey);
                var sqsConfig = new AmazonSQSConfig
                {
                    ServiceURL = "http://localhost:4566",
                };
                return new AmazonSQSClient(credentials, sqsConfig);
            });

            services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.GameCompleted, (sp, key) =>
            {
                var sqs = sp.GetRequiredService<IAmazonSQS>();
                return new SqsWorkQueue(sqs, "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/GAME_COMPLETED_QUEUE");
            });

            services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.MatchCompleted, (sp, key) =>
            {
                var sqs = sp.GetRequiredService<IAmazonSQS>();
                return new SqsWorkQueue(sqs, "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/MATCH_COMPLETED_QUEUE");
            });
            services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.PlayerCreated, (sp, key) =>
            {
                var sqs = sp.GetRequiredService<IAmazonSQS>();
                return new SqsWorkQueue(sqs, "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/PLAYER_CREATED_QUEUE");
            });
            services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.StatsUpdated, (sp, key) =>
            {
                var sqs = sp.GetRequiredService<IAmazonSQS>();
                return new SqsWorkQueue(sqs, "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/STATS_UPDATED_QUEUE");
            });
            services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.RatingUpdated, (sp, key) =>
            {
                var sqs = sp.GetRequiredService<IAmazonSQS>();
                return new SqsWorkQueue(sqs, "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/RATING_UPDATED_QUEUE");
            });

            _serviceProvider = services.BuildServiceProvider();

        }

        [Fact(Skip = "AWS STACK TEST")]
        public async Task CanEnqueueRatingUpdateRecord()
        {
            var service = new WorkQueueService(_serviceProvider);
            var match = CreateAndStartSimpleMatch();
            await service.EnqueueRatingProcessingAsync(match, CancellationToken.None);
        }

        [Fact(Skip = "AWS STACK TEST")]
        public async Task CanEnqueueStatUpdateRecord()
        {
            var service = new WorkQueueService(_serviceProvider);
            var match = CreateAndStartSimpleMatch();
            await service.EnqueueStatProcessingAsync(match, CancellationToken.None);
        }

        [Fact(Skip = "AWS STACK TEST")]
        public async Task CanEnqueueMatchRecord()
        {
            var service = new WorkQueueService(_serviceProvider);
            var match = CreateAndStartSimpleMatch();
            await service.EnqueueMatchResultAsync(match, CancellationToken.None);
        }

        [Fact(Skip = "AWS STACK TEST")]
        public async Task CanEnqueueGameRecord()
        {
            var service = new WorkQueueService(_serviceProvider);
            var match = CreateAndStartSimpleMatch();
            await service.EnqueueGameResultAsync(match, 1, CancellationToken.None);
        }

        private static IMatchSessionModel CreateAndStartSimpleMatch()
        {
            var diceFactory = new DiceServiceFactory();
            var gameFactory = new GameSessionFactory(diceFactory);
            var factory = new MatchSessionFactory(gameFactory);

            var queueKey = new QueueKey(MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame);
            var match = factory.Create(Guid.NewGuid(), queueKey);

            var player1Id = Guid.Parse("b08e895f-a397-4b44-89cc-2372e9b54657");
            var player1 = new LobbyEntry(player1Id);
            player1.SetConnectionId(Guid.NewGuid().ToString());
            var player2Id = Guid.Parse("d8e1f3b4-6120-4c16-acf2-b2b8d03f14aa");
            var player2 = new LobbyEntry(player2Id);
            player2.SetConnectionId(Guid.NewGuid().ToString());

            match.JoinSession(player1);
            match.JoinSession(player2);

            match.StartMatch(match.Player1.Id);
            return match;
        }
    }
}
