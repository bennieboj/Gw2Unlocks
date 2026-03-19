using Gw2Unlocks.Cache.Contract;
using Gw2Unlocks.Cache.SqlLite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Gw2Unlocks.Cache.Testing;

public static class ServiceCollectionExtensions
{
    // In-memory SQLite for testing
    public static IServiceCollection AddInMemoryGw2Cache(this IServiceCollection services)
    {
        services.AddSingleton<IGw2Cache>(sp => {
            var logger = sp.GetRequiredService<ILogger<SqlLiteCache>>();
            return new SqlLiteCache(":memory:", logger);
        });
        return services;
    }
}