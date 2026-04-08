using GuildWars2;
using GuildWars2.Collections;
using GuildWars2.Hero;
using GuildWars2.Hero.Equipment.Wardrobe;
using GuildWars2.Items;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gw2Unlocks.Api.Testing.Builders;

public class SkinBuilder
{
    private int _id = 1;
    private string _name = "Test Skin";
    private string _description = "Description";
    private SkinFlags _flags = new SkinFlagsBuilder().Build();
    private IImmutableValueList<Extensible<RaceName>> _races =
        ImmutableValueList<Extensible<RaceName>>.Empty;
    private Extensible<Rarity> _rarity = Rarity.Legendary;
    private Uri? _iconUrl = new("http://www.example.com");

    public SkinBuilder WithId(int id) { _id = id; return this; }
    public SkinBuilder WithName(string name) { _name = name; return this; }
    public SkinBuilder WithDescription(string description) { _description = description; return this; }
    public SkinBuilder WithFlags(SkinFlags flags) { _flags = flags; return this; }
    public SkinBuilder WithRaces(IImmutableValueList<Extensible<RaceName>> races) { _races = races; return this; }
    public SkinBuilder WithRarity(Rarity rarity) { _rarity = rarity; return this; }
    public SkinBuilder WithIconUrl(Uri? url) { _iconUrl = url; return this; }

    public EquipmentSkin Build()
    {
        return new EquipmentSkin
        {
            Id = _id,
            Name = _name,
            Description = _description,
            Flags = _flags,
            Races = _races,
            Rarity = _rarity,
            IconUrl = _iconUrl
        };
    }
}
