using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.IconSpriteSheet;

public readonly record struct InventoryKey(string Type, int Id);
public readonly record struct IconSpriteSheetInventoryItem(int Sheet, int X, int Y);
public class IconSpriteSheetInventoryData
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
    public Dictionary<string, IconSpriteSheetInventoryItem> Inventory { get; set; } = [];
}

public interface IIconSpriteSheetCache
{
    Task SaveIconSpreadSheets(Dictionary<string, byte[]> files, CancellationToken cancellationToken);
    Task SaveIconSpriteSheetInventory(IconSpriteSheetInventoryData inventory, CancellationToken cancellationToken);
    string GetIconSpreadSheetsPath();
    Task<IconSpriteSheetInventoryData> GetIconSpriteSheetInventory(CancellationToken cancellationToken);
}
