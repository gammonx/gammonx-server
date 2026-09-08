using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;

using GammonX.Lambda.Handlers;
using GammonX.Lambda.Services;

using GammonX.Models.Contracts;

using Newtonsoft.Json;

using Xunit;

namespace GammonX.Lambda.Tests.Gateway
{
    public class AuthConfigHandlerTests
    {
        private const string UserPoolId = "eu-central-1_TESTPOOL";

        private const string ClientId = "1a2b3c4d5e6f7g8h9i0j";

        [Fact]
        public void AuthConfigRouteResolvesToAuthConfigHandler()
        {
            var context = new TestLambdaContext { Logger = new TestLambdaLogger() };

            var handler = LambdaFunctionFactory.CreateApiHandler(MakeRequest(), Startup.Configure(), context);

            Assert.IsType<AuthConfigHandler>(handler);
        }

        [Fact]
        public async Task AuthConfigReturnsTheConfiguredPoolAndClient()
        {
            using var _ = new EnvironmentScope(UserPoolId, ClientId);

            var context = new TestLambdaContext { Logger = new TestLambdaLogger() };
            var handler = new AuthConfigHandler();

            var result = await handler.HandleAsync(MakeRequest(), context);

            var casted = Assert.IsType<AuthConfigResponseContract>(result);
            Assert.Equal(UserPoolId, casted.UserPoolId);
            Assert.Equal(ClientId, casted.ClientId);
        }

        // The mobile client destructures `userPoolId` / `clientId` verbatim, and
        // Program.cs serializes the contract with Newtonsoft. Guard the wire names
        // so renaming the C# properties cannot silently break sign-in.
        [Fact]
        public async Task AuthConfigSerializesWithLowerCamelCaseMemberNames()
        {
            using var _ = new EnvironmentScope(UserPoolId, ClientId);

            var context = new TestLambdaContext { Logger = new TestLambdaLogger() };
            var handler = new AuthConfigHandler();

            var result = await handler.HandleAsync(MakeRequest(), context);
            var json = JsonConvert.SerializeObject(result);

            Assert.Equal($@"{{""userPoolId"":""{UserPoolId}"",""clientId"":""{ClientId}""}}", json);
        }

        [Theory]
        [InlineData(null, ClientId)]
        [InlineData(UserPoolId, null)]
        [InlineData("", "")]
        public async Task AuthConfigThrowsWhenCognitoIsNotConfigured(string? userPoolId, string? clientId)
        {
            using var _ = new EnvironmentScope(userPoolId, clientId);

            var context = new TestLambdaContext { Logger = new TestLambdaLogger() };
            var handler = new AuthConfigHandler();

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(MakeRequest(), context));
        }

        private static APIGatewayProxyRequest MakeRequest()
        {
            return new APIGatewayProxyRequest
            {
                HttpMethod = "GET",
                Path = "/auth/config"
            };
        }

        /// <summary>
        /// Environment variables are process wide, so every test that touches them
        /// restores the previous values on dispose.
        /// </summary>
        private sealed class EnvironmentScope : IDisposable
        {
            private readonly string? _previousUserPoolId;

            private readonly string? _previousClientId;

            public EnvironmentScope(string? userPoolId, string? clientId)
            {
                _previousUserPoolId = Environment.GetEnvironmentVariable(AuthConfigHandler.UserPoolIdVariable);
                _previousClientId = Environment.GetEnvironmentVariable(AuthConfigHandler.ClientIdVariable);

                Environment.SetEnvironmentVariable(AuthConfigHandler.UserPoolIdVariable, userPoolId);
                Environment.SetEnvironmentVariable(AuthConfigHandler.ClientIdVariable, clientId);
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable(AuthConfigHandler.UserPoolIdVariable, _previousUserPoolId);
                Environment.SetEnvironmentVariable(AuthConfigHandler.ClientIdVariable, _previousClientId);
            }
        }
    }
}
