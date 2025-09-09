using GammonX.Engine.Services;

using GammonX.Server;
using GammonX.Server.Bot;
using GammonX.Server.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BotServiceOptions>(
	builder.Configuration.GetSection("BotService"));

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddSingleton<IMatchmakingService, SimpleMatchmakingService>();
builder.Services.AddSingleton<MatchSessionRepository>();
builder.Services.AddSingleton<IMatchSessionFactory, MatchSessionFactory>();
builder.Services.AddSingleton<IGameSessionFactory, GameSessionFactory>();
builder.Services.AddSingleton<IDiceServiceFactory, DiceServiceFactory>();

builder.Services.AddHttpClient<IBotService, WildbgBotService>((sp, client) =>
{
	var options = sp.GetRequiredService<IOptions<BotServiceOptions>>().Value;
	client.BaseAddress = new Uri(options.BaseUrl);
	client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

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

var app = builder.Build();

app.MapHub<MatchLobbyHub>("/matchhub");

// configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// used for webapplication factory tests
public partial class Program { }