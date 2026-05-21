using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.UnlockClassifier.Implementation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClassifier(this IServiceCollection services)
    {
        services.AddSingleton<IClassifier, Classifier>()
                .AddHostedService<ClassifierService>();
        return services;
    }
}