# Cache Inspector

Uses the existing cache-as-source registrations for `IGw2ApiSource`, `IGw2WikiSource`, and `IGw2WikiProcessingSource`. No live API/wiki sources, updater services, or write operations are registered by the host. Existing cache implementation behavior is unchanged.

## Run (PowerShell)

```powershell
$project = 'C:\Users\Ben\OneDrive\docs\Projects\Gw2Unlocks\src\Gw2Unlocks\Gw2Unlocks.CacheInspector.Host\Gw2Unlocks.CacheInspector.Host.csproj'
dotnet run --project $project -- --help
dotnet run --project $project
dotnet run --project $project -- lookup Achievements 1 --details
dotnet run --project $project -- lookup Achievements Slayer --search
dotnet run --project $project -- lookup Wiki 'Mini Dolyak' --details
dotnet run --project $project -- lookup Graph 'Mini Dolyak' --details
dotnet run --project $project -- links 'Mini Dolyak'
dotnet run --project $project -- links 'Mini Dolyak' --depth 3
dotnet run --project $project -- links 'Mini Dolyak' --incoming --depth 1
```

No arguments opens the Spectre terminal menu. It exposes the same lookup, search, details, direction, and depth choices as the CLI. Redirected input/output requires using CLI commands instead.

Datasets: Achievements, AchievementCategories, Items, Skins, Miniatures, Novelties, Titles, Wiki, Graph, Zones.

API lookups accept an exact ID or case-insensitive name. Other lookups accept case-insensitive titles/names. `--search` performs substring name matching. `--details` prints JSON records or raw wiki page XML. Wiki lookups scan the cached XML through the wiki source interface; this may take several seconds.

Graph traversal follows outgoing edges by default. Each edge is indented two spaces per level and includes edge type and metadata. Cycles and previously expanded nodes are labeled instead of expanded endlessly. Omit `--depth` to traverse all reachable edges; a finite depth labels cut-off branches. This displays cached graph relationships, not every hyperlink in raw wiki text. Redirect information not persisted in the graph cache cannot be resolved by this tool.

Exit codes: 0 = matches/success, 1 = not found, 2 = error (CLI validation failures may use Spectre's own nonzero code), 130 = cancellation handled by the host.

Cache location uses the existing `AddCacheDir()` convention. Missing JSON files inherit the existing cache source behavior (empty data). Classifier and sprite-sheet inspection are not included in this first version.

The existing solution was deliberately left unchanged in accordance with the instruction not to edit existing files. Build/run this project directly.
