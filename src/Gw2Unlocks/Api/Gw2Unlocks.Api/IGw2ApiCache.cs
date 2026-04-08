using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Hero.Equipment.Wardrobe;
using GuildWars2.Items;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
namespace Gw2Unlocks.Api;

public interface IGw2ApiCache : IGw2ApiSource
{
    Task SaveItemsToCacheAsync(Collection<Item> data, CancellationToken cancellationToken);
    Task SaveSkinsToCacheAsync(Collection<EquipmentSkin> data, CancellationToken cancellationToken);
    Task SaveAchievementsToCacheAsync(Collection<Achievement> data, CancellationToken cancellationToken);
    Task SaveMiniaturesToCacheAsync(Collection<Miniature> data, CancellationToken cancellationToken);
    Task SaveNoveltiesToCacheAsync(Collection<Novelty> data, CancellationToken cancellationToken);
    Task SaveTitlesToCacheAsync(Collection<Title> data, CancellationToken cancellationToken);
}
