using GuildWars2;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace Gw2Unlocks.Gw2SDK.Testing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFakeGw2SDK<THttpMessageHandler>(this IServiceCollection services) where THttpMessageHandler : HttpMessageHandler
    {
        services
            .AddTransient<THttpMessageHandler>()
            .AddHttpClient<Gw2Client>()
            .ConfigurePrimaryHttpMessageHandler<THttpMessageHandler>();

        return services;
    }
}
