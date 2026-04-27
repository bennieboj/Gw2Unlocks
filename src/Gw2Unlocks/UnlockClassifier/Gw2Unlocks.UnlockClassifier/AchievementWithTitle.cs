using GuildWars2.Hero.Achievements;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Gw2Unlocks.UnlockClassifier
{
    public record AchievementWithTitle : Achievement
    {
        public string? TitleName { get; init; }

        [SetsRequiredMembers]
        public AchievementWithTitle(Achievement source, string? titleName)
        {
            ArgumentNullException.ThrowIfNull(source);
            TitleName = titleName;

            Id = source.Id;
            Name = source.Name;
            IconUrl = source.IconUrl;
            Description = source.Description;
            Requirement = source.Requirement;
            LockedText = source.LockedText;
            Flags = source.Flags;
            Tiers = source.Tiers;
            Rewards = source.Rewards;
            Bits = source.Bits;
            Prerequisites = source.Prerequisites;
            PointCap = source.PointCap;
        }

    }
}