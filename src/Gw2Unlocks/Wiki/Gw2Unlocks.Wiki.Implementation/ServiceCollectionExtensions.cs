using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;

namespace Gw2Unlocks.Wiki.Implementation;

public static class ServiceCollectionExtensions
{
    internal const string WikiApiEndpoint = "https://wiki.guildwars2.com/api.php";

    // Honest operator identity for every request the wiki fetcher makes;
    // wiki.guildwars2.com blocks placeholder or browser-impersonating agents.
    internal const string UserAgent = "Gw2Unlocks/1.0 (https://github.com/bennieboj/Gw2Unlocks)";

    public static IServiceCollection AddWikiSource(this IServiceCollection services)
    {
        services.AddSingleton<IGw2WikiSource, Gw2WikiSource>()
                .AddSingleton(sp =>
                    new Lazy<Task<WikiSite>>(async () =>
                    {
                        var logger = sp.GetRequiredService<ILoggerFactory>()
                                       .CreateLogger("Gw2Unlocks.Wiki.Implementation.WikiSiteFactory");

                        // wiki.guildwars2.com runs 403 "block windows" against GitHub runner IPs.
                        // Each attempt must create a fresh WikiSite: a faulted Initialization
                        // task never re-executes, so re-awaiting the old one would not retry.
                        var retryPolicy = Policy
                            .Handle<HttpRequestException>()
                            .WaitAndRetryAsync(
                                retryCount: 3,
                                // 15s, 30s, 60s
                                sleepDurationProvider: attempt => TimeSpan.FromSeconds(15 * Math.Pow(2, attempt - 1)),
                                onRetry: (ex, delay, attempt, _) => logger.LogWarning(
                                    ex,
                                    "Wiki siteinfo request to {WikiEndpoint} failed ({ErrorMessage}); retry {RetryAttempt}/3 in {DelaySeconds}s",
                                    WikiApiEndpoint, ex.Message, attempt, delay.TotalSeconds));

                        try
                        {
                            return await retryPolicy.ExecuteAsync(async () =>
                            {
                                var client = new WikiClient { ClientUserAgent = UserAgent };
                                var site = new WikiSite(client, WikiApiEndpoint);
                                await site.Initialization; // fully async
                                return site;
                            });
                        }
                        catch (Exception ex)
                        {
                            // WikiClientLibrary exceptions don't carry the request URL,
                            // so surface the endpoint here to make failures reproducible locally.
                            throw new InvalidOperationException(
                                $"Wiki site initialization (siteinfo) failed for '{WikiApiEndpoint}' after retries. " +
                                "Reproduce by requesting that endpoint locally. See inner exception for details.",
                                ex);
                        }
                    })
                );
        return services;
    }

}