using GammonX.Lambda.Services;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.Lambda
{
	public static class Startup
	{
		public static IServiceProvider Configure()
		{
			var services = new ServiceCollection();

			// Register services
			services.AddSingleton<IDynamoRepository, DynamoRepository>();

			return services.BuildServiceProvider();
		}
	}
}
