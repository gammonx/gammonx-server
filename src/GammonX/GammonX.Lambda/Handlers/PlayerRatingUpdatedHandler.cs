using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

namespace GammonX.Lambda.Handlers
{
	/// <summary>
	/// Handles RATING_UPDATED event.
	/// Calculates the updated rating for a given player.
	/// </summary>
	public class PlayerRatingUpdatedHandler
	{
		/// <summary>
		/// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
		/// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
		/// region the Lambda function is executed in.
		/// </summary>
		public PlayerRatingUpdatedHandler()
		{
			// pass
		}

		/// <summary>
		/// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
		/// to respond to SQS messages.
		/// </summary>
		/// <param name="evnt">The event for the Lambda function handler to process.</param>
		/// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
		/// <returns></returns>
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
