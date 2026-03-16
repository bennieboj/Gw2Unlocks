using GuildWars2;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace Gw2Unlocks.Gw2SDK.Testing;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder AddFakeGw2Client(this IServiceCollection services)
    {
        services.AddTransient<FakeGw2Handler>();

        var builder = services.AddGw2Client()
                              .ConfigurePrimaryHttpMessageHandler<FakeGw2Handler>();
        return builder;
    }
}
