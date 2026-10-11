using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Gw2Unlocks.Common;

/// <summary>
/// Central default log levels in Microsoft <see cref="LogLevel"/> form.
/// Every host inherits these via <see cref="ServiceCollectionExtensions.SetupLogging"/>;
/// a project can override any key through the <c>Logging:LogLevel</c> section of its configuration.
/// </summary>
public static class LoggingDefaults
{
    public static IReadOnlyDictionary<string, LogLevel> DefaultLogLevel { get; } =
        new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase)
        {
            ["Default"] = LogLevel.Debug,
            ["Microsoft"] = LogLevel.Warning,
            ["Microsoft.AspNetCore.Hosting"] = LogLevel.Warning,
            ["Microsoft.AspNetCore.Routing"] = LogLevel.Warning,
            ["Microsoft.Extensions.Http.Logging"] = LogLevel.Warning,
            ["Microsoft.Hosting.Lifetime"] = LogLevel.Information,
            ["Polly"] = LogLevel.Critical,
            ["System"] = LogLevel.Warning,
            ["System.Net.Http.HttpClient"] = LogLevel.Warning,
            ["System.Net.Http"] = LogLevel.Warning,
        };
}