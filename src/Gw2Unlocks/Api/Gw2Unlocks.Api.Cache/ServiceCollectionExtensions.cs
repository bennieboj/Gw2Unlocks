using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.Api.Cache;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJsonCacheApiSource(this IServiceCollection services)
    {
        services.AddSingleton<IGw2ApiSource, Gw2ApiJsonCache>();
        return services;
    }
    public static IServiceCollection AddJsonCacheApi(this IServiceCollection services)
    {
        services.AddSingleton<IGw2ApiCache, Gw2ApiJsonCache>();
        return services;
    }
}