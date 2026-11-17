using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Xunit;

namespace GammonX.Hosting.Tests;

public class LoggingConfigurationTests
{
    [Theory]
    [InlineData("Error", "Error", LogEventLevel.Error, LogEventLevel.Error)]
    [InlineData("Verbose", "Verbose", LogEventLevel.Verbose, LogEventLevel.Verbose)]
    [InlineData("Error", "Verbose", LogEventLevel.Error, LogEventLevel.Verbose)]
    [InlineData("debug", "Warning", LogEventLevel.Debug, LogEventLevel.Warning)]
    [InlineData(null, null, LogEventLevel.Information, LogEventLevel.Information)]
    [InlineData("Warning", null, LogEventLevel.Warning, LogEventLevel.Warning)]
    public void EnvFileLevelsControlApplicationAndAspNetCoreLogging(
        string? application, string? framework, LogEventLevel expectedApplication, LogEventLevel expectedFramework)
    {
        // Arrange: unique keys avoid changing the environment used by other host tests.
        var prefix = $"LoggingTest_{Guid.NewGuid():N}_";
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllLines(path, new[]
            {
                $"{prefix}LOG_LEVEL__DEFAULT={application}",
                $"{prefix}LOG_LEVEL__MICROSOFTASPNETCORE={framework}"
            });
            Env.Load(path);
            var configuration = new ConfigurationBuilder().AddEnvironmentVariables(prefix).Build();

            // Act
            using var logger = new LoggerConfiguration().ApplyLogLevels(configuration).CreateLogger();
            var aspNetCore = logger.ForContext("SourceContext", "Microsoft.AspNetCore.Routing.EndpointMiddleware");
            var applicationLogger = logger.ForContext("SourceContext", "GammonX.Application");

            // Assert: framework overrides can also be more verbose than the default.
            foreach (var level in Enum.GetValues<LogEventLevel>())
            {
                Assert.Equal(level >= expectedApplication, logger.IsEnabled(level));
                Assert.Equal(level >= expectedApplication, applicationLogger.IsEnabled(level));
                Assert.Equal(level >= expectedFramework, aspNetCore.IsEnabled(level));
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "LOG_LEVEL__DEFAULT", null);
            Environment.SetEnvironmentVariable(prefix + "LOG_LEVEL__MICROSOFTASPNETCORE", null);
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("LOG_LEVEL:DEFAULT", "misspelled")]
    [InlineData("LOG_LEVEL:MICROSOFTASPNETCORE", "999")]
    public void InvalidLevelsAreRejected(string key, string value)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = value }).Build();

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => new LoggerConfiguration().ApplyLogLevels(configuration));

        // Assert
        Assert.Contains(key, error.Message);
    }
}
