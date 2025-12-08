using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

using GammonX.Models.Contracts;

using GammonX.Lambda.Services;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

namespace GammonX.Lambda
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var services = Startup.Configure();

			async Task<object> Router(object input, ILambdaContext context)
			{
                context.Logger.LogInformation($"Received input of type: '{input.GetType().FullName}' for function '{context.FunctionName}'");

                if (input is MemoryStream stream)
                {
                    var json = await ReadStreamAsStringAsync(stream);
                    context.Logger.LogInformation($"Received input JSON: {json.Length} characters");

                    var deserializedInput = DeserializeFunctionInput(json, context.FunctionName);

                    if (deserializedInput is SQSEvent sqsEvent)
                    {
                        context.Logger.LogInformation($"Received SQS event. Creating dedicated function handler...");
                        return await HandleSqsEventAsync(context, services, sqsEvent);
                    }
                    else if (deserializedInput is APIGatewayProxyRequest apiRequest)
                    {
                        context.Logger.LogInformation($"Received API Gateway request. Creating dedicated function handler...");
                        return await HandleGatewayRequestAsync(context, services, apiRequest);
                    }
                    else
                    {
                        context.Logger.LogInformation($"Received unknown function input. Unable to create function handler. Returning empy response.");
                        return new object();
                    }
                }                
                else
                {
                    context.Logger.LogInformation($"Received unknown function input. Unable to create function handler. Returning empy response.");
                    return new object();
                }
			}

			var bootstrapper = LambdaBootstrapBuilder.Create(Router, new DefaultLambdaJsonSerializer()).Build();
			await bootstrapper.RunAsync();
        }

        private static async Task<object> HandleSqsEventAsync(ILambdaContext context, IServiceProvider services, SQSEvent sqsEvent)
        {
            using var scope = services.CreateScope();
            var handler = LambdaFunctionFactory.CreateSqsHandler(scope.ServiceProvider, context.FunctionName);
            await handler.HandleAsync(sqsEvent, context);
            return new object();
        }

        private static async Task<object> HandleGatewayRequestAsync(ILambdaContext context, IServiceProvider services, APIGatewayProxyRequest apiRequest)
        {
            try
            {
                using var scope = services.CreateScope();
                var handler = LambdaFunctionFactory.CreateApiHandler(apiRequest, scope.ServiceProvider);

                if (handler == null)
                {
                    var notFound = Models.Contracts.ContractExtensions.ToResponse("The requested api route does not exist");
                    return CreateGatewayResponse(404, notFound);
                }

                var result = await handler.HandleAsync(apiRequest, context);
                if (result == null)
                {
                    var error = Models.Contracts.ContractExtensions.ToResponse("An error occurred while handling the api request");
                    return CreateGatewayResponse(500, error);
                }
                return CreateGatewayResponse(200, result);
            }
            catch (Exception ex)
            {
                return CreateGatewayResponse(500, Models.Contracts.ContractExtensions.ToResponse(ex.Message));
            }
        }

        private static APIGatewayProxyResponse CreateGatewayResponse(int httpCode, BaseResponseContract response)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = httpCode,
                Body = JsonConvert.SerializeObject(response),
                Headers = new Dictionary<string, string>
                {
                    {"Content-Type", "application/json"}
                }
            };
        }

        private static object? DeserializeFunctionInput(string json, string functionName)
        {
            switch (functionName)
            {
                case LambdaFunctions.GameCompletedFunc:
                case LambdaFunctions.MatchCompletedFunc:
                case LambdaFunctions.PlayerCreatedFunc:
                case LambdaFunctions.PlayerRatingUpdatedFunc:
                case LambdaFunctions.PlayerStatsUpdatedFunc:
                    var sqsEvent = JsonConvert.DeserializeObject<SQSEvent>(json);
                    return sqsEvent;
                case LambdaFunctions.ApiGatewayHandlerFunc:
                    var apiEvent = JsonConvert.DeserializeObject<APIGatewayProxyRequest>(json);
                    return apiEvent;
                default:
                    return null;
            }
        }

        private static async Task<string> ReadStreamAsStringAsync(Stream stream)
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
    }
}
