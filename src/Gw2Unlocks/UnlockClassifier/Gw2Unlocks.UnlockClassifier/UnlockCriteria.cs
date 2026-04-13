using System;

namespace Gw2Unlocks.UnlockClassifier;

public abstract class UnlockCriteria
{
    public abstract bool Matches(string unlock);
}

class NoOpCriteria : UnlockCriteria
{
    public override bool Matches(string unlock)
    {
        return false;
    }
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


class TokenCriteria(string TokenName) : UnlockCriteria
{
    public override bool Matches(string unlock)
    {
        var name = unlock.ToString();
        return string.Equals(
            name,
            TokenName,
            StringComparison.OrdinalIgnoreCase);
    }
}

class CurrencyCriteria(string CurrencyName) : UnlockCriteria
{
    public override bool Matches(string cost)
    {
        var costString = cost.ToString() ?? throw new ArgumentException("Cost must be convertible to string", nameof(cost));
        return costString.Contains(
            CurrencyName,
            StringComparison.OrdinalIgnoreCase);
    }
}