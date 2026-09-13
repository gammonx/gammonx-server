using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;

namespace GammonX.Lambda.Handlers
{
	/// <summary>
	/// Marker interface for lambda handlers that react on SQS events.
	/// </summary>
	public interface ISqsLambdaHandler
	{
		/// <summary>
		/// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
		/// to respond to SQS messages.
		/// </summary>
		/// <param name="event">The event for the Lambda function handler to process.</param>
		/// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
		/// <returns>A response containing the identifiers of records that should be retried.</returns>
		Task<SQSBatchResponse> HandleAsync(SQSEvent @event, ILambdaContext context);
	}
}
