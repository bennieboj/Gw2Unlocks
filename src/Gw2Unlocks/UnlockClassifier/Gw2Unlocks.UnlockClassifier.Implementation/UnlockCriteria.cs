using System;

namespace Gw2Unlocks.UnlockClassifier.Implementation;

internal interface IItemOrCurrencyCriteria
{
    string GetIItemOrCurrency();
}

internal sealed class ZoneCriteria(string ZoneName) : UnlockCriteria
{
    public override bool Matches(string unlock)
    {
        var name = unlock.ToString();
        return string.Equals(
            name,
            ZoneName,
            StringComparison.OrdinalIgnoreCase);
    }
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