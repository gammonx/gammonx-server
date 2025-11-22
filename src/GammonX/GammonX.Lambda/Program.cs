using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

using GammonX.Lambda.Handlers;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.Lambda
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var services = Startup.Configure();

			var matchCompletedHandler = services.GetRequiredKeyedService<ISqsLambdaHandler>(LambdaFunctions.MatchCompletedFunc);
			var gameCompletedHandler = services.GetRequiredKeyedService<ISqsLambdaHandler>(LambdaFunctions.GameCompletedFunc);
			var playerRatingUpdatedHandler = services.GetRequiredKeyedService<ISqsLambdaHandler>(LambdaFunctions.PlayerRatingUpdatedFunc);
			var playerStatsUpdatedHandler = services.GetRequiredKeyedService<ISqsLambdaHandler>(LambdaFunctions.PlayerStatsUpdatedFunc);

			async Task Router(SQSEvent input, ILambdaContext context)
			{
				switch (context.FunctionName)
				{
					case LambdaFunctions.MatchCompletedFunc:
						await matchCompletedHandler.HandleAsync(input, context);
						break;
					case LambdaFunctions.GameCompletedFunc:
						await gameCompletedHandler.HandleAsync(input, context);
						break;
					case LambdaFunctions.PlayerRatingUpdatedFunc:
						await playerRatingUpdatedHandler.HandleAsync(input, context);
						break;
					case LambdaFunctions.PlayerStatsUpdatedFunc:
						await playerStatsUpdatedHandler.HandleAsync(input, context);
						break;
					default:
						throw new Exception($"Unknown function: {context.FunctionName}");
				}
			}

			var bootstrap = LambdaBootstrapBuilder.Create<SQSEvent>(Router, new DefaultLambdaJsonSerializer()).Build();

			await bootstrap.RunAsync();
		}
	}
}
