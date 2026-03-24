using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.Wiki.WikiApi.Testing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFakeWikiApi(this IServiceCollection services)
    {
        services.AddSingleton<IWikiApi, FakeWikiApi>();
        return services;
    }

}