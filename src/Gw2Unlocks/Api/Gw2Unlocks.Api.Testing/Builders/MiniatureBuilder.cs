using System;
using GuildWars2.Hero.Equipment.Miniatures;

namespace Gw2Unlocks.Api.Testing.Builders;

public class MiniatureBuilder
{
    private int _id = 1;
    private string _name = "Mini";
    private readonly string _lockedText = "Locked";
    private readonly Uri _iconUrl = new("http://example.com");
    private readonly int _order = 1;
    private readonly int _itemId = 1;

    public MiniatureBuilder WithId(int id) { _id = id; return this; }
    public MiniatureBuilder WithName(string name) { _name = name; return this; }

    public Miniature Build() => new()
    {
        Id = _id,
        Name = _name,
        LockedText = _lockedText,
        IconUrl = _iconUrl,
        Order = _order,
        ItemId = _itemId
    };
}