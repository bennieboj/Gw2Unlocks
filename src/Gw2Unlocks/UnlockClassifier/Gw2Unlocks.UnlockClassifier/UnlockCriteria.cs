using System;

namespace Gw2Unlocks.UnlockClassifier;

public abstract class UnlockCriteria
{
    public abstract bool Matches(string unlock);
    public virtual bool AllowHistorical => false;

    public virtual int Priority => 0;
}