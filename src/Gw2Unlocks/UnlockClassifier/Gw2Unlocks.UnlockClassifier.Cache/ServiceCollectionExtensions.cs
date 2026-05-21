using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.UnlockClassifier.Cache;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClassifierCache(this IServiceCollection services)
    {
        services.AddSingleton<IClassifierCache, ClassifierCache>();
        return services;
    }
}