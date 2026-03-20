using System;
using GuildWars2;
using GuildWars2.Collections;
using GuildWars2.Hero.Equipment.Novelties;

namespace Gw2Unlocks.Api.Testing.Builders;

public class NoveltyBuilder
{
    private int _id = 1;
    private string _name = "Novelty";
    private readonly string _description = "Desc";
    private readonly Uri _iconUrl = new("http://example.com");
    private readonly Extensible<NoveltyKind> _slot = NoveltyKind.Tonic;
    private readonly IImmutableValueList<int> _unlockItems = ImmutableValueList.Create([1, 2, 3]);


    public NoveltyBuilder WithId(int id) { _id = id; return this; }
    public NoveltyBuilder WithName(string name) { _name = name; return this; }

    public Novelty Build() => new()
    {
        Id = _id,
        Name = _name,
        Description = _description,
        IconUrl = _iconUrl,
        Slot = _slot,
        UnlockItemIds = _unlockItems
    };
}