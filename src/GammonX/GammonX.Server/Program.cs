using DotNetEnv;

using GammonX.Engine.Services;

using GammonX.Server;
using GammonX.Server.Analysis;
using GammonX.Server.Bot;
using GammonX.Server.EntityFramework;
using GammonX.Server.EntityFramework.Services;
using GammonX.Server.Models;
using GammonX.Server.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using Serilog;

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

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
// -------------------------------------------------------------------------------
// GAME SERVICE SETUP
// -------------------------------------------------------------------------------
builder.Services.Configure<GameServiceOptions>(
	builder.Configuration.GetSection("GAME_SERVICE"));
// -------------------------------------------------------------------------------
// DATABASE SETUP
// -------------------------------------------------------------------------------
builder.Services.Configure<DatabaseOptions>(
	builder.Configuration.GetSection("DATABASE"));

builder.Services.AddDbContext<GammonXDbContext>((sp, dbContextBuilder)=>
{
	var options = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
	dbContextBuilder.UseNpgsql(options.ConnectionString);
});
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPlayerRepository, PlayerRepositoryImpl>();
builder.Services.AddScoped<IPlayerService, PlayerServiceImpl>();
builder.Services.AddScoped<IMatchRepository, MatchRepositoryImpl>();
builder.Services.AddScoped<IMatchService, MatchServiceImpl>();
// -------------------------------------------------------------------------------
// DEPENDENCY INJECTION
// -------------------------------------------------------------------------------
builder.Services.AddKeyedSingleton<IMatchmakingService, NormalMatchmakingService>(WellKnownMatchModus.Normal);
builder.Services.AddKeyedSingleton<IMatchmakingService, BotMatchmakingService>(WellKnownMatchModus.Bot);
builder.Services.AddKeyedSingleton<IMatchmakingService, RankedMatchmakingService>(WellKnownMatchModus.Ranked);
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
// LOGGING SETUP
// -------------------------------------------------------------------------------
builder.Host.UseSerilog((context, services, configuration) =>
{
	configuration
		.Enrich.FromLogContext()
		.WriteTo.Console();
});
// -------------------------------------------------------------------------------
// CORS SETUP
// -------------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy
			.AllowAnyOrigin() // TODO
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

app.Run();

// used for webapplication factory tests
public partial class Program { }