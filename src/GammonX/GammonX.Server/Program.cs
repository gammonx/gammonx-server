using GammonX.Engine.Services;

using GammonX.Server;
using GammonX.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddSingleton<SimpleMatchmakingService>();
builder.Services.AddSingleton<MatchSessionRepository>();
builder.Services.AddSingleton<IMatchSessionFactory, MatchSessionFactory>();
builder.Services.AddSingleton<IGameSessionFactory, GameSessionFactory>();
builder.Services.AddSingleton<IDiceServiceFactory, DiceServiceFactory>();

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