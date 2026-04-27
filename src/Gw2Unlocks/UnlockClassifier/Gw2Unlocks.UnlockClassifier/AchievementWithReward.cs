using GuildWars2.Hero.Achievements;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Gw2Unlocks.UnlockClassifier
{
    public record AchievementWithReward : Achievement
    {
        public string? RewardName { get; init; }
        public string? RewardIcon { get; init; }

        [SetsRequiredMembers]
        public AchievementWithReward(Achievement source, string? rewardIcon, string? titleName)
        {
            ArgumentNullException.ThrowIfNull(source);
            RewardName = titleName;
            RewardIcon = rewardIcon;

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