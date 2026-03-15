using Gw2Unlocks.Cache.Contract;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.Cache.SqlLite;

public static class ServiceCollectionExtensions
{
    // File-based SQLite for production
    public static IServiceCollection AddSqlLiteGw2Cache(this IServiceCollection services, string path)
    {
        services.AddSingleton<IGw2Cache>(_ => new SqlLiteCache(path));
        return services;
    }
}