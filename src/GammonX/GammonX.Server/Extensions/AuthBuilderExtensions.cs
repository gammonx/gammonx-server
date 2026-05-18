using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

using Microsoft.IdentityModel.Tokens;

using System.Text;

namespace GammonX.Server.Extensions
{
    public static class AuthBuilderExtensions
    {
        public static void AddAuthenticationConfig(this IServiceCollection services)
        {
            var cognitoUserPoolId = Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID");
            var cognitoClientId = Environment.GetEnvironmentVariable("COGNITO_CLIENT_ID");
            var cognitoRegion = Environment.GetEnvironmentVariable("COGNITO_REGION") ?? "eu-central-1";
            var useCognito = !string.IsNullOrEmpty(cognitoUserPoolId) && !string.IsNullOrEmpty(cognitoClientId);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                if (useCognito)
                {
                    // cognito access tokens have `client_id` (not `aud`) and `token_use=access`.
                    // we validate those in OnTokenValidated; built-in audience validation is off.
                    options.Authority = $"https://cognito-idp.{cognitoRegion}.amazonaws.com/{cognitoUserPoolId}";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = options.Authority
                    };
                }
                else
                {
                    // we fall back to non-validated jwt tokens if cognito is not activated
                    var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "super-secret-key-that-is-at-least-32-characters-long-for-hs256";
                    var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "";
                    var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
                        ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
                        ValidateLifetime = !string.IsNullOrEmpty(jwtSecret),
                        ValidateIssuerSigningKey = !string.IsNullOrEmpty(jwtSecret),
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
                    };
                }

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["token"].FirstOrDefault();
                        var bearerToken = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");

                        if (!string.IsNullOrEmpty(bearerToken))
                        {
                            context.Token = bearerToken;
                        }
                        else if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        if (!useCognito) return Task.CompletedTask;

                        var principal = context.Principal;
                        var tokenUse = principal?.FindFirst("token_use")?.Value;
                        var clientId = principal?.FindFirst("client_id")?.Value;

                        if (tokenUse != "access")
                        {
                            context.Fail($"Expected access token, got token_use={tokenUse ?? "<missing>"}");
                            return Task.CompletedTask;
                        }

                        if (!string.Equals(clientId, cognitoClientId, StringComparison.Ordinal))
                        {
                            context.Fail("client_id does not match expected Cognito app client");
                            return Task.CompletedTask;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        if (!useCognito)
                        {
                            // local dev: suppress the 401 so anonymous connections still work
                            context.HandleResponse();
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            var policy = useCognito
                ? new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build()
                : new AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true)
                    .Build();

            services.AddAuthorizationBuilder()
                .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build())
                .AddPolicy("AuthPolicy", policy);
        }
    }
}
