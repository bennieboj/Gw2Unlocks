using GuildWars2.Collections;
using GuildWars2.Hero.Equipment.Wardrobe;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gw2Unlocks.Api.Testing.Builders;

public class SkinFlagsBuilder
{
    private bool _hideIfLocked;
    private bool _noCost;
    private bool _overrideRarity;
    private bool _showInWardrobe;
    private readonly IImmutableValueList<string> _other = ImmutableValueList<string>.Empty;

    public SkinFlagsBuilder WithHideIfLocked(bool value = true) { _hideIfLocked = value; return this; }
    public SkinFlagsBuilder WithNoCost(bool value = true) { _noCost = value; return this; }
    public SkinFlagsBuilder WithOverrideRarity(bool value = true) { _overrideRarity = value; return this; }
    public SkinFlagsBuilder WithShowInWardrobe(bool value = true) { _showInWardrobe = value; return this; }

    public SkinFlags Build()
    {
        return new SkinFlags
        {
            HideIfLocked = _hideIfLocked,
            NoCost = _noCost,
            OverrideRarity = _overrideRarity,
            ShowInWardrobe = _showInWardrobe,
            Other = _other
        };
    }
}