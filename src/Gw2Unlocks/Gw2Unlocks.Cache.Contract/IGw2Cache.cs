using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gw2Unlocks.Cache.Contract;

/// <summary>
/// Abstraction for a Guild Wars 2 API cache.
/// Stores arbitrary endpoint/key -> JSON response mappings.
/// </summary>
public interface IGw2Cache
{
    /// <summary>
    /// Adds a new cached response or updates an existing one.
    /// </summary>
    /// <param name="endpoint">API endpoint, e.g. "/v2/items"</param>
    /// <param name="key">Unique key identifying the resource  (id: int)</param>
    /// <param name="jsonContent">JSON response content</param>>
    Task AddOrUpdateAsync(string endpoint, int id, string jsonContent);

    /// <summary>
    /// Retrieves a cached response. Returns null if not present.
    /// </summary>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="key">Unique key for the resource (id: int)</param>
    /// <returns>JSON string if cached, otherwise null</returns>
    Task<string?> GetCachedAsync(string endpoint, int id);

    /// <summary>
    /// Returns the subset of keys from <paramref name="keys"/> that are not yet cached.
    /// </summary>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="keys">All keys to check  (id: int)</param>
    /// <returns>Keys not present in the cache</returns>
        Task<IEnumerable<int>> GetNewKeysAsync(string endpoint, IEnumerable<int> ids);
}
