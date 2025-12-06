using DotNetEnv;

using GammonX.Engine.Services;

using GammonX.Models.Enums;

using GammonX.Server;
using GammonX.Server.Analysis;
using GammonX.Server.Bot;
using GammonX.Server.Services;

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
builder.Services.AddSingleton<IMatchSessionFactory, MatchSessionFactory>();
builder.Services.AddSingleton<IGameSessionFactory, GameSessionFactory>();
builder.Services.AddSingleton<IDiceServiceFactory, DiceServiceFactory>();
// -------------------------------------------------------------------------------
// MATCH ANALYSIS
// -------------------------------------------------------------------------------
builder.Services.AddSingleton<IMatchAnalysisQueue, MatchAnalysisQueue>();
builder.Services.AddScoped<IMatchAnalysisService, MatchAnalysisService>();
builder.Services.AddHostedService<MatchAnalysisWorker>();
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
app.MapControllers();

Log.Information("SERILOG LOGLEVEL: {SerilogLogLevel}", Environment.GetEnvironmentVariable("LOG_LEVEL__DEFAULT"));
Log.Information("ASPNETCORE LOGLEVEL: {AspNetCoreLogLevel}", Environment.GetEnvironmentVariable("LOG_LEVEL__MICROSOFTASPNETCORE"));
Log.Information("BOT SERVICE URL: {BotServiceUrl}", Environment.GetEnvironmentVariable("BOT_SERVICE__BASEURL"));
Log.Information("BOT SERVICE TIMEOUT: {BotServiceTimeout}s", Environment.GetEnvironmentVariable("BOT_SERVICE__TIMEOUTSECONDS"));
Log.Information("GAME SERVICE BASEPATH: {GameServiceBasePath}", Environment.GetEnvironmentVariable("GAME_SERVICE__BASEPATH"));

app.Run();

// used for webapplication factory tests
public partial class Program { }