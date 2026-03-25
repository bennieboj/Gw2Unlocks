using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Items;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
namespace Gw2Unlocks.Api;

public interface IGw2ApiCache : IGw2ApiSource
{
    Task SaveItemsToCacheAsync(ReadOnlyCollection<Item> data, CancellationToken cancellationToken);
    Task SaveAchievementsToCacheAsync(ReadOnlyCollection<Achievement> data, CancellationToken cancellationToken);
    Task SaveMiniaturesToCacheAsync(ReadOnlyCollection<Miniature> data, CancellationToken cancellationToken);
    Task SaveNoveltiesToCacheAsync(ReadOnlyCollection<Novelty> data, CancellationToken cancellationToken);
    Task SaveTitlesToCacheAsync(ReadOnlyCollection<Title> data, CancellationToken cancellationToken);
}
