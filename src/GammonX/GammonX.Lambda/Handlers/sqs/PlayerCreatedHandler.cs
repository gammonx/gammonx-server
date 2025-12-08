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
        public PlayerCreatedHandler() : base()
        {
            // pass
        }

        // <inheritdoc />
        [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
        public async Task HandleAsync(SQSEvent @event, ILambdaContext context)
		{
            try
            {
                if (_repo == null)
                {
                    context.Logger.LogInformation($"Setting up DI services...");
                    var services = Startup.Configure();
                    _repo = services.GetRequiredService<IDynamoDbRepository>();
                }

                foreach (var message in @event.Records)
                {
                    await ProcessMessageAsync(message, context);
                }
            }
			catch (Exception ex) 
			{
				foreach (var record in @event.Records)
				{
					context.Logger.LogError(ex, $"An error occurred while processing player created. Message id: '{record.MessageId}'");

                }
            }
		}

		private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
		{
            if (_repo == null)
                throw new NullReferenceException("db repo must not be null");

            context.Logger.LogInformation($"Processing message with id '{message.MessageId}'");

            var json = message.Body;
            var playerRecord = JsonConvert.DeserializeObject<PlayerRecordContract>(json);

            if (playerRecord == null)
            {
                context.Logger.LogError($"An error occurred while deserializing body of '{message.MessageId}'");
                return;
            }

            context.Logger.LogInformation($"Processing created player with id '{playerRecord.Id}'");

			var playerItem = playerRecord.ToPlayer();

            await _repo.SaveAsync(playerItem);

            context.Logger.LogInformation($"Processed created player with id '{playerRecord.Id}'");
        }
	}
}
