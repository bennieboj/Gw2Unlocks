using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.CacheUpdater;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUpdater(this IServiceCollection services)
    {
        services.AddSingleton<IUpdater, Updater>();
        return services;
    }
    public static IHttpClientBuilder AddGw2Caching(this IHttpClientBuilder builder)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddTransient<Gw2CacheHandler>();
        return builder.AddHttpMessageHandler<Gw2CacheHandler>();
    }
}