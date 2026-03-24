using System.Collections.ObjectModel;
using System.Threading;

using System.Threading.Tasks;

namespace Gw2Unlocks.Cache.Common;

public interface IGenericCache
{
    Task SaveToCacheAsync<T>(string fileName, ReadOnlyCollection<T> data, CancellationToken cancellationToken);
}