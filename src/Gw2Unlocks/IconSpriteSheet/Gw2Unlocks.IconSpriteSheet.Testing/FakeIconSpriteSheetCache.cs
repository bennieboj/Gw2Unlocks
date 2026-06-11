using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.IconSpriteSheet.Testing;

public class FakeIconSpriteSheetCache : IIconSpriteSheetCache
{
    public IconSpriteSheetInventoryData SavedInventory { get; private set; } = new();
    public Dictionary<string, byte[]> SavedIconSpreadSheets { get; private set; } = [];

    public bool HasPublishedSpreadSheets { get; private set; }

    public Task<IconSpriteSheetInventoryData> GetIconSpriteSheetInventory(CancellationToken cancellationToken)
    {
        return Task.FromResult(SavedInventory);
    }

    public string GetIconSpreadSheetsPath()
    {        
        return "fakeLocation";
    }

    public Task SaveIconSpreadSheets(Dictionary<string, byte[]> files, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(files);
        foreach (var file in files)
        {
            SavedIconSpreadSheets.Add(file.Key, file.Value);
        }
        return Task.CompletedTask;
    }

    public Task SaveIconSpriteSheetInventory(IconSpriteSheetInventoryData inventory, CancellationToken cancellationToken)
    {
        SavedInventory = inventory;
        return Task.CompletedTask;

    }
}
