using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;


namespace Gw2Unlocks.Wiki.WikiApi.Implementation;

public class RealWikiApi(Lazy<Task<WikiSite>> lazySite) : IWikiApi
{
    public async Task<JsonNode> QueryAsync(object parameters, CancellationToken cancellationToken)
    {
        var site = await lazySite.Value;
        return await site.InvokeMediaWikiApiAsync(new MediaWikiFormRequestMessage(parameters), cancellationToken);
    }
}