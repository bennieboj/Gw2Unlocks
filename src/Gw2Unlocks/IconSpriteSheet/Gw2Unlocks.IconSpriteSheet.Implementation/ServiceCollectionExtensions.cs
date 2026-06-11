using Microsoft.Extensions.DependencyInjection;
using Polly;
using System;
using System.Net.Http;

namespace Gw2Unlocks.IconSpriteSheet.Implementation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIconSpriteSheetGenerator(this IServiceCollection services)
    {
        services.AddSingleton<IIconSpriteSheetGenerator, IconSpriteSheetGenerator>();
        return services;
    }
    public static IHttpClientBuilder AddIconSpriteSheetGeneratorHttpClient(this IServiceCollection services)
    {        
        return services.AddHttpClient<IIconSpriteSheetGenerator, IconSpriteSheetGenerator>()
                .AddPolicyHandler(
                    Policy.Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(3, retry => TimeSpan.FromMilliseconds(200))
                );
    }


}
