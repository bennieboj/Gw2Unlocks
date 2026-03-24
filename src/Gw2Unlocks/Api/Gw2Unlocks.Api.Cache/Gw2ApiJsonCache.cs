using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Items;
using Gw2Unlocks.Cache.Common;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Api.Cache;
#pragma warning disable CA1812 // This class is instantiated via DI and not directly, so it may appear unused
internal sealed class Gw2ApiJsonCache : GenericCache, IGw2ApiCache
#pragma warning restore CA1812 // This class is instantiated via DI and not directly, so it may appear unused
{

    public Gw2ApiJsonCache() : base("api-cache")
    {
    }

    public Task<ReadOnlyCollection<Item>> GetItemsAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Item>("items.json", cancellationToken);
    public Task<ReadOnlyCollection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Achievement>("achievements.json", cancellationToken);
    public Task<ReadOnlyCollection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Miniature>("miniatures.json", cancellationToken);
    public Task<ReadOnlyCollection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Novelty>("novelties.json", cancellationToken);
    public Task<ReadOnlyCollection<Title>> GetTitlesAsync(CancellationToken cancellationToken) => LoadFromFileAsync<Title>("titles.json", cancellationToken);
}