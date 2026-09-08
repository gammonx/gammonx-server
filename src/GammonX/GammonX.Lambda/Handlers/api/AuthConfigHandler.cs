using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using GammonX.Models.Contracts;

namespace GammonX.Lambda.Handlers
{
    /// <summary>
    /// GET /auth/config > Lambda: AuthConfigHandler
    /// </summary>
    /// <remarks>
    /// Unlike the other api handlers this one touches no DynamoDb, so it does not
    /// derive from <see cref="LambdaHandlerBaseImpl"/> and needs no DI services.
    /// It only echoes the COGNITO_* environment variables that
    /// components/lambda/main.tf sets on the function.
    /// </remarks>
    public class AuthConfigHandler : IApiLambdaHandler
    {
        internal const string UserPoolIdVariable = "COGNITO_USER_POOL_ID";

        internal const string ClientIdVariable = "COGNITO_CLIENT_ID";

        // <inheritdoc />
        public Task<BaseResponseContract?> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var userPoolId = Environment.GetEnvironmentVariable(UserPoolIdVariable);
            var clientId = Environment.GetEnvironmentVariable(ClientIdVariable);

            // we fail loudly rather than answering with empty strings. A 200 carrying
            // a blank pool id leaves the client building an unusable CognitoUserPool
            // and failing much later with an opaque error.
            if (string.IsNullOrEmpty(userPoolId) || string.IsNullOrEmpty(clientId))
            {
                context.Logger.LogError($"Cognito is not configured on this function. '{UserPoolIdVariable}' and '{ClientIdVariable}' must both be set.");
                throw new InvalidOperationException($"'{UserPoolIdVariable}' and '{ClientIdVariable}' must both be set");
            }

            return Task.FromResult<BaseResponseContract?>(new AuthConfigResponseContract()
            {
                UserPoolId = userPoolId,
                ClientId = clientId
            });
        }
    }
}
