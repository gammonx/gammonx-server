using DotNetEnv;

using GammonX.Mars.NN.Services;

using GammonX.Mars.Server;

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
builder.Services.AddKeyedSingleton<IFeatureEvalService, DefaultFeatureEvalService>(GameModus.Backgammon, (serviceProvider, _) => new DefaultFeatureEvalService(serviceProvider, GameModus.Backgammon));
builder.Services.AddKeyedSingleton<IFeatureEvalService, DefaultFeatureEvalService>(GameModus.Tavla, (serviceProvider, _) => new DefaultFeatureEvalService(serviceProvider, GameModus.Tavla));
builder.Services.AddKeyedSingleton<IFeatureEvalService, DefaultFeatureEvalService>(GameModus.Portes, (serviceProvider, _) => new DefaultFeatureEvalService(serviceProvider, GameModus.Portes));
builder.Services.AddKeyedSingleton<IFeatureVectorExtractor, PlakotoFeatureVectorExtractor>(GameModus.Plakoto);
builder.Services.AddKeyedSingleton<IFeatureVectorExtractor, FevgaFeatureVectorExtractor>(GameModus.Fevga);
builder.Services.AddKeyedSingleton<IFeatureVectorExtractor, DefaultFeatureVectorExtractor>(GameModus.Backgammon);
builder.Services.AddKeyedSingleton<IFeatureVectorExtractor, DefaultFeatureVectorExtractor>(GameModus.Tavla);
builder.Services.AddKeyedSingleton<IFeatureVectorExtractor, DefaultFeatureVectorExtractor>(GameModus.Portes);
// The given max batch size for neural network inference was evaluated, test and set to 32
// Consider "../../GammonX.Mars.NN.Tests/Performance/**" for performance evaluation of different batch sizes.
const int maxInferenceBatchSize = 32;
// -------------------------------------------------------------------------------
// NEURAL EVAL SERVICE SETUP
// TorchSharp configuration
// - maxBatchSize: the maximum number of positions to process in a single batch
// - OMP_NUM_THREADS: the number of threads to use for parallel processing
// - MKL_NUM_THREADS: the number of threads to use for MKL parallel processing
// -------------------------------------------------------------------------------
var plakotoInference = BatchedNeuralEvalService.LoadEmbedded(typeof(Program).Assembly, GameModus.Plakoto, maxInferenceBatchSize);
if (plakotoInference != null)
{
    builder.Services.AddKeyedSingleton<INeuralEvalService>(GameModus.Plakoto, (_, _) => plakotoInference);
    builder.Services.AddSingleton((IHostedService)plakotoInference);
}
var fevgaInference = BatchedNeuralEvalService.LoadEmbedded(typeof(Program).Assembly, GameModus.Fevga, maxInferenceBatchSize);
if (fevgaInference != null)
{
    builder.Services.AddKeyedSingleton<INeuralEvalService>(GameModus.Fevga, (_, _) => fevgaInference);
    builder.Services.AddSingleton((IHostedService)fevgaInference);
}
var backgammonInference = BatchedNeuralEvalService.LoadEmbedded(typeof(Program).Assembly, GameModus.Backgammon, maxInferenceBatchSize);
if (backgammonInference != null)
{
    builder.Services.AddKeyedSingleton<INeuralEvalService>(GameModus.Backgammon, (_, _) => backgammonInference);
    builder.Services.AddSingleton((IHostedService)backgammonInference);
}
var tavlaInference = BatchedNeuralEvalService.LoadEmbedded(typeof(Program).Assembly, GameModus.Tavla, maxInferenceBatchSize);
if (tavlaInference != null)
{
    builder.Services.AddKeyedSingleton<INeuralEvalService>(GameModus.Tavla, (_, _) => tavlaInference);
    builder.Services.AddSingleton((IHostedService)tavlaInference);
}
var portesInference = BatchedNeuralEvalService.LoadEmbedded(typeof(Program).Assembly, GameModus.Portes, maxInferenceBatchSize);
if (portesInference != null)
{
    builder.Services.AddKeyedSingleton<INeuralEvalService>(GameModus.Portes, (_, _) => portesInference);
    builder.Services.AddSingleton((IHostedService)portesInference);
}
// -------------------------------------------------------------------------------
// LOGGING SETUP
// -------------------------------------------------------------------------------
builder.Host.UseSerilog((_, _, configuration) =>
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