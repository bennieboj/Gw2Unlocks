using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.Wiki.Testing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFakeWikiSourceSuccess(this IServiceCollection services)
    {
        services.AddSingleton<Gw2WikiSuccessResponseFake>();
        services.AddSingleton<IGw2WikiSource>(sp => sp.GetRequiredService<Gw2WikiSuccessResponseFake>());
        return services;
    }
    public static IServiceCollection AddFakeWikiCacheSuccess(this IServiceCollection services)
    {
        services.AddSingleton<Gw2WikiSuccessResponseFake>();
        services.AddSingleton<IGw2WikiCache>(sp => sp.GetRequiredService<Gw2WikiSuccessResponseFake>());
        return services;
    }
}
