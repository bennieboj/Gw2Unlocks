using Gw2Unlocks.Cache.Contract;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.Cache;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddGw2Caching(this IHttpClientBuilder builder, Gw2CacheOptions options)
    {
        System.ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddSingleton(options);
        builder.Services.AddTransient<Gw2CacheHandler>();
        return builder.AddHttpMessageHandler<Gw2CacheHandler>();
    }
}