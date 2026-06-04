using Gw2Unlocks.UnlockClassifier;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Website;

internal sealed class SiteGeneratorService(
    ILogger<SiteGeneratorService> logger,
    IClassifierCache classifierCache,
    IHostEnvironment env,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    private FileSystemWatcher? _watcherTemplates;
    private FileSystemWatcher? _watcherStaticFiles;
    private CancellationTokenSource? _debounceCts;
    private readonly SemaphoreSlim _buildLock = new(1, 1);

    private readonly JsonSerializerOptions serOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await DoWork(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SiteGeneratorService");
        }
        finally
        {
            hostApplicationLifetime.StopApplication();
        }
    }

    private async Task DoWork(CancellationToken stoppingToken)
    {
        if (!env.IsDevelopment())
        {
            await GenerateSite(stoppingToken);
            return;
        }
        
        //generate once
        await GenerateSite(stoppingToken);

        _watcherTemplates = new FileSystemWatcher("WebsiteTemplates")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        _watcherTemplates.Changed += OnChanged;
        _watcherTemplates.Created += OnChanged;
        _watcherTemplates.Deleted += OnChanged;
        _watcherTemplates.Renamed += OnChanged;
        _watcherTemplates.EnableRaisingEvents = true;

        _watcherStaticFiles = new FileSystemWatcher("WebsiteTemplates")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        _watcherStaticFiles.Changed += OnChanged;
        _watcherStaticFiles.Created += OnChanged;
        _watcherStaticFiles.Deleted += OnChanged;
        _watcherStaticFiles.Renamed += OnChanged;
        _watcherStaticFiles.EnableRaisingEvents = true;

        logger.LogInformation("Template watcher started");

        // keep service alive
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                await _buildLock.WaitAsync(token);
                try
                {
                    await GenerateSite(token);
                }
                finally
                {
                    _buildLock.Release();
                }
                logger.LogInformation("Site regenerated due to file change");
            }
            catch (TaskCanceledException)
            {
                // expected during rapid edits
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during site regeneration");
            }
        });
    }

    public override void Dispose()
    {
        if (_watcherTemplates != null)
        {
            _watcherTemplates.EnableRaisingEvents = false;
            _watcherTemplates.Changed -= OnChanged;
            _watcherTemplates.Created -= OnChanged;
            _watcherTemplates.Deleted -= OnChanged;
            _watcherTemplates.Renamed -= OnChanged;

            _watcherTemplates.Dispose();
        }
        if (_watcherStaticFiles != null)
        {
            _watcherStaticFiles.EnableRaisingEvents = false;
            _watcherStaticFiles.Changed -= OnChanged;
            _watcherStaticFiles.Created -= OnChanged;
            _watcherStaticFiles.Deleted -= OnChanged;
            _watcherStaticFiles.Renamed -= OnChanged;

            _watcherStaticFiles.Dispose();
        }

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();

        _buildLock?.Dispose();

        base.Dispose();
    }

    private async Task GenerateSite(CancellationToken stoppingToken)
    {
        var config = await classifierCache.GetClassifierConfigFromCacheAsync(stoppingToken);
        var unlockMapJson = JsonSerializer.Serialize(BuildUnlockMap(config), serOptions);
        var urls = new List<string>();

        var sidebar = BuildSidebar(config);

        var allUnlocks = config.UnlockGroups
            .SelectMany(g =>
                g.Unlocks
                .Concat(g.UnlockCategories.SelectMany(c => c.Unlocks)))
            .ToList();

        var css = await File.ReadAllTextAsync("WebsiteTemplates/page.css", stoppingToken);
        var js = await File.ReadAllTextAsync("WebsiteTemplates/page.js", stoppingToken);

        var wwwRootTempPath = "wwwroot";
        if (!Directory.Exists(wwwRootTempPath))
        {
            Directory.CreateDirectory(wwwRootTempPath);
        }
        CopyDirectoryContents("WebsiteStaticFiles", wwwRootTempPath);

        GeneratePage(
        new PageModel
        {
            Css = css,
            Js = js,
            Title = "GW2 Unlocks",
            Description = "All Guild Wars 2 unlocks",
            Url = "/",
            Unlocks = allUnlocks,
            TypeGroups = BuildTypeGroups(allUnlocks),
            Sidebar = sidebar,
            UnlockMapJson = unlockMapJson,
        },
        Path.Combine(wwwRootTempPath, "index.html")
    );

        urls.Add("/");

        foreach (var group in config.UnlockGroups)
        {
            var groupSlug = SlugHelper.Slugify(group.Name);

            var groupUnlocks = group.Unlocks
                .Concat(group.UnlockCategories.SelectMany(c => c.Unlocks))
                .ToList();

            var groupUrl = $"/{groupSlug}/";

            GeneratePage(
                new PageModel
                {
                    Css = css,
                    Js = js,
                    Title = $"GW2 {group.Name} Unlocks",
                    Description = $"All unlocks for {group.Name} in Guild Wars 2",
                    Url = groupUrl,
                    Unlocks = groupUnlocks,
                    TypeGroups = BuildTypeGroups(groupUnlocks),
                    Sidebar = sidebar,
                    UnlockMapJson = unlockMapJson,
                    Group = group
                },
                Path.Combine(wwwRootTempPath, groupSlug, "index.html")
            );

            urls.Add(groupUrl);

            foreach (var category in group.UnlockCategories)
            {
                var categorySlug = SlugHelper.Slugify(category.Name);

                var categoryUrl = $"/{groupSlug}/{categorySlug}/";

                GeneratePage(
                        new PageModel
                        {
                            Css = css,
                            Js = js,
                            Title = $"GW2 {category.Name} Unlocks",
                            Description = $"All unlocks for {category.Name} in Guild Wars 2",
                            Url = categoryUrl,
                            Unlocks = [.. category.Unlocks],
                            TypeGroups = BuildTypeGroups([.. category.Unlocks]),
                            Sidebar = sidebar,
                            UnlockMapJson = unlockMapJson,
                            Group = group,
                            Category = category
                        },
                        Path.Combine(wwwRootTempPath, groupSlug, categorySlug, "index.html")
                    );

                urls.Add(categoryUrl);
            }
        }

        GenerateSiteMap(wwwRootTempPath, urls);

        logger.LogInformation("Static site generation complete.");
    }

    public static void GenerateSiteMap(string publicPath, List<string> urls)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var url in urls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <loc>https://gw2unlocks.com{url}</loc>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <lastmod>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:sszzz}</lastmod>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        File.WriteAllText(Path.Combine(publicPath, "sitemap.xml"), sb.ToString());
    }

    private static UnlockMapModel BuildUnlockMap(ClassifyConfig config)
    {
        var map = new UnlockMapModel();

        foreach (var group in config.UnlockGroups)
        {
            var groupKey = SlugHelper.Slugify(group.Name);

            var groupDict = new Dictionary<UnlockClassifier.Type, List<int>>();
            var allGroupUnlocks = group.Unlocks.Concat(group.UnlockCategories.SelectMany(c => c.Unlocks));
            foreach (var unlock in allGroupUnlocks)
            {
                if (unlock.ApiData == null) continue;

                var type = unlock.ApiData.Type;
                var id = unlock.ApiData.Id;

                if (!groupDict.TryGetValue(type, out var list))
                {
                    list = [];
                    groupDict[type] = list;
                }

                list.Add(id);
            }

            map.Groups[groupKey] = groupDict;

            foreach (var category in group.UnlockCategories)
            {
                var catKey = SlugHelper.Slugify(category.Name);

                var catDict = new Dictionary<UnlockClassifier.Type, List<int>>();

                foreach (var unlock in category.Unlocks)
                {
                    if (unlock.ApiData == null) continue;

                    var type = unlock.ApiData.Type;
                    var id = unlock.ApiData.Id;

                    if (!catDict.TryGetValue(type, out var list))
                    {
                        list = [];
                        catDict[type] = list;
                    }

                    list.Add(id);
                }

                map.Categories[catKey] = catDict;
            }
        }

        return map;
    }
    static void GeneratePage(PageModel model, string outputPath)
    {
        var templateText = File.ReadAllText("WebsiteTemplates/page.html");

        var template = Template.Parse(templateText);

        if (template.HasErrors)
        {
            throw new InvalidOperationException(string.Join("\n", template.Messages));
        }

        var scriptObject = new Scriban.Runtime.ScriptObject();

        scriptObject.Import(model, renamer: member => member.Name);

        var context = new TemplateContext
        {
            LoopLimit = 100_000,
            LimitToString = 0,
            MemberRenamer = member => member.Name
        };

        context.PushGlobal(scriptObject);

        var result = template.Render(context);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        File.WriteAllText(outputPath, result);
    }
    private static readonly Dictionary<UnlockClassifier.Type, string> TypeGroupLabels =
        new() {
            { UnlockClassifier.Type.Achievement, "Achievements" },
            { UnlockClassifier.Type.Miniature, "Miniatures" },
            { UnlockClassifier.Type.Skin, "Skins" },
            { UnlockClassifier.Type.Novelty, "Novelties" }
        };

    private static List<TypeGroupModel> BuildTypeGroups(List<Unlock> unlocks)
    {
        return [.. unlocks
            .Where(u => u.ApiData != null)
            .GroupBy(u => u.ApiData!.Type)
            .Select(g => new TypeGroupModel
            {
                Type = g.Key,
                Label = TypeGroupLabels.TryGetValue(g.Key, out string? label) == true ? label : throw new ArgumentException("TypeGroupLabels"),
                Total = g.Count(),
                Unlocked = null, // JS fills this
                Unlocks = [.. g.Select(u => new UnlockRenderModel
                {
                    Id = u.ApiData!.Id,
                    Name = u.ApiData.Name,
                    IconUrl = u.ApiData.IconUrl?.ToString() ?? "",
                    Requirement = u.ApiData.Requirement ?? "",
                    RewardIcon = u.ApiData.RewardIconUrl?.ToString(),
                    RewardName = u.ApiData.RewardName,
                    Type = g.Key,
                    WikiUrl = BuildWikiUrl(u, g.Key)
                })]
            })];
    }

    static List<SidebarGroupModel> BuildSidebar(ClassifyConfig config)
    {
        return [.. config.UnlockGroups
            .Select(group => new SidebarGroupModel
            {
                Name = group.Name,
                Url = $"/{SlugHelper.Slugify(group.Name)}/",
                Slug = SlugHelper.Slugify(group.Name),
                IsWip = group.Unlocks.Count == 0 && group.UnlockCategories.All(c => c.Unlocks.Count == 0),

                Categories = [.. group.UnlockCategories
                    .Select(category => new SidebarCategoryModel
                    {
                        Name = category.Name,
                        Url = $"/{SlugHelper.Slugify(group.Name)}/{SlugHelper.Slugify(category.Name)}/",
                        Slug = SlugHelper.Slugify(category.Name),
                        IsWip = category.Unlocks.Count == 0
                    })]
            })];
    }

    private static string BuildWikiUrl(Unlock unlock, UnlockClassifier.Type type)
    {
        var id = unlock.ApiData!.ChatCodeId;

        if (type == UnlockClassifier.Type.Achievement)
        {
            return $"https://wiki.guildwars2.com/index.php?search={Uri.EscapeDataString(unlock.Name)}";
        }

        string chatLink = type switch
        {
            UnlockClassifier.Type.Miniature => CreateMiniChatLink(id),
            UnlockClassifier.Type.Skin => CreateSkinChatLink(id),
            UnlockClassifier.Type.Novelty => CreateItemChatLink(id),
            _ => ""
        };

        return $"https://wiki.guildwars2.com/index.php?title=Special%3ASearch&search={Uri.EscapeDataString(chatLink)}";
    }

    private static string CreateMiniChatLink(int id)
    {
        byte[] bytes =
        [
            0x02,
        0x01,
        (byte)(id & 0xFF),
        (byte)((id >> 8) & 0xFF),
        (byte)((id >> 16) & 0xFF),
        0x00
        ];

        return $"[&{Convert.ToBase64String(bytes)}]";
    }

    private static string CreateItemChatLink(int id)
    {
        byte[] bytes =
        [
            0x02,
        0x01,
        (byte)(id & 0xFF),
        (byte)((id >> 8) & 0xFF),
        (byte)((id >> 16) & 0xFF),
        0x00
        ];

        return $"[&{Convert.ToBase64String(bytes)}]";
    }

    private static string CreateSkinChatLink(int id)
    {
        byte[] bytes =
        [
            0x0A,
        (byte)(id & 0xFF),
        (byte)((id >> 8) & 0xFF),
        (byte)((id >> 16) & 0xFF),
        (byte)((id >> 24) & 0xFF)
        ];

        return $"[&{Convert.ToBase64String(bytes)}]";
    }

    private static void CopyDirectoryContents(string source, string destination)
    {
        if (!Directory.Exists(source))
            return;

        Directory.CreateDirectory(destination);

        // Copy files
        foreach (var file in Directory.GetFiles(source))
        {
            var destFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        // Copy subdirectories
        foreach (var dir in Directory.GetDirectories(source))
        {
            var destDir = Path.Combine(destination, Path.GetFileName(dir));

            CopyDirectoryContents(dir, destDir);
        }
    }
}