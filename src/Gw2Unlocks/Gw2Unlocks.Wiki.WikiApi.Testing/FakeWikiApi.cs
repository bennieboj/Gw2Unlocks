using System.Text.Json;
using System.Text.Json.Nodes;

namespace Gw2Unlocks.Wiki.WikiApi.Testing;

public class FakeWikiApi : IWikiApi
{
    private readonly Dictionary<string, JsonNode> _templateResponses = [];
    private readonly Dictionary<string, JsonNode> _categoryResponses = [];

    public void AddTemplate(string template, string wikitext)
    {
        _templateResponses[template] = TestHelper.Expand(wikitext);
    }

    public void AddCategory(string category, params string[] titles)
    {
        _categoryResponses[category] = JsonNode.Parse($$"""
        {
          "query": {
            "categorymembers": [
              {{string.Join(",", titles.Select(t => $$"""{"title":"{{t}}"}"""))}}
            ]
          }
        }
        """)!;
    }

    public Task<JsonNode> QueryAsync(object parameters, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToNode(parameters);

        var action = json?["action"]?.GetValue<string>();

        // ------------------------
        // expandtemplates
        // ------------------------
        if (action == "expandtemplates")
        {
            var text = json?["text"]?.GetValue<string>();

            if (text != null && _templateResponses.TryGetValue(text, out var value))
                return Task.FromResult(value);

            return Task.FromResult(JsonNode.Parse("{}")!);
        }

        // ------------------------
        // categorymembers
        // ------------------------
        if (action == "query")
        {
            var category = json?["cmtitle"]?.GetValue<string>();

            if (category != null &&
                category.StartsWith("Category:", StringComparison.InvariantCulture) &&
                _categoryResponses.TryGetValue(category["Category:".Length..], out var value))
            {
                return Task.FromResult(value);
            }

            return Task.FromResult(JsonNode.Parse("{}")!);
        }

        throw new InvalidOperationException("Unsupported query in FakeWikiApi");
    }
}