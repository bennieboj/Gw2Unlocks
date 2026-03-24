using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.Wiki.Cache;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJsonCacheWikiSource(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiSource, Gw2WikiJsonCache>();
        return services;
    }
    public static IServiceCollection AddJsonCacheWiki(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiCache, Gw2WikiJsonCache>();
        return services;
    }
}