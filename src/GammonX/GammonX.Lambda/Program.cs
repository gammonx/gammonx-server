using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

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

            await Startup.ConfigureDynamoDbTableAsync(services);

			async Task<object> Router(object input, ILambdaContext context)
			{
				if (input is SQSEvent sqsEvent)
                {
                    return HandleSqsEventAsync(context, services, sqsEvent);
                }
                else if (input is APIGatewayProxyRequest apiRequest)
                {
                    return HandleGatewayRequestAsync(context, services, apiRequest);
                }
                else
                {
                    return new object();
                }
			}

			var bootstrapper = LambdaBootstrapBuilder.Create<object>(Router, new DefaultLambdaJsonSerializer()).Build();
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
                var handler = LambdaFunctionFactory.CreateApiHandler(scope.ServiceProvider, context.FunctionName);

                var result = await handler.HandleAsync(apiRequest, context);

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = JsonConvert.SerializeObject(result),
                    Headers = new Dictionary<string, string>
                    {
                        {"Content-Type", "application/json"}
                    }
                };
            }
            catch (Exception ex)
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = ex.Message,
                    Headers = new Dictionary<string, string>
                    {
                        {"Content-Type", "application/text"}
                    }
                };
            }
        }
    }
}
