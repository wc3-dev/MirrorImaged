using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal bool IgnoresAlarm;
    internal int? Gold; // what SetResourceAmount and AddResourceAmount left
    internal HashSet<player>? Rescuers;
    internal float? RescueRange;
    internal bool Rescuing; // its 0.5 s checks for a rescuer are running
}

// The rest of the unit natives, as the probes units-more, units-more-2, units-types and units-gold measure them
// (facts, "Units"): kinds of a type and added kinds, sameness, illusions, selection, the alarm, rescue, a gold
// mine's gold, and what only shows.
public sealed partial class GameNatives
{
    const string TypesProbe = "facts, Units: probe units-types (11-08)";
    const string GoldProbe = "facts, Units: probe units-gold (11-08)";
    static readonly int[] Mines = [Code("Agld"), Code("Abgm"), Code("Aegm")]; // a gold mine's, a haunted one's, an entangled one's
    static readonly int MaxGold = Code("Gld1");

    /// <summary>Whether the two are one unit; nil and nil are (probe units-more).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probes units-more and units-more-2 (11-08)).</remarks>
    [Native(Level.Exact, Fact = MoreProbe)]
    public bool IsUnit(unit? whichUnit, unit? whichSpecifiedUnit) => whichUnit == whichSpecifiedUnit;

    /// <summary>
    /// The kinds of a unit type, as IsUnitType reads a unit of it, but never dead or ground (a footman isn't, a gryphon
    /// rider flies); false for an id that is no unit's (probe units-types).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units-types (11-08)).</remarks>
    [Native(Level.Exact, Fact = TypesProbe)]
    public bool IsUnitIdType(int unitId, unittype? whichUnitType) =>
        whichUnitType is not null && Data.Object(ObjectKind.Unit, unitId) is { } type && Is(type, null, whichUnitType.Id);

    /// <summary>
    /// Adds a kind of the classification (giant, summoned, undead, mechanical, peon, sapper, townhall, ancient, tauren)
    /// to the one unit, and its classification field, true; any other kind changes nothing, false (probe units-types).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units-types (11-08)).</remarks>
    [Native(Level.Exact, Fact = TypesProbe)]
    public bool UnitAddType(unit? whichUnit, unittype? whichUnitType) => Classify(whichUnit, whichUnitType, true);

    /// <summary>Takes a kind of the classification off the unit, its type's too (a barracks' mechanical), as UnitAddType adds one (probe units-types).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units-types (11-08)).</remarks>
    [Native(Level.Exact, Fact = TypesProbe)]
    public bool UnitRemoveType(unit? whichUnit, unittype? whichUnitType) => Classify(whichUnit, whichUnitType, false);

    bool Classify(unit? u, unittype? kind, bool add)
    {
        if (u is null || kind is null || !Classes.TryGetValue(kind.Id, out var name)) return false;
        var classes = BlzGetUnitIntegerField(u, Convert<unitintegerfield>(Classification));
        u.Fields[Classification] = add ? classes | Categories[name] : classes & ~Categories[name];
        return true;
    }

    /// <summary>
    /// True for an image of Mirror Image (probe abilities-spells), false for every unit the probe made (probe units-more).
    /// Close: the other abilities and items that make illusions (game-facts, Units) are not here.
    /// </summary>
    /// <remarks>Close: Only an image of Mirror Image counts; illusions made by other abilities and items are not modelled.</remarks>
    [Native(Level.Close)]
    public bool IsUnitIllusion(unit? whichUnit) => whichUnit?.Illusion ?? false;

    /// <summary>
    /// Whether it can be selected: alive, shown and not removed, a paused unit, a hero and a building too (probe
    /// units-more); never one that had Locust, though the ability is taken off again (probe units-locust).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probes units-more and units-more-2 (11-08)).</remarks>
    [Native(Level.Exact, Fact = MoreProbe)]
    public bool BlzIsUnitSelectable(unit? whichUnit) => whichUnit is { Dead: false, Hidden: false, Locust: false, Removed: false };

    /// <summary>A structure keeps the flag and answers true; any other unit false, keeping nothing (probe units-more-2).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probes units-more and units-more-2 (11-08)).</remarks>
    [Native(Level.Exact, Fact = MoreProbe)]
    public bool UnitIgnoreAlarm(unit? whichUnit, bool flag)
    {
        if (whichUnit is null || !IsBuildingNow(whichUnit)) return false;
        whichUnit.IgnoresAlarm = flag;
        return true;
    }

    /// <summary>What UnitIgnoreAlarm kept; false at first (probes units-more and units-more-2).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probes units-more and units-more-2 (11-08)).</remarks>
    [Native(Level.Exact, Fact = MoreProbe)]
    public bool UnitIgnoreAlarmToggled(unit? whichUnit) => whichUnit?.IgnoresAlarm ?? false;

    /// <summary>Changes nothing; the icon isn't drawn.</summary>
    /// <remarks>Recorded: Shows the unit's other icon; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Shows the unit's other icon; no native reads it back")]
    public void UnitSetUsesAltIcon(unit? whichUnit, bool flag)
    {
    }

    /// <summary>Changes nothing; no glow is drawn.</summary>
    /// <remarks>Recorded: Shows or hides the unit's team glow; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Shows or hides the unit's team glow; no native reads it back")]
    public void BlzShowUnitTeamGlow(unit? whichUnit, bool show)
    {
    }

    /// <summary>Changes nothing; no unit changes owner.</summary>
    /// <remarks>Recorded: Gives no unit away, fires nothing and changes nothing a native reads (probe units-more); what the game keeps it for isn't known.</remarks>
    [Native(Level.Recorded, Reason = "Gives no unit away, fires nothing and changes nothing a native reads (probe units-more); what the game keeps it for isn't known")]
    public void SetPlayerUnitsOwner(player? whichPlayer, int newOwner)
    {
    }

    const int UnitRescued = 63, PlayerUnitRescued = 19; // EVENT_UNIT_RESCUED, EVENT_PLAYER_UNIT_RESCUED
    const string RescueProbe = "probes units-rescue and units-rescue-range (11-21, D794)";

    // From the moment a unit is made rescuable it is checked every 0.5 s; the first check that finds a living, shown unit of a player it is rescuable by
    // within its range (the nearest) fires the rescue events, the owner's registrations first, with that unit the rescuer, and ends the rescue: the
    // owner doesn't change and nothing fires again. A paused unit isn't rescued until it is let go (probe units-rescue).
    // The set range, or by default 400 and a half of the rescuer's collision size less a half: a footman (31) rescued a unit 414 away and not 415, a Paladin (32) 415
    // and not 416 (probe units-rescue-range)
    static float Reach(unit rescued, unit rescuer) => rescued.RescueRange ?? 399.5f + rescuer.Type.Real(Collision) / 2;

    void CheckRescue(unit u)
    {
        if (u.Removed || u.Dead || u.Rescuers is not { Count: > 0 } rescuers)
        {
            u.Rescuing = false;
            return;
        }
        var rescuer = u.Paused ? null : _units.Values.Where(r => r != u && !r.Dead && !r.Removed && !r.Hidden && rescuers.Contains(r.Owner) && Distance2(r.X - u.X, r.Y - u.Y) < Math.Pow(Reach(u, r), 2))
            .MinBy(r => Distance2(r.X - u.X, r.Y - u.Y));
        if (rescuer is null)
        {
            _game.At(_game.Step + Game.Steps(0.5), () => CheckRescue(u));
            return;
        }
        (u.Rescuing, u.Rescuers) = (false, null);
        FireUnit(new(u, Other: rescuer), UnitRescued, PlayerUnitRescued);
    }

    /// <summary>Lets the player rescue the unit (or stops it): from now the unit is checked every 0.5 s, and a rescuer in range fires the rescue events.</summary>
    /// <remarks>Close: Checked every 0.5 s from the call, as the game does; the default range is fitted to a footman's and a Paladin's collision (the one of a building, which stands on the pathing grid, wasn't read), and a rescuer's range isn't changed by its owner's alliances (not measured).</remarks>
    [Native(Level.Close, Fact = RescueProbe, Reason = "Checked every 0.5 s from the call, as the game does; the default range is fitted to a footman's and a Paladin's collision (the one of a building, which stands on the pathing grid, wasn't read), and a rescuer's range isn't changed by its owner's alliances (not measured)")]
    public void SetUnitRescuable(unit? whichUnit, player? byWhichPlayer, bool flag)
    {
        if (whichUnit is null || byWhichPlayer is null) return;
        whichUnit.Rescuers ??= [];
        if (flag) whichUnit.Rescuers.Add(byWhichPlayer);
        else whichUnit.Rescuers.Remove(byWhichPlayer);
        if (!flag || whichUnit.Rescuing) return;
        whichUnit.Rescuing = true;
        _game.At(_game.Step + Game.Steps(0.5), () => CheckRescue(whichUnit));
    }

    /// <summary>Sets how near a rescuer must come to rescue the unit.</summary>
    /// <remarks>Close: A rescuer closer than the range is rescued from, none at 0 or below; whether a set range adds the rescuer's collision size, as the default does, isn't measured (SetUnitRescuable).</remarks>
    [Native(Level.Close, Fact = RescueProbe, Reason = "A rescuer closer than the range is rescued from, none at 0 or below; whether a set range adds the rescuer's collision size, as the default does, isn't measured (SetUnitRescuable)")]
    public void SetUnitRescueRange(unit? whichUnit, float range)
    {
        if (whichUnit is not null) whichUnit.RescueRange = range;
    }

    /// <summary>The unit that rescued, in a rescue event; nil outside one.</summary>
    /// <remarks>Close: The unit that rescued, the nearest in range; nil outside a rescue event (SetUnitRescuable).</remarks>
    [Native(Level.Close, Fact = RescueProbe, Reason = "The unit that rescued, the nearest in range; nil outside a rescue event (SetUnitRescuable)")]
    public unit? GetRescuer() => Response(UnitRescued, PlayerUnitRescued)?.Other;

    /// <summary>
    /// A gold mine's gold: its Max Gold (its gold mine ability's Gld1, 12500) until set; 0 for any other unit, a
    /// haunted or entangled mine too (probe units-gold). A dead mine keeps it.
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units-gold (11-08)).</remarks>
    [Native(Level.Exact, Fact = GoldProbe)]
    public int GetResourceAmount(unit? whichUnit) =>
        whichUnit is null ? 0 : whichUnit.Gold ?? Abilities(whichUnit).FirstOrDefault(a => a?.BaseId == Mines[0])?.Integer(MaxGold, 1) ?? 0;

    /// <summary>
    /// Sets a mine's gold, a haunted or entangled one's too, as it is, 0 and below too, with no death; any other unit
    /// keeps none (probe units-gold).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units-gold (11-08)).</remarks>
    [Native(Level.Exact, Fact = GoldProbe)]
    public void SetResourceAmount(unit? whichUnit, int amount)
    {
        if (whichUnit is not null && Abilities(whichUnit).Any(a => a is not null && Mines.Contains(a.BaseId))) whichUnit.Gold = amount;
    }

    /// <summary>Adds to a mine's gold as SetResourceAmount sets it, in 32 bits that wrap (probe units-gold).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units-gold (11-08)).</remarks>
    [Native(Level.Exact, Fact = GoldProbe)]
    public void AddResourceAmount(unit? whichUnit, int amount) => SetResourceAmount(whichUnit, unchecked(GetResourceAmount(whichUnit) + amount));
}
