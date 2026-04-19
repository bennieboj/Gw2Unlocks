using GuildWars2.Collections;
using GuildWars2.Hero.Achievements.Categories;
using System;

namespace Gw2Unlocks.Api.Testing.Builders;

public class AchievementCategoryBuilder
{
    private int _id = 1;
    private string _name = "Test Category";
    private string _description = "Description";
    private int _order;
    private Uri _iconUrl = new("http://example.com");

    private IImmutableValueList<AchievementRef> _achievements
        = ImmutableValueList<AchievementRef>.Empty;

    private IImmutableValueList<AchievementRef>? _tomorrow
        = ImmutableValueList<AchievementRef>.Empty;

    public AchievementCategoryBuilder WithId(int id) { _id = id; return this; }

    public AchievementCategoryBuilder WithName(string name) { _name = name; return this; }

    public AchievementCategoryBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public AchievementCategoryBuilder WithOrder(int order)
    {
        _order = order;
        return this;
    }

    public AchievementCategoryBuilder WithIconUrl(Uri iconUrl)
    {
        _iconUrl = iconUrl;
        return this;
    }

    public AchievementCategoryBuilder WithAchievements(IImmutableValueList<AchievementRef> achievements)
    {
        _achievements = achievements;
        return this;
    }

    public AchievementCategoryBuilder WithTomorrow(IImmutableValueList<AchievementRef>? tomorrow)
    {
        _tomorrow = tomorrow;
        return this;
    }

    public AchievementCategory Build() => new()
    {
        Id = _id,
        Name = _name,
        Description = _description,
        Order = _order,
        IconUrl = _iconUrl,
        Achievements = _achievements,
        Tomorrow = _tomorrow
    };
}