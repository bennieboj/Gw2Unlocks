using Gw2Unlocks.WikiProcessing;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.WikiProcessing.Implementation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWikiGraphSource(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiProcessingSource, Gw2WikiProcessingSource>();
        return services;
    }

    public static IServiceCollection AddUpdater(this IServiceCollection services)
    {
        services.AddHostedService<UpdaterService>();
        return services;
    }
}