using GuildWars2.Hero.Achievements;
using GuildWars2.Hero.Achievements.Titles;
using GuildWars2.Hero.Equipment.Miniatures;
using GuildWars2.Hero.Equipment.Novelties;
using GuildWars2.Items;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Api;
public interface IGw2ApiSource
{
    Task<ReadOnlyCollection<Item>> GetItemsAsync(CancellationToken cancellationToken);
    Task<ReadOnlyCollection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken);
    Task<ReadOnlyCollection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken);
    Task<ReadOnlyCollection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken);
    Task<ReadOnlyCollection<Title>> GetTitlesAsync(CancellationToken cancellationToken);
}
