using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using GammonX.DynamoDb.Repository;
using GammonX.Lambda.Extensions;
using GammonX.Models.Contracts;

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
		/// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
		/// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
		/// region the Lambda function is executed in.
		/// </summary>
		public PlayerCreatedHandler(IDynamoDbRepository repo) : base(repo)
		{
			// pass
		}

		// <inheritdoc />
		public async Task HandleAsync(SQSEvent evnt, ILambdaContext context)
		{
			foreach (var message in evnt.Records)
			{
				await ProcessMessageAsync(message, context);
			}
		}

		private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
		{
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
