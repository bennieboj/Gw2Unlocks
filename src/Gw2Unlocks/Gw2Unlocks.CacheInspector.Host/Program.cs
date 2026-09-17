using System;
using Gw2Unlocks.Api.Cache;
using Gw2Unlocks.Cache.Common;
using Gw2Unlocks.CacheInspector.Host;
using Gw2Unlocks.Wiki.Cache;
using Gw2Unlocks.WikiProcessing.Cache;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddCacheDir()
    .AddJsonCacheApiSource()
    .AddCacheWikiAsSource()
    .AddJsonCacheWikiProcessingSource();
services.AddSingleton<CacheInspector>();
services.AddSingleton<InspectorConsole>();
using var provider = services.BuildServiceProvider();
var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("cache-inspector");
    config.AddAsyncDelegate<LookupSettings>("lookup", (_, settings, token) =>
        provider.GetRequiredService<InspectorConsole>().LookupAsync(settings, token))
        .WithDescription("Look up or search API records, wiki pages, graph nodes and zones.");
    config.AddAsyncDelegate<GraphSettings>("links", (_, settings, token) =>
        provider.GetRequiredService<InspectorConsole>().GraphAsync(settings, token))
        .WithDescription("Print all reachable graph links, indented two spaces per level.");
    config.AddAsyncDelegate("interactive", (_, token) =>
        provider.GetRequiredService<InspectorConsole>().InteractiveAsync(token))
        .WithDescription("Open the interactive terminal menu.");
    config.SetExceptionHandler((ex, _) =>
    {
        AnsiConsole.WriteLine($"Error: {ex.Message}");
        return ex is OperationCanceledException ? 130 : 2;
    });
});
return await app.RunAsync(args.Length == 0 ? ["interactive"] : args);
