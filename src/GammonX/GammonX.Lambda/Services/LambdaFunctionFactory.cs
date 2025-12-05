using GammonX.DynamoDb.Items;
using GammonX.Lambda.Handlers;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.Lambda.Services
{
	public static class LambdaFunctionFactory
	{
		public static ISqsLambdaHandler CreateSqsHandler(IServiceProvider services, string functionName)
		{
			switch (functionName)
			{
				case LambdaFunctions.GameCompletedFunc:
				case LambdaFunctions.PlayerRatingUpdatedFunc:
				case LambdaFunctions.PlayerStatsUpdatedFunc:
				case LambdaFunctions.MatchCompletedFunc:
				case LambdaFunctions.PlayerCreatedFunc:
					return services.GetRequiredKeyedService<ISqsLambdaHandler>(functionName);
                default:
					throw new InvalidOperationException($"unknown lambda function with name '{functionName}'");
			}
		}

		public static IApiLambdaHandler CreateApiHandler(IServiceProvider services, string functionName)
		{
			switch (functionName)
			{
                case LambdaFunctions.GetPlayerRatingFunc:
                    return services.GetRequiredKeyedService<IApiLambdaHandler>(functionName);
                default:
                    throw new InvalidOperationException($"unknown lambda function with name '{functionName}'");
            }
		}

    }
}
