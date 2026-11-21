namespace MirrorImaged.Natives;

// A unit's life, mana, place, facing, height, owner and flags, as the units probe and the facts' "Units" measure them.
public sealed partial class GameNatives
{
    const int LifeState = 0, MaxLifeState = 1, ManaState = 2, MaxManaState = 3; // UNIT_STATE_*

    // A unit's life is kept up to the most life, as the most life less the damage that leaves, in floats (0.41 reads
    // 0.4100037 on a footman of 420, 0.4099731 on a Paladin of 650); below 0.405 the unit dies, the death event inside
    // the call reading life 0 (facts, Units; probe units). A dead unit stays dead, its life kept from 0 to its most.
    void SetLife(unit u, float life, unit? killer = null)
    {
        var (max, old) = (u.MaxLife, u.Life);
        var now = max - (max - Math.Min(life, max));
        if (!u.Dead && now < Dying)
        {
            Kill(u, killer);
            return;
        }
        u.Life = u.Dead ? Math.Max(now, 0) : now;
        StateChanged(u, LifeState, old, u.Life);
    }

    /// <summary>Life, most life, mana and most mana; 0 for any other state.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitState(unit? whichUnit, unitstate? whichUnitState) => whichUnit is null ? 0 : whichUnitState?.Id switch
    {
        LifeState => whichUnit.Life,
        MaxLifeState => whichUnit.MaxLife,
        ManaState => whichUnit.Mana,
        MaxManaState => whichUnit.MaxMana,
        _ => 0,
    };

    /// <summary>
    /// Life as SetWidgetLife, mana kept from 0 up to the most mana; the most life and mana don't change this way
    /// (facts, Units: BlzSetUnitMaxHP and BlzSetUnitMaxMana do).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Units: SetUnitState of the maxima changes nothing; mana clamps (10-21); facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Units: SetUnitState of the maxima changes nothing; mana clamps (10-21); " + UnitsProbe)]
    public void SetUnitState(unit? whichUnit, unitstate? whichUnitState, float newVal)
    {
        if (whichUnit is null) return;
        if (whichUnitState?.Id == LifeState) SetLife(whichUnit, newVal);
        else if (whichUnitState?.Id == ManaState) SetMana(whichUnit, newVal);
    }

    void SetMana(unit u, float mana)
    {
        var old = u.Mana;
        u.Mana = Math.Max(Math.Min(mana, u.MaxMana), 0);
        StateChanged(u, ManaState, old, u.Mana);
    }

    /// <summary>The most life as a whole number; 0 for a null unit.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public int BlzGetUnitMaxHP(unit? whichUnit) => (int)(whichUnit?.MaxLife ?? 0);

    /// <summary>Keeps the life, cut to the new most life when it is lower (facts, Units).</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Units: BlzSetUnitMaxHP keeps life and clamps it (10-21); facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Units: BlzSetUnitMaxHP keeps life and clamps it (10-21); " + UnitsProbe)]
    public void BlzSetUnitMaxHP(unit? whichUnit, int hp)
    {
        if (whichUnit is null) return;
        whichUnit.MaxLife = hp;
        if (whichUnit.Life > hp) SetLife(whichUnit, hp);
    }

    /// <summary>The most mana as a whole number; 0 for a null unit.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public int BlzGetUnitMaxMana(unit? whichUnit) => (int)(whichUnit?.MaxMana ?? 0);

    /// <summary>Sets the most mana, then keeps the mana within it; nothing for a null unit.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public void BlzSetUnitMaxMana(unit? whichUnit, int mana)
    {
        if (whichUnit is null) return;
        whichUnit.MaxMana = mana;
        SetMana(whichUnit, whichUnit.Mana);
    }

    /// <summary>The unit's x; 0 for a null unit.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitX(unit? whichUnit) => whichUnit?.X ?? 0;

    /// <summary>The unit's y; 0 for a null unit.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe)]
    public float GetUnitY(unit? whichUnit) => whichUnit?.Y ?? 0;

    /// <summary>
    /// Moves the unit, ignoring pathing and the map's bounds (facts, Units: x 5096 past an edge of 4096 stays), firing the
    /// regions' events (probe units-regions).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Units: SetUnitX/Y ignore pathing and bounds (10-21); facts, Units: probe units (11-08); facts, Regions: probe units-regions (11-08)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Units: SetUnitX/Y ignore pathing and bounds (10-21); " + UnitsProbe + "; " + RegionsProbe)]
    public void SetUnitX(unit? whichUnit, float newX)
    {
        if (whichUnit is not null) Move(whichUnit, newX, whichUnit.Y);
    }

    /// <summary>Moves the unit to the new y, as SetUnitX does for x.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Units: SetUnitX/Y ignore pathing and bounds (10-21); facts, Units: probe units (11-08); facts, Regions: probe units-regions (11-08)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Units: SetUnitX/Y ignore pathing and bounds (10-21); " + UnitsProbe + "; " + RegionsProbe)]
    public void SetUnitY(unit? whichUnit, float newY)
    {
        if (whichUnit is not null) Move(whichUnit, whichUnit.X, newY);
    }

    /// <summary>The ground under the unit, not its height above it (facts, Units: 0 for a lifted footman on flat ground).</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Units: BlzGetUnitZ reads the ground under the unit (10-27); facts, Units: probe units (11-08)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Units: BlzGetUnitZ reads the ground under the unit (10-27); " + UnitsProbe)]
    public float BlzGetUnitZ(unit? whichUnit) => whichUnit is null ? 0 : Ground(whichUnit.X, whichUnit.Y);

    /// <summary>As BlzGetUnitZ; Close: not measured, and the game may draw the unit elsewhere.</summary>
    /// <remarks>Close: it gives BlzGetUnitZ's answer; the height the local player sees is not measured.</remarks>
    [Native(Level.Close)]
    public float BlzGetLocalUnitZ(unit? whichUnit) => BlzGetUnitZ(whichUnit);
}
