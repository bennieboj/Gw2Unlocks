using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.Api.Testing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFakeApiSourceSuccess(this IServiceCollection services)
    {
        services.AddSingleton<Gw2ApiSuccessResponseFake>();
        services.AddSingleton<IGw2ApiSource>(sp => sp.GetRequiredService<Gw2ApiSuccessResponseFake>());
        return services;
    }
    public static IServiceCollection AddFakeApiCacheSuccess(this IServiceCollection services)
    {
        services.AddSingleton<Gw2ApiSuccessResponseFake>();
        services.AddSingleton<IGw2ApiCache>(sp => sp.GetRequiredService<Gw2ApiSuccessResponseFake>());
        return services;
    }
    public static IServiceCollection AddakeApiSourceTransient(this IServiceCollection services)
    {
        services.AddSingleton<Gw2ApiTransientFailingResponseFake>();
        services.AddSingleton<IGw2ApiSource>(sp => sp.GetRequiredService<Gw2ApiTransientFailingResponseFake>());
        return services;
    }
}