using DotNetEnv;

using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server;
using GammonX.Server.Bot;
using GammonX.Server.Extensions;
using GammonX.Server.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using Serilog;

using System.Text;

// -------------------------------------------------------------------------------
// ENVIRONMENT SETUP
// -------------------------------------------------------------------------------
var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
if (!isDocker)
{
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
}
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
// -------------------------------------------------------------------------------
// LOGGING SETUP
// -------------------------------------------------------------------------------
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .WriteTo.Console();
});
// -------------------------------------------------------------------------------
// GAME SERVICE SETUP
// -------------------------------------------------------------------------------
builder.Services.Configure<GameServiceOptions>(
    builder.Configuration.GetSection("GAME_SERVICE"));
// -------------------------------------------------------------------------------
// WORK QUEUE SETUP
// -------------------------------------------------------------------------------
builder.Services.AddWorkQueueServices(builder.Configuration.GetSection("WORK_QUEUE"));
// -------------------------------------------------------------------------------
// API GATEWAY SETUP
// -------------------------------------------------------------------------------
builder.Services.AddRepositoryServices(builder.Configuration.GetSection("REPOSITORY"));
// -------------------------------------------------------------------------------
// DEPENDENCY INJECTION
// -------------------------------------------------------------------------------
builder.Services.AddKeyedSingleton<IMatchmakingService, NormalMatchmakingService>(MatchModus.Normal);
builder.Services.AddKeyedSingleton<IMatchmakingService, BotMatchmakingService>(MatchModus.Bot);
builder.Services.AddKeyedSingleton<IMatchmakingService, RankedMatchmakingService>(MatchModus.Ranked);
builder.Services.AddKeyedSingleton<IMatchmakingService, UnknownMatchmakingService>(MatchModus.Unknown);
builder.Services.AddSingleton<IMatchmakingService, CompositeMatchmakingService>();
builder.Services.AddHostedService<RankedMatchmakingWorker>();
builder.Services.AddHostedService<NormalMatchmakingWorker>();
builder.Services.AddSingleton<MatchSessionRepository>();
builder.Services.AddSingleton<PlayerConnectionRepository>();
builder.Services.AddSingleton<IMatchSessionFactory, MatchSessionFactory>();
builder.Services.AddSingleton<IGameSessionFactory, GameSessionFactory>();
builder.Services.AddSingleton<IDiceServiceFactory, DiceServiceFactory>();
builder.Services.AddSingleton<ICancellationTokenService, CancellationTokenServiceImpl>();
// -------------------------------------------------------------------------------
// BOT SERVICE SETUP
// -------------------------------------------------------------------------------
builder.Services.Configure<BotServiceOptions>(
    builder.Configuration.GetSection("BOT_SERVICE"));

builder.Services.AddHttpClient<IBotService, WildbgBotService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<BotServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});
// -------------------------------------------------------------------------------
// AUTHENTICATION + AUTHORIZATION SETUP
// -------------------------------------------------------------------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
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
        OnAuthenticationFailed = context =>
        {
            // we allow invalid JWT token for now
            Log.Warning("JWT authentication failed: {Exception}", context.Exception?.Message);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            // we make auth for now optional
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy("OptionalJwt", new AuthorizationPolicyBuilder()
        .RequireAssertion(context => true)
        .Build());
// -------------------------------------------------------------------------------
// CORS SETUP
// -------------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

var app = builder.Build();
// -------------------------------------------------------------------------------
// ROUTING SETUP
// -------------------------------------------------------------------------------
var basePath = app.Services.GetRequiredService<IOptions<GameServiceOptions>>().Value.BasePath;
app.UsePathBase(basePath);
app.Use((context, next) =>
{
    context.Request.PathBase = basePath;
    return next();
});
// -------------------------------------------------------------------------------
// APP CONFIGURATION
// -------------------------------------------------------------------------------
// signalR hubs
app.MapHub<MatchLobbyHub>("/matchhub");
// health check
app.MapHealthChecks("/health");
// configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseAuthentication();
app.MapControllers();

Log.Information("SERILOG LOGLEVEL: {SerilogLogLevel}", Environment.GetEnvironmentVariable("LOG_LEVEL__DEFAULT"));
Log.Information("ASPNETCORE LOGLEVEL: {AspNetCoreLogLevel}", Environment.GetEnvironmentVariable("LOG_LEVEL__MICROSOFTASPNETCORE"));
Log.Information("BOT SERVICE URL: {BotServiceUrl}", Environment.GetEnvironmentVariable("BOT_SERVICE__BASEURL"));
Log.Information("BOT SERVICE TIMEOUT: {BotServiceTimeout}s", Environment.GetEnvironmentVariable("BOT_SERVICE__TIMEOUTSECONDS"));
Log.Information("GAME SERVICE BASEPATH: {GameServiceBasePath}", Environment.GetEnvironmentVariable("GAME_SERVICE__BASEPATH"));

app.Run();

// used for webapplication factory tests
public partial class Program { }