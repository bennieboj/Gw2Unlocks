using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.Wiki.Implementation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWikiSource(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiSource, Gw2WikiSource>();
        return services;
    }

}