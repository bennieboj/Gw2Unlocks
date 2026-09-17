using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace Gw2Unlocks.CacheInspector.Host;

internal sealed class InspectorConsole(CacheInspector inspector)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<int> LookupAsync(LookupSettings settings, CancellationToken token)
    {
        var matches = await inspector.LookupAsync(settings, token);
        foreach (var match in matches.OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Key, StringComparer.Ordinal))
        {
            // WriteLine treats cached text literally, not as Spectre markup.
            AnsiConsole.WriteLine(match.Key == match.Name ? match.Name : $"{match.Key}: {match.Name}");
            if (settings.Details)
                AnsiConsole.WriteLine(match.Data is string xml ? xml : JsonSerializer.Serialize(match.Data, JsonOptions));
        }
        AnsiConsole.WriteLine(matches.Count == 0 ? "Not found in cached data." : $"Found {matches.Count} match(es).");
        return matches.Count == 0 ? 1 : 0;
    }

    public async Task<int> GraphAsync(GraphSettings settings, CancellationToken token)
    {
        var graph = await inspector.GetGraphAsync(token);
        var key = graph.Nodes.ContainsKey(settings.Page) ? settings.Page
            : graph.Nodes.Keys.FirstOrDefault(x => x.Equals(settings.Page, StringComparison.OrdinalIgnoreCase));
        if (key is null)
        {
            AnsiConsole.WriteLine("Node not found in cached graph.");
            return 1;
        }
        foreach (var line in GraphTraversal.Walk(graph, key, settings.Incoming, settings.Depth, token))
            AnsiConsole.WriteLine(line);
        return 0;
    }

    public async Task<int> InteractiveAsync(CancellationToken token)
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            AnsiConsole.WriteLine("Interactive mode requires a terminal. Use --help for CLI commands.");
            return 2;
        }
        while (!token.IsCancellationRequested)
        {
            var action = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Inspect cached data")
                .AddChoices("Lookup / search", "Graph links", "Exit"));
            if (action == "Exit") return 0;
            try
            {
                if (action == "Lookup / search")
                {
                    var settings = new LookupSettings
                    {
                        DataSet = AnsiConsole.Prompt(new SelectionPrompt<DataSet>().Title("Dataset").AddChoices(Enum.GetValues<DataSet>())),
                        Query = await AnsiConsole.AskAsync<string>("ID / name / title:", token),
                        Search = await AnsiConsole.ConfirmAsync("Substring search?", false, token),
                        Details = await AnsiConsole.ConfirmAsync("Show full records?", false, token)
                    };
                    await LookupAsync(settings, token);
                }
                else
                {
                    var settings = new GraphSettings
                    {
                        Page = await AnsiConsole.AskAsync<string>("Starting graph page:", token),
                        Incoming = await AnsiConsole.ConfirmAsync("Follow incoming links?", false, token)
                    };
                    if (await AnsiConsole.ConfirmAsync("Limit depth?", false, token))
                        settings.Depth = AnsiConsole.Prompt(new TextPrompt<int>("Maximum depth:").Validate(x => x >= 0));
                    await GraphAsync(settings, token);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                AnsiConsole.WriteLine($"Error: {ex.Message}");
            }
        }
        return 130;
    }
}
