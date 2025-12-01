using GammonX.Lambda.Handlers;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.Lambda.Services
{
	public static class LambdaFunctionFactory
	{
		public static ISqsLambdaHandler Create(IServiceProvider services, string functionName)
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
	}
}
