using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.WikiProcessing.Cache;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJsonCacheWikiGraphSource(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiGraphSource, Gw2WikiGraphJsonCache>();
        return services;
    }
    public static IServiceCollection AddJsonCacheWikiGraph(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiGraphCache, Gw2WikiGraphJsonCache>();
        return services;
    }
}