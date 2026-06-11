using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.IconSpriteSheet;

public record IconSpriteSheetInput(string Type, int Id, Uri IconUrl);
public record IconSpriteSheetResult(Dictionary<string, Byte[]> Files, IconSpriteSheetInventoryData InventoryData);


public interface IIconSpriteSheetGenerator
{
    public Task<IconSpriteSheetResult> Generate(IEnumerable<IconSpriteSheetInput> inputs, CancellationToken cancellation);
}
