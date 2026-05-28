using DotNetEnv;

using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server;
using GammonX.Server.Bot;
using GammonX.Server.Extensions;
using GammonX.Server.Services;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

using Serilog;

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

builder.Services.AddHttpClient(WellKnownBotServices.WildBg, (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<BotServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.WildBg);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});
builder.Services.AddHttpClient(WellKnownBotServices.Mars, (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<BotServiceOptions>>().Value;
    client.BaseAddress = new Uri(options.Mars);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});
builder.Services.AddKeyedSingleton<IBotService>(WellKnownBotServices.WildBg, (sp, _) =>
    new WildbgBotService(sp.GetRequiredService<IHttpClientFactory>().CreateClient(WellKnownBotServices.WildBg)));
builder.Services.AddKeyedSingleton<IBotService>(WellKnownBotServices.Mars, (sp, _) =>
    new MarsBotService(sp.GetRequiredService<IHttpClientFactory>().CreateClient(WellKnownBotServices.Mars)));
// -------------------------------------------------------------------------------
// AUTHENTICATION + AUTHORIZATION SETUP
// -------------------------------------------------------------------------------
builder.Services.AddAuthenticationConfig();
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
// -------------------------------------------------------------------------------
// CORE SETUP
// -------------------------------------------------------------------------------
// we trust X-Forwarded-* headers from CloudFront/ALB so Request. Scheme reflects the original https scheme.
// without this, UseHttpsRedirection can issue 307s that break the SignalR WebSocket upgrade.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks();
var app = builder.Build();

// we validate bot service URLs eagerly so a missing env var surfaces at startup rather than mid-game when the first bot move is requested.
var botOptions = app.Services.GetRequiredService<IOptions<BotServiceOptions>>().Value;
if (string.IsNullOrEmpty(botOptions.WildBg))
    throw new InvalidOperationException("BOT_SERVICE__WILDBG is not configured. Set the environment variable before starting the server.");
if (string.IsNullOrEmpty(botOptions.Mars))
    throw new InvalidOperationException("BOT_SERVICE__MARS is not configured. Set the environment variable before starting the server.");
// -------------------------------------------------------------------------------
// ROUTING SETUP
// -------------------------------------------------------------------------------

// we must run before any middleware that inspects scheme/host (HTTPS redirect, auth, redirect URI generation).
app.UseForwardedHeaders();

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
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<MatchLobbyHub>("/matchhub");
app.MapHealthChecks("/health");
app.MapControllers();

Log.Information("SERILOG LOGLEVEL: {SerilogLogLevel}", Environment.GetEnvironmentVariable("LOG_LEVEL__DEFAULT"));
Log.Information("ASPNETCORE LOGLEVEL: {AspNetCoreLogLevel}", Environment.GetEnvironmentVariable("LOG_LEVEL__MICROSOFTASPNETCORE"));
Log.Information("WILDBG BOT SERVICE URL: {BotServiceUrl}", Environment.GetEnvironmentVariable("BOT_SERVICE__WILDBG"));
Log.Information("MARS BOT SERVICE URL: {BotServiceUrl}", Environment.GetEnvironmentVariable("BOT_SERVICE__MARS"));
Log.Information("BOT SERVICE TIMEOUT: {BotServiceTimeout}s", Environment.GetEnvironmentVariable("BOT_SERVICE__TIMEOUTSECONDS"));
Log.Information("GAME SERVICE BASEPATH: {GameServiceBasePath}", Environment.GetEnvironmentVariable("GAME_SERVICE__BASEPATH"));

app.Run();

// we require this for webapplication factory tests
public partial class Program { }