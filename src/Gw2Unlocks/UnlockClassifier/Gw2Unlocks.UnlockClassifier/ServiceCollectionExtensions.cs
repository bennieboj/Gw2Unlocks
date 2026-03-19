using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.UnlockClassifier;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUpdater(this IServiceCollection services)
    {
        services.AddSingleton<IClassifier, Classifier>();
        return services;
    }
}