using Amazon;
using Amazon.Runtime;
using Amazon.SQS;

using GammonX.Models.Enums;

using GammonX.Server.Queue;

using Microsoft.Extensions.Options;

namespace GammonX.Server.Extensions
{
    public static class WorkQueueServiceExtensions
    {
        public static void AddWorkQueueServices(this IServiceCollection services, IConfiguration workQueueOptions)
        {
            services.AddSingleton<IWorkQueueService, WorkQueueService>();
            services.AddHealthChecks().AddCheck<WorkQueueHealthCheck>("work-queues");

            services.Configure<WorkQueueOptions>(workQueueOptions);
            var configuredOptions = workQueueOptions.Get<WorkQueueOptions>() ?? new WorkQueueOptions();
            ValidateWorkQueueOptions(configuredOptions);

            // we check manually if a real work queue config is required
            if (string.IsNullOrWhiteSpace(configuredOptions.URL))
            {
                // we setup a dummy work queue
                services.AddSingleton<IWorkQueue, LogWorkQueue>();
                Serilog.Log.Information("WorkQueue: '{LogWorkQueueName}' Queue URL: '{ConfiguredOptionsUrl}'", nameof(LogWorkQueue), configuredOptions.URL);
                return;
            }

            Serilog.Log.Information("WorkQueue: '{SqsWorkQueueName}' Queue URL: '{ConfiguredOptionsUrl}'", nameof(SqsWorkQueue), configuredOptions.URL);
            // we setup an aws simple queue service
            services.AddSingleton<IAmazonSQS>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<WorkQueueOptions>>().Value;
                var isLocal = string.IsNullOrEmpty(options.REGION);
                var keyAuth = !string.IsNullOrEmpty(options.AWS_ACCESS_KEY_ID) && !string.IsNullOrEmpty(options.AWS_SECRET_ACCESS_KEY);
                if (isLocal)
                {
                    // local docker instance
                    var credentials = new BasicAWSCredentials(options.AWS_ACCESS_KEY_ID, options.AWS_SECRET_ACCESS_KEY);
                    var sqsConfig = new AmazonSQSConfig
                    {
                        ServiceURL = options.URL,
                    };
                    return new AmazonSQSClient(credentials, sqsConfig);
                }
                else if (keyAuth)
                {
                    // AWS hosted accessed locally
                    var region = RegionEndpoint.GetBySystemName(options.REGION);
                    var sqsConfig = new AmazonSQSConfig
                    {
                        // We do not need a specific service url, the region endpoint is sufficient
                        RegionEndpoint = region
                    };
                    // We use access key based auth
                    var credentials = new BasicAWSCredentials(options.AWS_ACCESS_KEY_ID, options.AWS_SECRET_ACCESS_KEY);
                    return new AmazonSQSClient(credentials, sqsConfig);
                }
                else
                {
                    // AWS hosted
                    var region = RegionEndpoint.GetBySystemName(options.REGION);
                    var sqsConfig = new AmazonSQSConfig
                    {
                        // We do not need a specific service url, the region endpoint is sufficient
                        RegionEndpoint = region
                    };
                    // We use role based auth
                    return new AmazonSQSClient(sqsConfig);
                }
            });

            if (!string.IsNullOrWhiteSpace(configuredOptions.GAME_COMPLETED_QUEUE_URL))
            {
                services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.GameCompleted, (sp, _) =>
                {
                    var options = sp.GetRequiredService<IOptions<WorkQueueOptions>>().Value;
                    return CreateSqsWorkQueue(sp, options.GAME_COMPLETED_QUEUE_URL, WorkQueueType.GameCompleted.GetName());
                });
            }
            if (!string.IsNullOrWhiteSpace(configuredOptions.MATCH_COMPLETED_QUEUE_URL))
            {
                services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.MatchCompleted, (sp, _) =>
                {
                    var options = sp.GetRequiredService<IOptions<WorkQueueOptions>>().Value;
                    return CreateSqsWorkQueue(sp, options.MATCH_COMPLETED_QUEUE_URL, WorkQueueType.MatchCompleted.GetName());
                });
            }

            if (!string.IsNullOrWhiteSpace(configuredOptions.PLAYER_CREATED_QUEUE_URL))
            {
                services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.PlayerCreated, (sp, _) =>
                {
                    var options = sp.GetRequiredService<IOptions<WorkQueueOptions>>().Value;
                    return CreateSqsWorkQueue(sp, options.PLAYER_CREATED_QUEUE_URL, WorkQueueType.PlayerCreated.GetName());
                });
            }

            if (!string.IsNullOrWhiteSpace(configuredOptions.STATS_UPDATED_QUEUE_URL))
            {
                services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.StatsUpdated, (sp, _) =>
                {
                    var options = sp.GetRequiredService<IOptions<WorkQueueOptions>>().Value;
                    return CreateSqsWorkQueue(sp, options.STATS_UPDATED_QUEUE_URL, WorkQueueType.StatsUpdated.GetName());
                });
            }

            if (!string.IsNullOrWhiteSpace(configuredOptions.RATING_UPDATED_QUEUE_URL))
            {
                services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.RatingUpdated, (sp, _) =>
                {
                    var options = sp.GetRequiredService<IOptions<WorkQueueOptions>>().Value;
                    return CreateSqsWorkQueue(sp, options.RATING_UPDATED_QUEUE_URL, WorkQueueType.RatingUpdated.GetName());
                });
            }
        }

        internal static void ValidateWorkQueueOptions(WorkQueueOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var queueUrlSettings = new (string Name, string Value)[]
            {
                (nameof(WorkQueueOptions.GAME_COMPLETED_QUEUE_URL), options.GAME_COMPLETED_QUEUE_URL),
                (nameof(WorkQueueOptions.MATCH_COMPLETED_QUEUE_URL), options.MATCH_COMPLETED_QUEUE_URL),
                (nameof(WorkQueueOptions.PLAYER_CREATED_QUEUE_URL), options.PLAYER_CREATED_QUEUE_URL),
                (nameof(WorkQueueOptions.STATS_UPDATED_QUEUE_URL), options.STATS_UPDATED_QUEUE_URL),
                (nameof(WorkQueueOptions.RATING_UPDATED_QUEUE_URL), options.RATING_UPDATED_QUEUE_URL)
            };

            if (string.IsNullOrWhiteSpace(options.URL))
            {
                if (queueUrlSettings.Any(queue => !string.IsNullOrWhiteSpace(queue.Value)))
                {
                    throw new InvalidOperationException($"Typed work queue URLs require '{nameof(WorkQueueOptions.URL)}' to enable real queue mode.");
                }

                return;
            }

            var missingQueueUrls = queueUrlSettings
            .Where(queue => string.IsNullOrWhiteSpace(queue.Value))
            .Select(queue => queue.Name)
            .ToArray();

            if (missingQueueUrls.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Work queue mode is enabled by '{nameof(WorkQueueOptions.URL)}', but these queue URLs are missing: {string.Join(", ", missingQueueUrls)}.");
            }

            if (options.MAX_RETRY_ATTEMPTS < 1)
            {
                throw new InvalidOperationException("Work queue retry attempts must be at least 1.");
            }

            if (options.RETRY_BASE_DELAY_MILLISECONDS < 0)
            {
                throw new InvalidOperationException("Work queue retry delay cannot be negative.");
            }
        }

        private static SqsWorkQueue CreateSqsWorkQueue(IServiceProvider services, string queueUrl, string eventType)
        {
            var sqs = services.GetRequiredService<IAmazonSQS>();
            var options = services.GetRequiredService<IOptions<WorkQueueOptions>>().Value;
            return new SqsWorkQueue(
                sqs,
                queueUrl,
                eventType,
                options.MAX_RETRY_ATTEMPTS,
                TimeSpan.FromMilliseconds(options.RETRY_BASE_DELAY_MILLISECONDS));
        }
    }
}
