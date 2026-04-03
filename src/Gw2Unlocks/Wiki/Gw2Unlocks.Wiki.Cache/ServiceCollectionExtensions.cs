using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.Wiki.Cache;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCacheWikiAsSource(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiSource, Gw2WikiCache>();
        return services;
    }
    public static IServiceCollection AddCacheWikiAsCache(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiCache, Gw2WikiCache>();
        return services;
    }
}