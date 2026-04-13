using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.WikiProcessing.Cache;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJsonCacheWikiProcessingSource(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiProcessingSource, Gw2WikiProcessingJsonCache>();
        return services;
    }
    public static IServiceCollection AddJsonCacheWikiProcessing(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiProcessingCache, Gw2WikiProcessingJsonCache>();
        return services;
    }
}