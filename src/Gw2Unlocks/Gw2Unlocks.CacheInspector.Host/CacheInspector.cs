using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Gw2Unlocks.Api;
using Gw2Unlocks.Wiki;
using Gw2Unlocks.WikiProcessing;

namespace Gw2Unlocks.CacheInspector.Host;

internal sealed record Match(string Key, string Name, object Data);

internal sealed class CacheInspector(IGw2ApiSource api, IGw2WikiSource wiki, IGw2WikiProcessingSource processing)
{
    public async Task<List<Match>> LookupAsync(LookupSettings settings, CancellationToken token)
    {
        var query = settings.Query;
        bool Matches(string value) => settings.Search
            ? value.Contains(query, StringComparison.OrdinalIgnoreCase)
            : value.Equals(query, StringComparison.OrdinalIgnoreCase);

        List<Match> Filter<T>(IEnumerable<T> records, Func<T, int> id, Func<T, string> name) => records
            .Where(x => Matches(name(x)) || (!settings.Search && id(x).ToString(CultureInfo.InvariantCulture) == query))
            .Select(x => new Match(id(x).ToString(CultureInfo.InvariantCulture), name(x), x!)).ToList();

        switch (settings.DataSet)
        {
            case DataSet.Achievements:
                return Filter(await api.GetAchievementsAsync(token), x => x.Id, x => x.Name);
            case DataSet.AchievementCategories:
                return Filter(await api.GetAchievementCategoriesAsync(token), x => x.Id, x => x.Name);
            case DataSet.Items:
                return Filter(await api.GetItemsAsync(token), x => x.Id, x => x.Name);
            case DataSet.Skins:
                return Filter(await api.GetSkinsAsync(token), x => x.Id, x => x.Name);
            case DataSet.Miniatures:
                return Filter(await api.GetMiniaturesAsync(token), x => x.Id, x => x.Name);
            case DataSet.Novelties:
                return Filter(await api.GetNoveltiesAsync(token), x => x.Id, x => x.Name);
            case DataSet.Titles:
                return Filter(await api.GetTitlesAsync(token), x => x.Id, x => x.Name);
            case DataSet.Graph:
                return (await processing.GetAcquisitionGraph(token)).Nodes
                    .Where(x => Matches(x.Key)).Select(x => new Match(x.Key, x.Key, x.Value)).ToList();
            case DataSet.Zones:
                return (await processing.GetZoneData(token)).Zones
                    .Where(x => Matches(x.Name)).Select(x => new Match(x.Name, x.Name, x)).ToList();
            case DataSet.Wiki:
                var results = new List<Match>();
                // Parse titles so XML-escaped characters such as & work correctly.
                await foreach (var xml in wiki.StreamAllPages(token))
                {
                    var page = XElement.Parse(xml);
                    var title = page.Element(page.Name.Namespace + "title")?.Value;
                    if (title is not null && Matches(title))
                        results.Add(new Match(title, title, xml));
                }
                return results;
            default:
                throw new ArgumentOutOfRangeException(nameof(settings));
        }
    }

    public Task<AcquisitionGraph> GetGraphAsync(CancellationToken token) => processing.GetAcquisitionGraph(token);
}
