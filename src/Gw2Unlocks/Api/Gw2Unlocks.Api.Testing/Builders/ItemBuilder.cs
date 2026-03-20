
using GuildWars2;
using GuildWars2.Collections;
using GuildWars2.Items;
using System;

namespace Gw2Unlocks.Api.Testing.Builders;
public class ItemBuilder
{
    private int _id = 1;
    private string _name = "Test Item";
    private string _description = "Description";
    private int _level = 1;
    private Extensible<Rarity> _rarity = Rarity.Legendary;
    private Coin _vendorValue = Coin.Zero;
    private IImmutableValueList<Extensible<GameType>> _gameTypes = ImmutableValueList<Extensible<GameType>>.Empty;
    private ItemFlags _flags = new ItemFlagsBuilder().Build();
    private ItemRestriction _restrictions = new ItemRestrictionBuilder().Build();
    private string _chatLink = "dummy-chatlink";
    private Uri? _iconUrl = new("http://www.example.com");

    public ItemBuilder WithId(int id) { _id = id; return this; }
    public ItemBuilder WithName(string name) { _name = name; return this; }
    public ItemBuilder WithDescription(string description) { _description = description; return this; }
    public ItemBuilder WithLevel(int level) { _level = level; return this; }
    public ItemBuilder WithRarity(Rarity rarity) { _rarity = rarity; return this; }
    public ItemBuilder WithVendorValue(Coin coin) { _vendorValue = coin; return this; }
    public ItemBuilder WithGameTypes(IImmutableValueList<Extensible<GameType>> gameTypes) { _gameTypes = gameTypes; return this; }
    public ItemBuilder WithFlags(ItemFlags flags) { _flags = flags; return this; }
    public ItemBuilder WithRestrictions(ItemRestriction restrictions) { _restrictions = restrictions; return this; }
    public ItemBuilder WithChatLink(string chatLink) { _chatLink = chatLink; return this; }
    public ItemBuilder WithIconUrl(Uri? url) { _iconUrl = url; return this; }

    public Item Build()
    {
        return new Item
        {
            Id = _id,
            Name = _name,
            Description = _description,
            Level = _level,
            Rarity = _rarity,
            VendorValue = _vendorValue,
            GameTypes = _gameTypes,
            Flags = _flags,
            Restrictions = _restrictions,
            ChatLink = _chatLink,
            IconUrl = _iconUrl
        };
    }
}