using GuildWars2;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Reflection.Metadata;

namespace Gw2Unlocks.Api.Testing;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddGw2ClientForTesting<THandler>(this IServiceCollection services) where THandler : HttpMessageHandler
    {
        services.AddTransient<THandler>();

        var builder = services.AddGw2Client()
                              .ConfigurePrimaryHttpMessageHandler<THandler>();
        return builder;
    }
}
