using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using GammonX.Lambda.Handlers.Contracts;

namespace GammonX.Lambda.Handlers
{
    /// <summary>
    /// Marker interface for lambda handlers that react on AWS API Gateway requests.
    /// </summary>
    public interface IApiLambdaHandler
    {
        /// <summary>
		/// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
		/// to respond to SQS messages.
		/// </summary>
		/// <param name="evnt">The event for the Lambda function handler to process.</param>
		/// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
		/// <returns>Task to be awaited</returns>
		Task<BaseResponseContract?> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context);
    }
}
