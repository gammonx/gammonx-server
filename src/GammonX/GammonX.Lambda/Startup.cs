using DotNetEnv;
using DotNetEnv.Configuration;
using GammonX.DynamoDb;
using GammonX.DynamoDb.Extensions;
using GammonX.DynamoDb.Repository;
using GammonX.Lambda.Handlers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
			// -------------------------------------------------------------------------------
			// ENVIRONMENT SETUP
			// -------------------------------------------------------------------------------
			var envLocal = Path.Combine(Directory.GetCurrentDirectory(), ".env.local");
			var env = Path.Combine(Directory.GetCurrentDirectory(), ".env");
			if (File.Exists(envLocal))
			{
				Env.Load(envLocal);
			}
			else if (File.Exists(env))
			{
				Env.Load(env);
			}
			// -------------------------------------------------------------------------------
			// CONFIGURATION SETUP
			// -------------------------------------------------------------------------------
			var configuration = new ConfigurationBuilder()
				.AddEnvironmentVariables()
				.Build();
			services.Configure<DynamoDbOptions>(configuration.GetSection("AWS"));
			// -------------------------------------------------------------------------------
			// SERVICES SETUP
			// -------------------------------------------------------------------------------
			services.AddSingleton<IDynamoRepository, DynamoDbRepository>();
			services.AddKeyedTransient<ISqsLambdaHandler, MatchCompletedHandler>(LambdaFunctions.MatchCompletedFunc);
			services.AddKeyedTransient<ISqsLambdaHandler, GameCompletedHandler>(LambdaFunctions.GameCompletedFunc);
			services.AddKeyedTransient<ISqsLambdaHandler, PlayerRatingUpdatedHandler>(LambdaFunctions.PlayerRatingUpdatedFunc);
			services.AddKeyedTransient<ISqsLambdaHandler, PlayerStatsUpdatedHandler>(LambdaFunctions.PlayerStatsUpdatedFunc);
			// -------------------------------------------------------------------------------
			// DATABASE SETUP
			// -------------------------------------------------------------------------------
			var awsConfig = configuration.GetSection("AWS");
			services.AddConditionalDynamoDb(awsConfig);

			_provider = services.BuildServiceProvider();

			return _provider;
		}
	}
}
