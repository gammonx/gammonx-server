using Amazon.Lambda.APIGatewayEvents;

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

		/// <summary>
		/// Initializes an instance of <see cref="IApiLambdaHandler"/> based on the specified
		/// <see cref="APIGatewayProxyRequest.HttpMethod"/> and <see cref="APIGatewayProxyRequest.Resource"/>
		/// given in <paramref name="request"/>.
		/// </summary>
		/// <remarks>
		/// If the given API routes is not known. A <c>null</c> value is returned.
		/// </remarks>
		/// <param name="request">API gateway request.</param>
		/// <param name="services">Service provider.</param>
		/// <returns>An intance of <see cref="IApiLambdaHandler"/>.</returns>
		public static IApiLambdaHandler? CreateApiHandler(APIGatewayProxyRequest request, IServiceProvider services)
		{
            if (ApiRoutes.TryGetValue((request.HttpMethod, request.Resource), out var handlerType))
            {
                var handler = services.GetRequiredKeyedService<IApiLambdaHandler>(handlerType);
				return handler;
            }

			// a return value of null will result in a 404 HTTP response.
			return null;
		}

        private static readonly Dictionary<(string method, string resource), Type> ApiRoutes =
			new()
			{
				{ ("GET", "/players/{id}/{variant}/rating"), typeof(GetPlayerRatingHandler) }
			};
    }
}
