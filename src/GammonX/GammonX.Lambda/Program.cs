using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

using GammonX.DynamoDb;
using GammonX.DynamoDb.Services;
using GammonX.Lambda.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GammonX.Lambda
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var services = Startup.Configure();

			using (var scope = services.CreateScope())
			{
				var options = services.GetRequiredService<IOptions<DynamoDbOptions>>().Value;
				if (options.Required)
				{
					var dynamoClient = scope.ServiceProvider.GetRequiredService<IAmazonDynamoDB>();
					await DynamoDbInitializer.EnsureTablesExistAsync(dynamoClient, options);
				}
			}

			async Task Router(SQSEvent input, ILambdaContext context)
			{
				var lambdaFuncHandler = LambdaFunctionFactory.Create(services, context.FunctionName);
				await lambdaFuncHandler.HandleAsync(input, context);
			}

			var bootstrap = LambdaBootstrapBuilder.Create<SQSEvent>(Router, new DefaultLambdaJsonSerializer()).Build();

			await bootstrap.RunAsync();
		}
	}
}
