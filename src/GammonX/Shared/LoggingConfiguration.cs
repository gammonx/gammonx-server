using Serilog;
using Serilog.Events;

namespace GammonX.Hosting;

internal static class LoggingConfiguration
{
    internal static LoggerConfiguration ApplyLogLevels(this LoggerConfiguration logger, IConfiguration configuration)
    {
        var defaultLevel = ReadLevel(configuration, "LOG_LEVEL:DEFAULT", LogEventLevel.Information);
        var aspNetCoreLevel = ReadLevel(configuration, "LOG_LEVEL:MICROSOFTASPNETCORE", defaultLevel);
        return logger.MinimumLevel.Is(defaultLevel).MinimumLevel.Override("Microsoft.AspNetCore", aspNetCoreLevel);
    }

    private static LogEventLevel ReadLevel(IConfiguration configuration, string key, LogEventLevel fallback)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (!Enum.TryParse<LogEventLevel>(value, true, out var level) || !Enum.IsDefined(level))
        {
            throw new InvalidOperationException($"Configuration '{key}' must be a valid Serilog log level.");
        }

        return level;
    }
}
