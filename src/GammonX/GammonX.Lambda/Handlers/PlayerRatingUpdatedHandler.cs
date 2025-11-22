using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

using GammonX.Lambda.Services;

namespace GammonX.Lambda.Handlers
{
	/// <summary>
	/// Handles <see cref="LambdaFunctions.PlayerRatingUpdatedFunc"/> event.
	/// Calculates the updated rating for a given player.
	/// </summary>
	public class PlayerRatingUpdatedHandler : LambdaHandlerBaseImpl, ISqsLambdaHandler
	{
		/// <summary>
		/// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
		/// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
		/// region the Lambda function is executed in.
		/// </summary>
		public PlayerRatingUpdatedHandler(IDynamoRepository repo) : base(repo)
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

		private static async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
		{
			context.Logger.LogInformation($"Processed message {message.Body}");
			await Task.CompletedTask;
		}
	}
}
