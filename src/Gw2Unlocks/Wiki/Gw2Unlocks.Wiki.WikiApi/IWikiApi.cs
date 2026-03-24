using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki.WikiApi;

public interface IWikiApi
{
    Task<JsonNode> QueryAsync(object parameters, CancellationToken cancellationToken);
}
