using System.Text.RegularExpressions;

namespace Gw2Unlocks.Website;

internal static partial class SlugHelper
{
    public static string Slugify(string value)
    {
#pragma warning disable CA1308
        value = value.ToLowerInvariant();
#pragma warning restore CA1308

        value = InvalidCharsRegex().Replace(value, "");
        value = WhitespaceRegex().Replace(value, "-");

        return value.Trim('-');
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}