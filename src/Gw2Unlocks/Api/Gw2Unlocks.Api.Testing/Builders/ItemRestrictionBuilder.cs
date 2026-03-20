using GuildWars2;
using GuildWars2.Collections;
using GuildWars2.Hero;
using GuildWars2.Items;

namespace Gw2Unlocks.Api.Testing.Builders;


public class ItemRestrictionBuilder
{
    private IImmutableValueList<Extensible<RaceName>> _races = ImmutableValueList<Extensible<RaceName>>.Empty;
    private IImmutableValueList<Extensible<ProfessionName>> _professions = ImmutableValueList<Extensible<ProfessionName>>.Empty;
    private IImmutableValueList<Extensible<BodyType>> _bodyTypes = ImmutableValueList<Extensible<BodyType>>.Empty;
    private IImmutableValueList<string> _other = ImmutableValueList<string>.Empty;

    public ItemRestrictionBuilder WithRaces(params Extensible<RaceName>[] races)
    {
        _races = ImmutableValueList.Create(races);
        return this;
    }

    public ItemRestrictionBuilder AddRace(Extensible<RaceName> race)
    {
        _races = _races.Add(race);
        return this;
    }

    public ItemRestrictionBuilder WithProfessions(params Extensible<ProfessionName>[] professions)
    {
        _professions = ImmutableValueList.Create(professions);
        return this;
    }

    public ItemRestrictionBuilder AddProfession(Extensible<ProfessionName> profession)
    {
        _professions = _professions.Add(profession);
        return this;
    }

    public ItemRestrictionBuilder WithBodyTypes(params Extensible<BodyType>[] bodyTypes)
    {
        _bodyTypes = ImmutableValueList.Create(bodyTypes);
        return this;
    }

    public ItemRestrictionBuilder AddBodyType(Extensible<BodyType> bodyType)
    {
        _bodyTypes = _bodyTypes.Add(bodyType);
        return this;
    }

    public ItemRestrictionBuilder WithOther(params string[] others)
    {
        _other = ImmutableValueList.Create(others);
        return this;
    }

    public ItemRestrictionBuilder AddOther(string other)
    {
        _other = _other.Add(other);
        return this;
    }

    public ItemRestriction Build()
    {
        return new ItemRestriction
        {
            Races = _races,
            Professions = _professions,
            BodyTypes = _bodyTypes,
            Other = _other
        };
    }
}
