using System.Text.RegularExpressions;

namespace Gw2Unlocks.SiteGenerator;

internal static class SlugHelper
{
    public static string Slugify(string value)
    {
#pragma warning disable CA1308
        value = value.ToLowerInvariant();
#pragma warning restore CA1308
        value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
        value = Regex.Replace(value, @"\s+", "-");
        return value.Trim('-');
    }
}