using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;

namespace Gw2Unlocks.Wiki.Implementation;

public static class ServiceCollectionExtensions
{
    internal const string WikiApiEndpoint = "https://wiki.guildwars2.com/api.php";

    public static IServiceCollection AddWikiSource(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiSource, Gw2WikiSource>()
                .AddSingleton(sp =>
                    new Lazy<Task<WikiSite>>(async () =>
                    {
                        var client = new WikiClient { ClientUserAgent = "MyGW2WikiCrawler/1.0 (your@email.com)" };
                        try
                        {
                            var site = new WikiSite(client, WikiApiEndpoint);
                            await site.Initialization; // fully async
                            return site;
                        }
                        catch (Exception ex)
                        {
                            // WikiClientLibrary exceptions don't carry the request URL,
                            // so surface the endpoint here to make failures reproducible locally.
                            throw new InvalidOperationException(
                                $"Wiki site initialization (siteinfo) failed for '{WikiApiEndpoint}'. " +
                                "Reproduce by requesting that endpoint locally. See inner exception for details.",
                                ex);
                        }
                    })
                );
        return services;
    }

}