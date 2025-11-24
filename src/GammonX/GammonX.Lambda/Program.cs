using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

using GammonX.Lambda.Services;

namespace GammonX.Lambda
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var services = Startup.Configure();

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
