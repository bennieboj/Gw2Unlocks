using GuildWars2.Collections;
using GuildWars2.Hero.Achievements;

namespace Gw2Unlocks.Api.Testing.Builders;

public class AchievementFlagsBuilder
{
    private bool _categoryDisplay;
    private bool _daily;
    private bool _hidden;
    private bool _ignoreNearlyComplete;
    private bool _moveToTop;
    private bool _pvp;
    private bool _repairOnLogin;
    private bool _repeatable;
    private bool _requiresUnlock;
    private bool _permanent;
    private bool _weekly;
    private bool _monthly;

    public AchievementFlagsBuilder CategoryDisplay(bool value = true) { _categoryDisplay = value; return this; }
    public AchievementFlagsBuilder Daily(bool value = true) { _daily = value; return this; }
    public AchievementFlagsBuilder Hidden(bool value = true) { _hidden = value; return this; }
    public AchievementFlagsBuilder IgnoreNearlyComplete(bool value = true) { _ignoreNearlyComplete = value; return this; }
    public AchievementFlagsBuilder MoveToTop(bool value = true) { _moveToTop = value; return this; }
    public AchievementFlagsBuilder Pvp(bool value = true) { _pvp = value; return this; }
    public AchievementFlagsBuilder RepairOnLogin(bool value = true) { _repairOnLogin = value; return this; }
    public AchievementFlagsBuilder Repeatable(bool value = true) { _repeatable = value; return this; }
    public AchievementFlagsBuilder RequiresUnlock(bool value = true) { _requiresUnlock = value; return this; }
    public AchievementFlagsBuilder Permanent(bool value = true) { _permanent = value; return this; }
    public AchievementFlagsBuilder Weekly(bool value = true) { _weekly = value; return this; }
    public AchievementFlagsBuilder Monthly(bool value = true) { _monthly = value; return this; }

    private readonly IImmutableValueList<string> _other = ImmutableValueList<string>.Empty;

    public AchievementFlags Build()
    {
        return new AchievementFlags
        {
            CategoryDisplay = _categoryDisplay,
            Daily = _daily,
            Hidden = _hidden,
            IgnoreNearlyComplete = _ignoreNearlyComplete,
            MoveToTop = _moveToTop,
            Pvp = _pvp,
            RepairOnLogin = _repairOnLogin,
            Repeatable = _repeatable,
            RequiresUnlock = _requiresUnlock,
            Permanent = _permanent,
            Weekly = _weekly,
            Monthly = _monthly,
            Other = _other,
        };
    }
}