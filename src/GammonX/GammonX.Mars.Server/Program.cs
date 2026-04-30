using DotNetEnv;

using GammonX.Mars.Server;
using GammonX.Mars.Server.NN;
using GammonX.Mars.Server.Services;
using GammonX.Mars.Server.Services.NN;
using GammonX.Models.Enums;

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
// GAME SERVICE SETUP
// -------------------------------------------------------------------------------
builder.Services.Configure<ServiceOptions>(
    builder.Configuration.GetSection("SERVICE"));
// -------------------------------------------------------------------------------
// DEPENDENCY INJECTION
// -------------------------------------------------------------------------------
builder.Services.AddKeyedSingleton<IFeatureEvalService, PlakotoFeatureEvalService>(GameModus.Plakoto);
builder.Services.AddKeyedSingleton<IFeatureEvalService, FevgaFeatureEvalService>(GameModus.Fevga);
builder.Services.AddKeyedSingleton<IFeatureVectorExtractor, PlakotoFeatureVectorExtractor>(GameModus.Plakoto);
builder.Services.AddKeyedSingleton<IFeatureVectorExtractor, FevgaFeatureVectorExtractor>(GameModus.Fevga);
builder.Services.AddKeyedSingleton<INeuralEvalService>(GameModus.Plakoto, (_, _) => NeuralEvalService.LoadEmbedded(GameModus.Plakoto));
builder.Services.AddKeyedSingleton<INeuralEvalService>(GameModus.Fevga, (_, _) => NeuralEvalService.LoadEmbedded(GameModus.Fevga));
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
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
// -------------------------------------------------------------------------------
// CORE SETUP
// -------------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
var app = builder.Build();
// -------------------------------------------------------------------------------
// ROUTING SETUP
// -------------------------------------------------------------------------------
var basePath = app.Services.GetRequiredService<IOptions<ServiceOptions>>().Value.BasePath;
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
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();