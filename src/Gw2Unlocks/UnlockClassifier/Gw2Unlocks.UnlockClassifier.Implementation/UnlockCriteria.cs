using System;
using System.Linq;

namespace Gw2Unlocks.UnlockClassifier.Implementation;

internal interface IItemOrCurrencyCriteria
{
    string GetIItemOrCurrency();
}

internal sealed class ZoneCriteria(string ZoneName, int priority = 80) : UnlockCriteria
{
    public override bool Matches(string unlock)
    {
        var name = unlock.ToString();
        return string.Equals(
            name,
            ZoneName,
            StringComparison.OrdinalIgnoreCase);
    }
    public override int Priority { get; } = priority;
}


internal sealed class TokenCriteria(string TokenName, int priority = 80, bool UsedInZoneSpecification = true) : UnlockCriteria, IItemOrCurrencyCriteria
{
    public bool UsedInZoneSpecification { get; } = UsedInZoneSpecification;

    public string GetIItemOrCurrency()
    {
        return TokenName;
    }
    public override int Priority { get; } = priority;

    public override bool Matches(string unlock)
    {
        var name = unlock.ToString();
        return string.Equals(
            name,
            TokenName,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool MatchesCost(string cost)
    {
        var costString = cost.ToString() ?? throw new ArgumentException("Token must be convertible to string for cost matching", nameof(cost));
        return costString.Contains(
            TokenName,
            StringComparison.OrdinalIgnoreCase);
    }
}


internal sealed class AchievementCategoryCriteria(string AchievementCategoryName, int priority = 80) : UnlockCriteria
{
    public override int Priority { get; } = priority;

    public override bool Matches(string unlock)
    {
        var name = unlock.ToString();
        return string.Equals(
            name,
            AchievementCategoryName,
            StringComparison.OrdinalIgnoreCase);
    }
}


/// <summary>
/// The expansion or release a wiki infobox declares via its <c>requires</c> parameter, which
/// Template:Infobox requires turns into an expansion notice and a category. The wiki accepts both
/// a short code and a spelled-out name (<c>eod</c> or <c>end of dragons</c>), so a criterion matches
/// any of the aliases it is given.
/// </summary>
/// <remarks>
/// This is not a scored criterion. It never contributes priority, it is never matched against a
/// node: it constrains <em>which</em> categories a classification may land in, so that a release
/// stated once on the item page prunes candidates from every other release.
/// </remarks>
internal sealed class RequiresExpansionOrReleaseCriteria(params string[] aliases) : UnlockCriteria
{
    public override int Priority => 0;

    public override bool Matches(string requires)
    {
        if (string.IsNullOrWhiteSpace(requires))
        {
            return false;
        }

        // A page may require more than one release, e.g. "requires = lws5, eod".
        return requires
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(required => aliases.Any(
                alias => string.Equals(alias, required, StringComparison.OrdinalIgnoreCase)));
    }
}


internal sealed class SetCriteria(string SetName, int priority = 100) : UnlockCriteria
{
    public override int Priority { get; } = priority;

    public override bool Matches(string unlock)
    {
        var name = unlock.ToString();
        return string.Equals(
            name,
            SetName,
            StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class CraftingMaterialCriteria(string CraftingMaterialName, int priority = 80) : UnlockCriteria, IItemOrCurrencyCriteria
{
    public string GetIItemOrCurrency()
    {
        return CraftingMaterialName;
    }
    public override int Priority { get; } = priority;

    public override bool Matches(string unlock)
    {
        var name = unlock.ToString();
        return string.Equals(
            name,
            CraftingMaterialName,
            StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class CurrencyCriteria(string CurrencyName, bool UsedInZoneSpecification = true, int priority = 80, bool allowHistorical = false) : UnlockCriteria, IItemOrCurrencyCriteria
{
    public bool UsedInZoneSpecification { get; } = UsedInZoneSpecification;
    public override bool AllowHistorical { get; } = allowHistorical;
    public override int Priority { get; } = priority;

    public string GetIItemOrCurrency()
    {
        return CurrencyName;
    }
    public override bool Matches(string cost)
    {
        var costString = cost.ToString() ?? throw new ArgumentException("Cost must be convertible to string", nameof(cost));
        return costString.Contains(
            CurrencyName,
            StringComparison.OrdinalIgnoreCase);
    }
}