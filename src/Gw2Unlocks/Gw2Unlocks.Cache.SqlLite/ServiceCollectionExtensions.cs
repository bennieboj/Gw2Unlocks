using Gw2Unlocks.Cache.Contract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gw2Unlocks.Cache.SqlLite;

public static class ServiceCollectionExtensions
{
    // File-based SQLite for production
    public static IServiceCollection AddSqlLiteGw2Cache(this IServiceCollection services, string path)
    {
        services.AddSingleton<IGw2Cache>(sp => {
            var logger = sp.GetRequiredService<ILogger<SqlLiteCache>>();
            return new SqlLiteCache(path, logger);
        });
        return services;
    }
}