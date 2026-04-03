using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;

namespace Gw2Unlocks.Wiki.Implementation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWikiSource(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiSource, Gw2WikiSource>()
                .AddSingleton(sp =>
                    new Lazy<Task<WikiSite>>(async () =>
                    {
                        var client = new WikiClient { ClientUserAgent = "MyGW2WikiCrawler/1.0 (your@email.com)" };
                        var site = new WikiSite(client, "https://wiki.guildwars2.com/api.php");
                        await site.Initialization; // fully async
                        return site;
                    })
                );
        return services;
    }

}