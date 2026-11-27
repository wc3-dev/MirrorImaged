using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// What a unit shows and is flagged with: pause, hide, invulnerable, its own data, colour, scale and tint.
public sealed partial class GameNatives
{
    /// <summary>Whether PauseUnit paused the unit; BlzPauseUnitEx's pauses don't count here, and a death unpauses it (probes units, units-more-2).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08); facts, Units: probes units-more and units-more-2 (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe + "; " + MoreProbe)]
    public bool IsUnitPaused(unit? whichHero) => whichHero?.Paused ?? false;

    /// <summary>
    /// A count of its own, up for true and down for false, which IsUnitPaused doesn't read (probe units). While it is
    /// above 0 the unit is held as a stun holds it: it doesn't move, fight or cast, it reads stunned to IsUnitType and its
    /// current order, and its buffs' time runs on; let go, the order it had comes back as after PauseUnit.
    /// </summary>
    /// <remarks>Close: the count is measured (probe units); that it holds the unit as a stun does, with no order event when it begins and the pause's order back when it ends, is the model's reading of the native's purpose, not measured.</remarks>
    [Native(Level.Close, Reason = "the count is measured (probe units); that it holds the unit as a stun does, with no order event when it begins and the pause's order back when it ends, is the model's reading of the native's purpose, not measured")]
    public void BlzPauseUnitEx(unit? whichUnit, bool flag)
    {
        if (whichUnit is not { } u) return;
        var held = Held(u);
        u.Pauses += flag ? 1 : -1;
        if (!held && Held(u)) Holding(u);
        else if (held && !Held(u)) LetGo(u);
    }

    /// <summary>Hides the unit (false) or shows it again (true); nothing for nil.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public void ShowUnit(unit? whichUnit, bool show)
    {
        if (whichUnit is not null) whichUnit.Hidden = !show;
    }

    /// <summary>Whether <c>ShowUnit</c> hid the unit; false for nil.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public bool IsUnitHidden(unit? whichUnit) => whichUnit?.Hidden ?? false;

    /// <summary>
    /// Invulnerable is having Avul: true adds it first in the unit's list, false takes it off, also one UnitAddAbility
    /// added (probe abilities).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08); facts, Abilities: probe abilities (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe + "; " + AbilitiesProbe)]
    public void SetUnitInvulnerable(unit? whichUnit, bool flag)
    {
        if (flag && whichUnit is not null && Find(whichUnit, Invulnerable) is null && Data.Object(ObjectKind.Ability, Invulnerable) is { } avul) Give(whichUnit, avul, 0);
        else if (!flag && Find(whichUnit, Invulnerable) is { } a) Take(a);
    }

    /// <summary>Whether the unit has Avul, from SetUnitInvulnerable or UnitAddAbility (probe abilities).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08); facts, Abilities: probe abilities (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe + "; " + AbilitiesProbe)]
    public bool BlzIsUnitInvulnerable(unit? whichUnit) => Find(whichUnit, Invulnerable) is not null;

    /// <summary>Keeps the integer on the unit, for <c>GetUnitUserData</c>.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public void SetUnitUserData(unit? whichUnit, int data)
    {
        if (whichUnit is not null) whichUnit.UserData = data;
    }

    /// <summary>The integer <c>SetUnitUserData</c> kept; 0 until then and for nil.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public int GetUnitUserData(unit? whichUnit) => whichUnit?.UserData ?? 0;
}
