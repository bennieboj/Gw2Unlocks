using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Globalization;

namespace Gw2Unlocks.Common;

public static class ServiceCollectionExtensions
{
    public static ILoggingBuilder SetupLogging(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(loggingBuilder);
        ArgumentNullException.ThrowIfNull(configuration);

        // Default to Information if not set
        var defaultLevelString = configuration["Logging:LogLevel:Default"] ?? "Information";
        var defaultLevel = MapToSerilogLevel(defaultLevelString) ?? LogEventLevel.Information;

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(defaultLevel)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "[{Timestamp:dd/MM/yy HH:mm:ss:fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.File(
                path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "dotnet-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                formatProvider: CultureInfo.InvariantCulture,
                outputTemplate: "[{Timestamp:dd/MM/yy HH:mm:ss:fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
            );

        // Apply category overrides from Microsoft-style config
        var logLevels = configuration.GetSection("Logging:LogLevel").GetChildren();
        foreach (var pair in logLevels)
        {
            if (pair.Key.Equals("Default", StringComparison.OrdinalIgnoreCase))
                continue;

            var serilogLevel = MapToSerilogLevel(pair.Value);
            if (serilogLevel is null)
                continue;

            if (pair.Value != null && pair.Value.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                // None means completely disabled for this category
                loggerConfig.MinimumLevel.Override(pair.Key, LogEventLevel.Fatal);
                loggerConfig.Filter.ByExcluding(e => e.Properties.ContainsKey("SourceContext") &&
                                                     e.Properties["SourceContext"].ToString().Contains(pair.Key, StringComparison.CurrentCultureIgnoreCase));
            }
            else
            {
                loggerConfig.MinimumLevel.Override(pair.Key, serilogLevel.Value);
            }
        }

        var serilogLogger = loggerConfig.CreateLogger();

        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog(serilogLogger, dispose: true);
        loggingBuilder.SetMinimumLevel(LogLevel.Trace);

        return loggingBuilder;
    }

    private static LogEventLevel? MapToSerilogLevel(string? microsoftLevel)
    {
        if (string.IsNullOrWhiteSpace(microsoftLevel))
            return null;

        return microsoftLevel.ToUpperInvariant() switch
        {
            "TRACE" => LogEventLevel.Verbose,
            "DEBUG" => LogEventLevel.Debug,
            "INFORMATION" => LogEventLevel.Information,
            "WARNING" => LogEventLevel.Warning,
            "ERROR" => LogEventLevel.Error,
            "CRITICAL" => LogEventLevel.Fatal,
            "NONE" => null, // handled separately
            _ => null
        };
    }
}
