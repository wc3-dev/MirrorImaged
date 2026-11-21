using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// Units that spells call: the ability's unit type (its UnitID field, one of the levels'), how many (DataA or DataB by the ability), for how long (its
// duration, a hero's own for a hero) and the summon event (decisions D381). Close: they stand near the caster or the target place, and what the
// game gives them besides their type (a water elemental's wave attack, a wolf's invisibility) is the type's own abilities.
public sealed partial class GameNatives
{
    const int UnitSummon = 84, PlayerUnitSummon = 47;

    /// <summary>The unit the summon event is about: the one that was called.</summary>
    /// <remarks>Close: the event fires as each unit is made, with the caster as the summoner; the game's timing of it is not measured (D381).</remarks>
    [Native(Level.Close)] public unit? GetSummonedUnit() => Response(UnitSummon, PlayerUnitSummon)?.Other;

    /// <summary>The unit that called it: the caster.</summary>
    /// <remarks>Close: the event fires as each unit is made, with the caster as the summoner; the game's timing of it is not measured (D381).</remarks>
    [Native(Level.Close)] public unit? GetSummoningUnit() => Response(UnitSummon, PlayerUnitSummon)?.Unit;

    // The unit types an ability summons, as its data lists them by level
    IEnumerable<int> SummonTypes(ability a) =>
        (Data.FieldAt(ObjectKind.Ability, "UnitID") is { } field ? a.Type.Text(Data.Own(a.Type, field), a.Level) : null) is { Length: > 0 } list
            ? list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(Code) : [];

    // The units made round the place, the way a ring is spaced, for the caster's owner, each a summoned unit that ends with its time
    void Summon(unit caster, ability a, float x, float y, int count)
    {
        var types = SummonTypes(a).ToList();
        if (types.Count == 0 || count <= 0) return;
        for (var n = 0; n < count; n++)
        {
            var angle = n * 2 * MathF.PI / count;
            if (Create(caster.Owner, types[Math.Min(n, types.Count - 1)], x + 80 * MathF.Cos(angle), y + 80 * MathF.Sin(angle), 270, types[Math.Min(n, types.Count - 1)]) is not { } made) continue;
            var seconds = Field(a, Code("adur"));
            UnitApplyTimedLife(made, Code("BTLF"), seconds);
            FireUnit(new(caster, Other: made, Skill: a.Type.Id), UnitSummon, PlayerUnitSummon);
        }
    }
}
