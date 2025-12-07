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

        [Fact]
        public async Task CanEnqueueMatchRecord()
        {
            // TODO :: create a proper finished match
            var service = new WorkQueueService(_serviceProvider);
            var match = CreateAndStartSimpleMatch();
            await service.EnqueueMatchResultAsync(match, CancellationToken.None);
        }

        [Fact]
        public async Task CanEnqueueGameRecord()
        {
            // TODO :: create a proper finished game
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

            var player1 = new LobbyEntry(Guid.NewGuid());
            player1.SetConnectionId(Guid.NewGuid().ToString());
            var player2 = new LobbyEntry(Guid.NewGuid());
            player2.SetConnectionId(Guid.NewGuid().ToString());

            match.JoinSession(player1);
            match.JoinSession(player2);

            match.StartNextGame(match.Player1.Id);
            return match;
        }
    }
}
