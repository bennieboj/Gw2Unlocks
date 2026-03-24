using Gw2Unlocks.Wiki.WikiApi.Implementation;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.Wiki.Implementation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWikiSource(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiSource, Gw2WikiSource>()
                .AddRealWikiApi();
        return services;
    }

}