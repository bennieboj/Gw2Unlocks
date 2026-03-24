using System.Text.Json.Nodes;

namespace Gw2Unlocks.Wiki.WikiApi.Testing;

public static class TestHelper
{
    public static JsonNode Expand(string text)
    {
        return new JsonObject
        {
            ["expandtemplates"] = new JsonObject
            {
                ["wikitext"] = text
            }
        };
    }
}
