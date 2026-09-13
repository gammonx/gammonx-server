using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

using GammonX.DynamoDb.Repository;
using GammonX.Lambda.Extensions;
using GammonX.Models.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GammonX.Lambda.Handlers
{
	/// <summary>
	/// Handles <see cref="LambdaFunctions.PlayerCreatedFunc"/> event.
	/// Calculates the updated rating for a given player.
	/// </summary>
	public class PlayerCreatedHandler : LambdaHandlerBaseImpl, ISqsLambdaHandler
	{
        /// <summary>
        /// Default constructor for container based lambda execution. 
        /// This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
		public PlayerCreatedHandler(IDynamoDbRepository repo) : base(repo)
		{
			// pass
		}

        /// <summary>
        /// Default constructor for .zip based lambda execution. We need to kick off the DI manually.
        /// </summary>
        public PlayerCreatedHandler()
        {
            // pass
        }

        // <inheritdoc />
        [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
        public async Task<SQSBatchResponse> HandleAsync(SQSEvent @event, ILambdaContext context)
		{
            if (Repo == null)
            {
                context.Logger.LogInformation("Setting up DI services...");
                var services = Startup.Configure();
                Repo = services.GetRequiredService<IDynamoDbRepository>();
            }

            var failures = new List<SQSBatchResponse.BatchItemFailure>();
            foreach (var message in @event.Records)
            {
                try
                {
                    await ProcessMessageAsync(message, context);
                }
                catch (Exception ex)
                {
                    context.Logger.LogError(ex, $"An error occurred while processing player created. Message id: '{message.MessageId}'");
                    failures.Add(new SQSBatchResponse.BatchItemFailure { ItemIdentifier = message.MessageId });
                }
            }

            return new SQSBatchResponse(failures);
		}

		private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
		{
            if (Repo == null)
                throw new NullReferenceException("db repo must not be null");

            context.Logger.LogInformation($"Processing message with id '{message.MessageId}'");

            var json = message.Body;
            var playerRecord = JsonConvert.DeserializeObject<PlayerRecordContract>(json);

            if (playerRecord == null)
            {
				throw new InvalidOperationException($"Unable to deserialize player created message '{message.MessageId}'.");
            }

            context.Logger.LogInformation($"Processing created player with id '{playerRecord.Id}'");

			var playerItem = playerRecord.ToPlayer();

            await Repo.SaveAsync(playerItem);

            context.Logger.LogInformation($"Processed created player with id '{playerRecord.Id}'");
        }
	}
}
