using GuildWars2.Hero.Achievements;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gw2Unlocks.UnlockClassifier
{
    internal sealed class AchievementWithRewardJsonConverter : JsonConverter<AchievementWithReward>
    {
        public override AchievementWithReward Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, AchievementWithReward value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (Achievement)value, options);

            // now add your extra properties manually if needed
            writer.WriteString("rewardName", value.RewardName);
            writer.WriteString("rewardIcon", value.RewardIcon);
        }
    }

    [JsonConverter(typeof(AchievementWithRewardJsonConverter))]
    public record AchievementWithReward : Achievement
    {
        public string? RewardName { get; init; }
        public string? RewardIcon { get; init; }

        [SetsRequiredMembers]
        public AchievementWithReward(Achievement source, string? rewardIcon, string? titleName, Uri? iconUrl)
        {
            ArgumentNullException.ThrowIfNull(source);
            RewardName = titleName;
            RewardIcon = rewardIcon;

            Id = source.Id;
            Name = source.Name;
            IconUrl = iconUrl;
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