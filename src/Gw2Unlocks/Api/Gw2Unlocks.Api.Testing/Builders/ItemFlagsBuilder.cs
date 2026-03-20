using GuildWars2.Collections;
using GuildWars2.Items;

namespace Gw2Unlocks.Api.Testing.Builders;

public class ItemFlagsBuilder
{
    private bool _accountBindOnUse;
    private bool _accountBound;
    private bool _attuned;
    private bool _bulkConsume;
    private bool _deleteWarning;
    private bool _hideSuffix;
    private bool _infused;
    private bool _monsterOnly;
    private bool _noMysticForge;
    private bool _noSalvage;
    private bool _noSell;
    private bool _notUpgradeable;
    private bool _noUnderwater;
    private bool _soulbindOnUse;
    private bool _soulbound;
    private bool _tonic;
    private bool _unique;
    private readonly IImmutableValueList<string> _other = ImmutableValueList<string>.Empty;

    public ItemFlagsBuilder WithAccountBindOnUse(bool value = true) { _accountBindOnUse = value; return this; }
    public ItemFlagsBuilder WithAccountBound(bool value = true) { _accountBound = value; return this; }
    public ItemFlagsBuilder WithAttuned(bool value = true) { _attuned = value; return this; }
    public ItemFlagsBuilder WithBulkConsume(bool value = true) { _bulkConsume = value; return this; }
    public ItemFlagsBuilder WithDeleteWarning(bool value = true) { _deleteWarning = value; return this; }
    public ItemFlagsBuilder WithHideSuffix(bool value = true) { _hideSuffix = value; return this; }
    public ItemFlagsBuilder WithInfused(bool value = true) { _infused = value; return this; }
    public ItemFlagsBuilder WithMonsterOnly(bool value = true) { _monsterOnly = value; return this; }
    public ItemFlagsBuilder WithNoMysticForge(bool value = true) { _noMysticForge = value; return this; }
    public ItemFlagsBuilder WithNoSalvage(bool value = true) { _noSalvage = value; return this; }
    public ItemFlagsBuilder WithNoSell(bool value = true) { _noSell = value; return this; }
    public ItemFlagsBuilder WithNotUpgradeable(bool value = true) { _notUpgradeable = value; return this; }
    public ItemFlagsBuilder WithNoUnderwater(bool value = true) { _noUnderwater = value; return this; }
    public ItemFlagsBuilder WithSoulbindOnUse(bool value = true) { _soulbindOnUse = value; return this; }
    public ItemFlagsBuilder WithSoulbound(bool value = true) { _soulbound = value; return this; }
    public ItemFlagsBuilder WithTonic(bool value = true) { _tonic = value; return this; }
    public ItemFlagsBuilder WithUnique(bool value = true) { _unique = value; return this; }

    public ItemFlags Build()
    {
        return new ItemFlags
        {
            AccountBindOnUse = _accountBindOnUse,
            AccountBound = _accountBound,
            Attuned = _attuned,
            BulkConsume = _bulkConsume,
            DeleteWarning = _deleteWarning,
            HideSuffix = _hideSuffix,
            Infused = _infused,
            MonsterOnly = _monsterOnly,
            NoMysticForge = _noMysticForge,
            NoSalvage = _noSalvage,
            NoSell = _noSell,
            NotUpgradeable = _notUpgradeable,
            NoUnderwater = _noUnderwater,
            SoulbindOnUse = _soulbindOnUse,
            Soulbound = _soulbound,
            Tonic = _tonic,
            Unique = _unique,
            Other = _other
        };
    }
}
