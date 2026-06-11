using Microsoft.Extensions.DependencyInjection;

namespace Gw2Unlocks.IconSpriteSheet.Cache;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIconSpriteSheetCache(this IServiceCollection services)
    {
        services.AddSingleton<IIconSpriteSheetCache, IconSpriteSheetCache>();
        return services;
    }
}