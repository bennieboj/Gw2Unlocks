using System.Text.Json;
using System.Text.Json.Nodes;

namespace Gw2Unlocks.Wiki.WikiApi.Testing;

public class FakeWikiApi : IWikiApi
{
    private readonly Dictionary<string, JsonNode> _templateResponses = [];
    private readonly Dictionary<string, JsonNode> _pageResponses = [];

    public void AddTemplate(string template, string wikitext)
    {
        _templateResponses[template] = TestHelper.Expand(wikitext);
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

        if (action == "query" && json?["prop"]?.ToString() == "revisions")
        {
            var title = json?["titles"]?.ToString();
            if(title != null && _pageResponses.TryGetValue(title, out var value))
                return Task.FromResult(value);
        }

        throw new InvalidOperationException("Unsupported query in FakeWikiApi");
    }

    public void AddPage(string title, string wikitext)
    {
        var x = JsonNode.Parse($$"""
    {
      "query": {
        "pages": {
          "123": {
            "pageid": 123,
            "title": "Test",
            "revisions": [
              {
                "slots": {
                  "main": {
                    "*": {{JsonSerializer.Serialize(wikitext)}}
                  }
                }
              }
            ]
          }
        }
      }
    }
    """)!;

        _pageResponses[title] = x;
    }
}