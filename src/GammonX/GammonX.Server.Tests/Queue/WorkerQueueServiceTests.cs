using Amazon.Runtime;
using Amazon.SQS;

using DotNetEnv;

using GammonX.Engine.Services;

using GammonX.Models;
using GammonX.Models.Contracts;
using GammonX.Models.Enums;

using GammonX.Server.Models;
using GammonX.Server.Queue;
using GammonX.Server.Services;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using MatchType = GammonX.Models.Enums.MatchType;

namespace GammonX.Server.Tests.Queue
{
    public class WorkerQueueServiceTests
    {
        static WorkerQueueServiceTests()
        {
            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
            {
                var envLocal = Path.Combine(AppContext.BaseDirectory, ".env.local");
                var env = Path.Combine(AppContext.BaseDirectory, ".env");

                if (File.Exists(envLocal))
                {
                    Env.Load(envLocal);
                }
                else if (File.Exists(env))
                {
                    Env.Load(env);
                }
            }
        }

        public static bool IsAwsEnvironment => Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") == "AWS";

        private readonly IServiceProvider _serviceProvider;

        public WorkerQueueServiceTests()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IAmazonSQS>(_ =>
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

            services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.GameCompleted, (sp, _) =>
            {
                var sqs = sp.GetRequiredService<IAmazonSQS>();
                return new SqsWorkQueue(sqs, "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/GAME_COMPLETED_QUEUE", WorkQueueType.GameCompleted.GetName());
            });

            services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.MatchCompleted, (sp, _) =>
            {
                var sqs = sp.GetRequiredService<IAmazonSQS>();
                return new SqsWorkQueue(sqs, "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/MATCH_COMPLETED_QUEUE", WorkQueueType.MatchCompleted.GetName());
            });
            services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.PlayerCreated, (sp, _) =>
            {
                var sqs = sp.GetRequiredService<IAmazonSQS>();
                return new SqsWorkQueue(sqs, "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/PLAYER_CREATED_QUEUE", WorkQueueType.PlayerCreated.GetName());
            });
            services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.StatsUpdated, (sp, _) =>
            {
                var sqs = sp.GetRequiredService<IAmazonSQS>();
                return new SqsWorkQueue(sqs, "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/STATS_UPDATED_QUEUE", WorkQueueType.StatsUpdated.GetName());
            });
            services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.RatingUpdated, (sp, _) =>
            {
                var sqs = sp.GetRequiredService<IAmazonSQS>();
                return new SqsWorkQueue(sqs, "http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/RATING_UPDATED_QUEUE", WorkQueueType.RatingUpdated.GetName());
            });

            _serviceProvider = services.BuildServiceProvider();

        }

        [Fact(Skip = "AWS_STACK", SkipUnless = nameof(IsAwsEnvironment))]
        public async Task CanEnqueueRatingUpdateRecord()
        {
            var service = new WorkQueueService(_serviceProvider);
            var match = CreateAndCompleteSimpleMatch();
            await service.EnqueueRatingProcessingAsync(match, CancellationToken.None);
        }

        [Fact(Skip = "AWS_STACK", SkipUnless = nameof(IsAwsEnvironment))]
        public async Task CanEnqueueStatUpdateRecord()
        {
            var service = new WorkQueueService(_serviceProvider);
            var match = CreateAndStartSimpleMatch();
            await service.EnqueueStatProcessingAsync(match, CancellationToken.None);
        }

        [Fact(Skip = "AWS_STACK", SkipUnless = nameof(IsAwsEnvironment))]
        public async Task CanEnqueueMatchRecord()
        {
            var service = new WorkQueueService(_serviceProvider);
            var match = CreateAndCompleteSimpleMatch();
            await service.EnqueueMatchResultAsync(match, CancellationToken.None);
        }

        [Fact(Skip = "AWS_STACK", SkipUnless = nameof(IsAwsEnvironment))]
        public async Task CanEnqueueGameRecord()
        {
            var service = new WorkQueueService(_serviceProvider);
            var match = CreateAndStartSimpleMatch();
            await service.EnqueueGameResultAsync(match, 1, CancellationToken.None);
        }

        [Fact]
        public async Task EnqueueGameResultPublishesOneCompositeMessage()
        {
            var queue = new Mock<IWorkQueue>();
            queue
                .Setup(value => value.EnqueueAsync(It.IsAny<GameCompletedWorkContract>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            var services = new ServiceCollection();
            services.AddKeyedSingleton(WorkQueueType.GameCompleted, queue.Object);
           
            var service = new WorkQueueService(services.BuildServiceProvider());
            var match = CreateAndStartSimpleMatch();

            await service.EnqueueGameResultAsync(match, 1, CancellationToken.None);

            queue.Verify(
                value => value.EnqueueAsync(
                    It.Is<GameCompletedWorkContract>(work => work.Records.Length == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            queue.Verify(
                value => value.EnqueueBatchAsync(It.IsAny<IEnumerable<GameRecordContract>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task EnqueueMatchResultPublishesOneCompositeMessage()
        {
            var queue = new Mock<IWorkQueue>();
            queue
                .Setup(value => value.EnqueueAsync(It.IsAny<MatchCompletedWorkContract>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            var services = new ServiceCollection();
            services.AddKeyedSingleton(WorkQueueType.MatchCompleted, queue.Object);
            
            var service = new WorkQueueService(services.BuildServiceProvider());
            var match = CreateAndCompleteSimpleMatch();

            await service.EnqueueMatchResultAsync(match, CancellationToken.None);

            queue.Verify(
                value => value.EnqueueAsync(
                    It.Is<MatchCompletedWorkContract>(work => work.Records.Length == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            queue.Verify(
                value => value.EnqueueBatchAsync(It.IsAny<IEnumerable<MatchRecordContract>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task EnqueueRatingProcessingPublishesOneCompositeMessage()
        {
            var queue = new Mock<IWorkQueue>();
            queue
                .Setup(value => value.EnqueueAsync(It.IsAny<RatingUpdateWorkContract>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            var services = new ServiceCollection();
            services.AddKeyedSingleton(WorkQueueType.RatingUpdated, queue.Object);
            
            var service = new WorkQueueService(services.BuildServiceProvider());
            var match = CreateAndCompleteSimpleMatch();

            await service.EnqueueRatingProcessingAsync(match, CancellationToken.None);

            queue.Verify(
                value => value.EnqueueAsync(
                    It.Is<RatingUpdateWorkContract>(work => work.Records.Length == 2),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            queue.Verify(
                value => value.EnqueueBatchAsync(It.IsAny<IEnumerable<MatchRecordContract>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static IMatchSessionModel CreateAndStartSimpleMatch()
        {
            var diceFactory = new DiceServiceFactory();
            var gameFactory = new GameSessionFactory(diceFactory);
            var factory = new MatchSessionFactory(gameFactory);

            var queueKey = new QueueKey(MatchVariant.Tavli, MatchModus.Ranked, MatchType.CashGame, BotLevel.Hard);
            var match = factory.Create(Guid.NewGuid(), queueKey);

            var player1Id = Guid.Parse("b08e895f-a397-4b44-89cc-2372e9b54657");
            var player1 = new PlayerConnection(player1Id);
            player1.SetConnectionId(Guid.NewGuid().ToString());
            var player2Id = Guid.Parse("d8e1f3b4-6120-4c16-acf2-b2b8d03f14aa");
            var player2 = new PlayerConnection(player2Id);
            player2.SetConnectionId(Guid.NewGuid().ToString());

            match.JoinSession(player1);
            match.JoinSession(player2);

            match.StartMatch(match.Player1.Id);
            var gameSession = match.GetGameSession(match.GameRound);
            Assert.NotNull(gameSession);
            gameSession.StopGame(new GameResultModel(
                player1Id,
                GameResult.Gammon,
                GameResult.LostGammon,
                2));
            return match;
        }

        private static IMatchSessionModel CreateAndCompleteSimpleMatch()
        {
            var diceFactory = new DiceServiceFactory();
            var gameFactory = new GameSessionFactory(diceFactory);
            var factory = new MatchSessionFactory(gameFactory);
            var queueKey = new QueueKey(MatchVariant.Tavli, MatchModus.Ranked, MatchType.SevenPointGame, BotLevel.Hard);
            var match = factory.Create(Guid.NewGuid(), queueKey);
            var player1 = new PlayerConnection(Guid.NewGuid());
            player1.SetConnectionId(Guid.NewGuid().ToString());
            var player2 = new PlayerConnection(Guid.NewGuid());
            player2.SetConnectionId(Guid.NewGuid().ToString());
            match.JoinSession(player1);
            match.JoinSession(player2);
            match.StartMatch(player1.Id);

            match.ResignMatch(player2.Id);
            Assert.True(match.IsMatchOver());
            return match;
        }
    }
}
