using GammonX.Lambda.Handlers;
using GammonX.Lambda.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GammonX.Lambda
{
	public static class Startup
	{
		private static IServiceProvider? _provider;

		public static IServiceProvider Configure()
		{
			if (_provider != null)
				return _provider;

			var services = new ServiceCollection();

			services.AddSingleton<IDynamoRepository, DynamoRepository>();
			services.AddKeyedTransient<ISqsLambdaHandler, MatchCompletedHandler>(LambdaFunctions.MatchCompletedFunc);
			services.AddKeyedTransient<ISqsLambdaHandler, GameCompletedHandler>(LambdaFunctions.GameCompletedFunc);
			services.AddKeyedTransient<ISqsLambdaHandler, PlayerRatingUpdatedHandler>(LambdaFunctions.PlayerRatingUpdatedFunc);
			services.AddKeyedTransient<ISqsLambdaHandler, PlayerStatsUpdatedHandler>(LambdaFunctions.PlayerStatsUpdatedFunc);

			_provider = services.BuildServiceProvider();
			return _provider;
		}
	}
}
