using Gw2Unlocks.Cache.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.IconSpriteSheet.Cache;

public class IconSpriteSheetCache(CachePaths cachePaths) : GenericCache(cachePaths.CacheDir, "icon-sprite-sheet"), IIconSpriteSheetCache
{
    private const string inventoryName = "inventory.json";
    public Task<IconSpriteSheetInventoryData> GetIconSpriteSheetInventory(CancellationToken cancellationToken) => 
        LoadFromFileAsync<IconSpriteSheetInventoryData>(inventoryName, cancellationToken);

    public string GetIconSpreadSheetsPath()
    {
        return Path.Combine(CacheFolder);
    }

    public async Task SaveIconSpreadSheets(Dictionary<string, byte[]> files, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(files);

        string[] filePaths = Directory.GetFiles(CacheFolder);
        foreach (string filePath in filePaths)
            File.Delete(filePath);
        
        foreach (var file in files)
        {
            await SaveToCacheBytesAsync(file.Key, file.Value, cancellationToken);
        }
    } 

    public Task SaveIconSpriteSheetInventory(IconSpriteSheetInventoryData inventory, CancellationToken cancellationToken) =>
        SaveToCacheJsonAsync(inventoryName, inventory, cancellationToken);
}
