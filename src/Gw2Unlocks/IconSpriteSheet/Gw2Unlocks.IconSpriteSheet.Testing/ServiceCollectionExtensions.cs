using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.IconSpriteSheet.Testing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFakeIconSpriteSheetCache(this IServiceCollection services)
    {
        services.AddSingleton<IIconSpriteSheetCache, FakeIconSpriteSheetCache>();
        return services;
    }

    public static IServiceCollection AddFakeIconSpriteSheetHttpMessagehandler(this IServiceCollection services)
    {
        services.AddSingleton<FakeHttpMessageHandler>();
        return services;
    }

    public static IHttpClientBuilder AddFakeHttpMessageHandler(this IHttpClientBuilder builder)
    {
        builder.AddHttpMessageHandler<FakeHttpMessageHandler>();
        return builder;
    }
}

internal sealed class FakeHttpMessageHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        if (request.RequestUri != null && request.RequestUri.ToString().Contains("fail", System.StringComparison.OrdinalIgnoreCase))
        {
            response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
        byte[] pngBytes =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
            0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
            0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44,
            0xAE, 0x42, 0x60, 0x82
        ];
        response.Content = new ByteArrayContent(pngBytes);

        return Task.FromResult(response);
    }
}
