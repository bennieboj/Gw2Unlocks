using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.CacheUpdater;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUpdater(this IServiceCollection services)
    {
        services.AddSingleton<IUpdater, Updater>()
                .AddHostedService<UpdaterService>();
        return services;
    }
}