using GuildWars2.Collections;
using GuildWars2.Hero.Achievements.Titles;

namespace Gw2Unlocks.Api.Testing.Builders;

public class TitleBuilder
{
    private int _id = 1;
    private string _name = "Title";
    private readonly IImmutableValueList<int>? _achievements = ImmutableValueList.Create([1, 2, 3]);
    private readonly int? _points = 10;

    public TitleBuilder WithId(int id) { _id = id; return this; }
    public TitleBuilder WithName(string name) { _name = name; return this; }

    public Title Build() => new()
    {
        Id = _id,
        Name = _name,
        Achievements = _achievements,
        AchievementPointsRequired = _points
    };
}