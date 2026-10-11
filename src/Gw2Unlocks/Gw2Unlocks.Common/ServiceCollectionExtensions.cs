using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Gw2Unlocks.Common;

public static class ServiceCollectionExtensions
{
    public static ILoggingBuilder SetupLogging(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(loggingBuilder);
        ArgumentNullException.ThrowIfNull(configuration);

        // Code defaults (LoggingDefaults) with per-project Logging:LogLevel config layered on top;
        // any key present in configuration wins over the default, and config may add new keys.
        var logLevels = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in LoggingDefaults.DefaultLogLevel)
            logLevels[pair.Key] = pair.Value;

        foreach (var pair in configuration.GetSection("Logging:LogLevel").GetChildren())
        {
            if (Enum.TryParse(pair.Value, ignoreCase: true, out LogLevel parsedLevel) && Enum.IsDefined(parsedLevel))
                logLevels[pair.Key] = parsedLevel;
        }

        // Default to Debug (LoggingDefaults) if not set anywhere
        var defaultLevel = logLevels.TryGetValue("Default", out var configuredDefault)
            ? MapToSerilogLevel(configuredDefault)
            : LogEventLevel.Information;

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

        // Apply category overrides (defaults merged with config)
        foreach (var pair in logLevels)
        {
            if (pair.Key.Equals("Default", StringComparison.OrdinalIgnoreCase))
                continue;

            if (pair.Value == LogLevel.None)
            {
                // None means completely disabled for this category
                loggerConfig.MinimumLevel.Override(pair.Key, LogEventLevel.Fatal);
                loggerConfig.Filter.ByExcluding(e => e.Properties.ContainsKey("SourceContext") &&
                                                     e.Properties["SourceContext"].ToString().Contains(pair.Key, StringComparison.CurrentCultureIgnoreCase));
            }
            else
            {
                loggerConfig.MinimumLevel.Override(pair.Key, MapToSerilogLevel(pair.Value));
            }
        }

        var serilogLogger = loggerConfig.CreateLogger();

        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog(serilogLogger, dispose: true);
        loggingBuilder.SetMinimumLevel(LogLevel.Trace);

        return loggingBuilder;
    }

    private static LogEventLevel MapToSerilogLevel(LogLevel microsoftLevel)
    {
        return microsoftLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            // LogLevel.None is handled by the caller (category disabled);
            // as a base level it also silences everything below Fatal.
            _ => LogEventLevel.Fatal,
        };
    }
}
