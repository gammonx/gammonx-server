using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using GammonX.Lambda.Handlers;

using Microsoft.Extensions.DependencyInjection;

namespace GammonX.Lambda.Services
{
	internal static class LambdaFunctionFactory
	{
		private static readonly HashSet<string> _wellKnownSqsFunctions =
		[
			LambdaFunctions.GameCompletedFunc,
			LambdaFunctions.PlayerRatingUpdatedFunc,
			LambdaFunctions.PlayerStatsUpdatedFunc,
			LambdaFunctions.MatchCompletedFunc,
			LambdaFunctions.PlayerCreatedFunc,
		];

		public static ISqsLambdaHandler CreateSqsHandler(IServiceProvider services, string functionName)
		{
			if (_wellKnownSqsFunctions.Contains(functionName))
				return services.GetRequiredKeyedService<ISqsLambdaHandler>(functionName);

			throw new InvalidOperationException($"unknown lambda function with name '{functionName}'");
		}

		/// <summary>
		/// Initializes an instance of <see cref="IApiLambdaHandler"/> based on the specified
		/// <see cref="APIGatewayProxyRequest.HttpMethod"/> and <see cref="APIGatewayProxyRequest.Path"/>
		/// given in <paramref name="request"/>.
		/// </summary>
		/// <remarks>
		/// If the given API routes is not known. A <c>null</c> value is returned.
		/// </remarks>
		/// <param name="request">API gateway request.</param>
		/// <param name="services">Service provider.</param>
		/// <param name="context">Lambda context for logging.</param>
		/// <returns>An instance of <see cref="IApiLambdaHandler"/>.</returns>
		public static IApiLambdaHandler? CreateApiHandler(APIGatewayProxyRequest request, IServiceProvider services, ILambdaContext context)
		{
			context.Logger.LogInformation($"CreateApiHandler called with HttpMethod: '{request.HttpMethod}', Path: '{request.Path}', Resource: '{request.Resource}'");
			context.Logger.LogInformation($"Available routes: {string.Join(", ", ApiRoutes.Keys.Select(k => $"({k.method} {k.pattern})"))}");

			// we match against the actual path using pattern matching
			foreach (var route in ApiRoutes)
			{
				if (request.HttpMethod == route.Key.method && MatchesPathPattern(request.Path, route.Key.pattern, out var pathParameters))
				{
					context.Logger.LogInformation($"Route matched! Pattern: '{route.Key.pattern}', Handler type: '{route.Value.Name}'");

					// we populate PathParameters if using $default catch-all route
					if (request.PathParameters == null || request.PathParameters.Count == 0)
					{
						request.PathParameters = pathParameters;
						context.Logger.LogInformation($"Extracted path parameters: {string.Join(", ", pathParameters.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
					}

					var handler = services.GetRequiredKeyedService<IApiLambdaHandler>(route.Value);
					context.Logger.LogInformation($"Handler instance created successfully");
					return handler;
				}
			}

			context.Logger.LogWarning($"No matching route found for HttpMethod: '{request.HttpMethod}', Path: '{request.Path}'. Returning null (404).");
			// we return value of null which will result in a 404 HTTP response.
			return null;
		}

		/// <summary>
		/// Checks if a request path matches a route pattern with path parameters.
		/// </summary>
		/// <param name="path">The actual request path (e.g., "/players/123/rating/Backgammon")</param>
		/// <param name="pattern">The route pattern (e.g., "/players/{id}/rating/{variant}")</param>
		/// <param name="pathParameters">Dictionary of extracted path parameters if matched</param>
		/// <returns>True if the path matches the pattern.</returns>
		private static bool MatchesPathPattern(string path, string pattern, out Dictionary<string, string> pathParameters)
		{
			pathParameters = new Dictionary<string, string>();

			if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(pattern))
				return false;

			var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
			var patternSegments = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);

			if (pathSegments.Length != patternSegments.Length)
				return false;

			for (int i = 0; i < pathSegments.Length; i++)
			{
				// if pattern segment is a parameter (e.g., "{id}"), extract it
				if (patternSegments[i].StartsWith('{') && patternSegments[i].EndsWith('}'))
				{
					var paramName = patternSegments[i].Trim('{', '}');
					pathParameters[paramName] = pathSegments[i];
					continue;
				}

				// otherwise, segments must match exactly
				if (pathSegments[i] != patternSegments[i])
					return false;
			}

			return true;
		}

        private static readonly Dictionary<(string method, string pattern), Type> ApiRoutes =
			new()
			{
				{ ("GET", "/players/{id}/rating/{variant}"), typeof(GetPlayerRatingHandler) }
			};
    }
}
