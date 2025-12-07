using Amazon;
using Amazon.Runtime;
using Amazon.SQS;

using GammonX.Server.Queue;

using Microsoft.Extensions.Options;

namespace GammonX.Server.Extensions
{
    public static class WorkQueueServiceExtensions
    {
        public static void AddWorkQueueServices(this IServiceCollection services, IConfiguration workQueueOptions)
        {
            services.AddSingleton<IWorkQueueService, WorkQueueService>();

            services.Configure<WorkQueueOptions>(workQueueOptions);
            // we check manually if a real work queue config is required
            var queueUrl = Environment.GetEnvironmentVariable("WORK_QUEUE__URL");
            if (string.IsNullOrEmpty(queueUrl))
            {
                // we setup a dummy work queue
                services.AddSingleton<IWorkQueue, LogWorkQueue>();
                Serilog.Log.Information($"WorkQueue: '{nameof(LogWorkQueue)}' Queue URL: '{queueUrl}'");
                return;
            }

            Serilog.Log.Information($"WorkQueue: '{nameof(SqsWorkQueue)}' Queue URL: '{queueUrl}'");
            // we setup an aws simple queue service
            services.AddSingleton<IAmazonSQS>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var options = sp.GetRequiredService<IOptions<WorkQueueOptions>>().Value;
                var isLocal = string.IsNullOrEmpty(options.REGION);
                var keyAuth = !string.IsNullOrEmpty(options.AWS_ACCESS_KEY_ID) && !string.IsNullOrEmpty(options.AWS_SECRET_ACCESS_KEY);
                if (isLocal)
                {
                    // local docker instance
                    var accessKeyId = options.AWS_ACCESS_KEY_ID;
                    var secretAccessKey = options.AWS_SECRET_ACCESS_KEY;
                    var credentials = new BasicAWSCredentials(options.AWS_ACCESS_KEY_ID, options.AWS_SECRET_ACCESS_KEY);
                    var sqsConfig = new AmazonSQSConfig
                    {
                        ServiceURL = options.SERVICEURL,
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

            var gameCompletedQueueUrl = Environment.GetEnvironmentVariable("WORK_QUEUE__GAME_COMPLETED_QUEUE_URL");
            if (!string.IsNullOrEmpty(gameCompletedQueueUrl))
            {
                services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.GameCompleted, (sp, key) =>
                {
                    var sqs = sp.GetRequiredService<IAmazonSQS>();
                    var options = sp.GetRequiredService<IOptions<WorkQueueOptions>>().Value;
                    return new SqsWorkQueue(sqs, options.GAME_COMPLETED_QUEUE_URL);
                });
            }
            var matchCompletedQueueUrl = Environment.GetEnvironmentVariable("WORK_QUEUE__MATCH_COMPLETED_QUEUE_URL");
            if (!string.IsNullOrEmpty(matchCompletedQueueUrl))
            {
                services.AddKeyedSingleton<IWorkQueue>(WorkQueueType.MatchCompleted, (sp, key) =>
                {
                    var sqs = sp.GetRequiredService<IAmazonSQS>();
                    var options = sp.GetRequiredService<IOptions<WorkQueueOptions>>().Value;
                    return new SqsWorkQueue(sqs, options.MATCH_COMPLETED_QUEUE_URL);
                });
            }

            // todo add other queues
        }
    }
}
