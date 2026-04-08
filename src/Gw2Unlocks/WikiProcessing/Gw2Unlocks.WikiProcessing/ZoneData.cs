using System.Collections.ObjectModel;

namespace Gw2Unlocks.WikiProcessing;

public record ZoneData
{
    public ZoneData()
    {        
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
    public Collection<Zone> Zones { get; set; } = [];
}

public record Zone (string Name, Collection<string> AchievementCategories)
{
}