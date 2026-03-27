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
    Task<Collection<Item>> GetItemsAsync(CancellationToken cancellationToken);
    Task<Collection<Achievement>> GetAchievementsAsync(CancellationToken cancellationToken);
    Task<Collection<Miniature>> GetMiniaturesAsync(CancellationToken cancellationToken);
    Task<Collection<Novelty>> GetNoveltiesAsync(CancellationToken cancellationToken);
    Task<Collection<Title>> GetTitlesAsync(CancellationToken cancellationToken);
}
