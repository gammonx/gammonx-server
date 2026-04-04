var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// -------------------------------------------------------------------------------
// APP CONFIGURATION
// -------------------------------------------------------------------------------
var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();
