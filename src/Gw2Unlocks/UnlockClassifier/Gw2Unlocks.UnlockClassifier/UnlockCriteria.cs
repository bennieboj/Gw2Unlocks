using System;

namespace Gw2Unlocks.UnlockClassifier;

public abstract class UnlockCriteria
{
    public abstract bool Matches(string unlock);
}