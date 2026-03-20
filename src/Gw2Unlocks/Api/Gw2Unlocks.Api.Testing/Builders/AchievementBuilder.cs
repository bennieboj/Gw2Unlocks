using System;
using GuildWars2.Collections;
using GuildWars2.Hero.Achievements;


namespace Gw2Unlocks.Api.Testing.Builders;

public class AchievementBuilder
{
    private int _id = 1;
    private string _name = "Test Achievement";
    private readonly Uri? _iconUrl = new("http://example.com");
    private readonly string _description = "Description";
    private readonly string _requirement = "Requirement";
    private readonly string _lockedText = "Locked";
    private readonly AchievementFlags _flags = new AchievementFlagsBuilder().Build();
    private readonly IImmutableValueList<AchievementTier> _tiers = ImmutableValueList<AchievementTier>.Empty;
    private readonly IImmutableValueList<AchievementReward>? _rewards = ImmutableValueList<AchievementReward>.Empty;
    private readonly IImmutableValueList<AchievementBit>? _bits = ImmutableValueList<AchievementBit>.Empty;
    private readonly IImmutableValueList<int> _prerequisites = ImmutableValueList<int>.Empty;
    private readonly int? _pointCap = 10;

    public AchievementBuilder WithId(int id) { _id = id; return this; }
    public AchievementBuilder WithName(string name) { _name = name; return this; }

    public Achievement Build() => new()
    {
        Id = _id,
        Name = _name,
        IconUrl = _iconUrl,
        Description = _description,
        Requirement = _requirement,
        LockedText = _lockedText,
        Flags = _flags,
        Tiers = _tiers,
        Rewards = _rewards,
        Bits = _bits,
        Prerequisites = _prerequisites,
        PointCap = _pointCap
    };
}