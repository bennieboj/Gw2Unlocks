using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.UnlockClassifier;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClassifier(this IServiceCollection services)
    {
        services.AddSingleton<IClassifier, Classifier>()
                .AddSingleton<IClassifierCache, ClassifierCache>()
                .AddHostedService<ClassifierService>();
        return services;
    }
}