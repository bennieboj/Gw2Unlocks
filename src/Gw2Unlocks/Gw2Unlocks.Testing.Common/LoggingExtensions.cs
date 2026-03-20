using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Gw2Unlocks.Testing.Common;

public static class LoggingExtensions
{
    public static IServiceCollection AddXunitLogging(
        this IServiceCollection services,
        ITestOutputHelper output,
        LogLevel minimumLevel = LogLevel.Debug)
    {
        services.AddLogging(builder =>
        {
            builder.AddProvider(new XunitLoggerProvider(output));
            builder.SetMinimumLevel(minimumLevel);
        });

        return services;
    }
}