using System;

namespace Gw2Unlocks.UnlockClassifier;

public abstract class UnlockCriteria
{
    public abstract bool Matches(string unlock);
}

class ZoneCriteria(string ZoneName) : UnlockCriteria
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


class TokenCriteria(string TokenName, bool UsedInZoneSpecification = true) : UnlockCriteria
{
    public bool UsedInZoneSpecification { get; } = UsedInZoneSpecification;

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


class AchievementCategoryCriteria(string AchievementCategoryName) : UnlockCriteria
{
    public override bool Matches(string unlock)
    {
        var name = unlock.ToString();
        return string.Equals(
            name,
            AchievementCategoryName,
            StringComparison.OrdinalIgnoreCase);
    }
}

class CraftingMaterialCriteria(string craftingMaterialName) : UnlockCriteria
{
    public override bool Matches(string unlock)
    {
        var name = unlock.ToString();
        return string.Equals(
            name,
            craftingMaterialName,
            StringComparison.OrdinalIgnoreCase);
    }
}

class CurrencyCriteria(string CurrencyName, bool UsedInZoneSpecification = true) : UnlockCriteria
{
    public bool UsedInZoneSpecification { get; } = UsedInZoneSpecification;

    public override bool Matches(string cost)
    {
        var costString = cost.ToString() ?? throw new ArgumentException("Cost must be convertible to string", nameof(cost));
        return costString.Contains(
            CurrencyName,
            StringComparison.OrdinalIgnoreCase);
    }
}