using MirrorImaged.Data;
using MirrorImaged.Natives;

namespace MirrorImaged.Tests;

/// <summary>
/// Damage from code against the facts' "Damage" (game-facts.md, probes of 10-16 to 11-06): the damaging and damaged
/// events, the armour formula, the amount the actions change, the death inside the call and its killer. What
/// abilities change in a hit is wave 4's.
/// </summary>
public sealed class DamageTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard()
    {
        var data = new StandardData()
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;umvs=270;urac=human;umvt=foot;udty=large;uarm=Metal;utar=ground;uaen=1;ua1w=normal;ua1g=ground;udef=2;udtm=3.04;udea=3"))
            .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;umvs=190;udef=0;udty=medium;uaen=0;udtm=3;udea=3"))
            .Add(ObjectKind.Ability, "Avul", Values("alev=1"))
            .Add(ObjectKind.Ability, "ACmi", Values("alev=1"));
        foreach (var (key, value) in Values("DefenseArmor=0.06;DamageBonusNormal=1.00,1.50,1.00,0.70,1.00,1.00,0.05,1.00;DamageBonusPierce=2.00,0.75,1.00,0.35,1.00,0.50,0.05,1.50;DecayTime=2;BoneDecayTime=88;StructureDecayTime=30"))
            data.Constants[key] = value;
        return data;
    }

    Simulation Run(string main) => Simulation.Load(_maps.Build($$"""
        local log = {}
        local function say(...) local t = {} for i = 1, select("#", ...) do t[i] = tostring((select(i, ...))) end print(table.concat(t, " ")) end
        local function f2(x) return string.format("%.2f", x) end
        local function foot(x) return CreateUnit(Player(0), FourCC("hfoo"), x or 0, 0, 0) end
        local function watch(u)
            local g = CreateTrigger()
            TriggerRegisterUnitEvent(g, u, EVENT_UNIT_DAMAGING)
            TriggerAddAction(g, function() say("damaging", f2(GetEventDamage()), GetUnitName(GetEventDamageSource())) end)
            local d = CreateTrigger()
            TriggerRegisterUnitEvent(d, u, EVENT_UNIT_DAMAGED)
            TriggerAddAction(d, function() say("damaged", f2(GetEventDamage()), f2(GetWidgetLife(u))) end)
        end
        function config() end
        function main()
        {{main}}
        end
        """), "", data: Standard());

    static List<string> Printed(Simulation simulation) => [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];

    // facts: 100 melee/normal on a footman (armour 2) lost 89.3, its life dropping after the events, DAMAGING reading 100
    [Fact]
    public void A_hit_is_cut_by_armor_between_its_two_events()
    {
        using var simulation = Run("""
            local a, b = foot(), foot()
            watch(b)
            say(UnitDamageTarget(a, b, 100, false, false, ATTACK_TYPE_MELEE, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS), f2(GetWidgetLife(b)))
            """);

        Assert.Equal(["damaging 100.00 Footman", "damaged 89.29 420.00", "true 330.71"], Printed(simulation));
    }

    // facts, 11-06 and 10-31: armour 5 takes 76.92 of 100; -2.5 counts as -2 (111.64), stopping at -20 (170.99)
    [Fact]
    public void Armor_cuts_and_negative_armor_adds_up_to_twenty()
    {
        using var simulation = Run("""
            local a, t = foot(), foot()
            for _, armor in ipairs({5, 0, -2.5, -20, -50}) do
                BlzSetUnitArmor(t, armor)
                SetWidgetLife(t, 420)
                UnitDamageTarget(a, t, 100, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS)
                say(armor, f2(BlzGetUnitArmor(t)), f2(420 - GetWidgetLife(t)))
            end
            """);

        Assert.Equal(["5 5.00 76.92", "0 0.00 100.00", "-2.5 -2.50 111.64", "-20 -20.00 170.99", "-50 -50.00 170.99"], Printed(simulation));
    }

    // facts: armour cuts only damage type normal; the attack type's row for the defence type does too (divine 0.05: 4.46)
    [Fact]
    public void Only_normal_damage_meets_armor_and_the_defense_type()
    {
        using var simulation = Run("""
            local a, t = foot(), foot()
            local function hit(attack, damage)
                SetWidgetLife(t, 420)
                UnitDamageTarget(a, t, 100, false, false, attack, damage, WEAPON_TYPE_WHOKNOWS)
                return f2(420 - GetWidgetLife(t))
            end
            say(hit(ATTACK_TYPE_NORMAL, DAMAGE_TYPE_MAGIC), hit(ATTACK_TYPE_CHAOS, DAMAGE_TYPE_ENHANCED), hit(ATTACK_TYPE_MELEE, DAMAGE_TYPE_NORMAL), hit(ATTACK_TYPE_PIERCE, DAMAGE_TYPE_NORMAL))
            BlzSetUnitIntegerField(t, UNIT_IF_DEFENSE_TYPE, 6)
            say(hit(ATTACK_TYPE_MELEE, DAMAGE_TYPE_NORMAL))
            """);

        Assert.Equal(["100.00 100.00 89.29 89.29", "4.46"], Printed(simulation));
    }

    // facts: the damage type set before armour decides it, after armour it does nothing; the amount set in either event is what the life loses
    [Fact]
    public void What_the_actions_change_reaches_the_hit()
    {
        using var simulation = Run("""
            local a, t = foot(), foot()
            BlzSetUnitArmor(t, 5)
            local before = CreateTrigger()
            TriggerRegisterUnitEvent(before, t, EVENT_UNIT_DAMAGING)
            TriggerAddAction(before, function() say("set", BlzSetEventDamageType(DAMAGE_TYPE_UNIVERSAL), BlzGetEventDamageType() == DAMAGE_TYPE_UNIVERSAL) end)
            UnitDamageTarget(a, t, 100, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS)
            say(f2(GetWidgetLife(t)))
            DestroyTrigger(before)
            local after = CreateTrigger()
            TriggerRegisterUnitEvent(after, t, EVENT_UNIT_DAMAGED)
            TriggerAddAction(after, function() BlzSetEventDamageType(DAMAGE_TYPE_UNIVERSAL) BlzSetEventDamage(50) end)
            SetWidgetLife(t, 420)
            UnitDamageTarget(a, t, 100, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS)
            say(f2(GetWidgetLife(t)))
            say(BlzSetEventDamage(1), BlzSetEventAttackType(ATTACK_TYPE_MAGIC), GetEventDamage())
            """);

        Assert.Equal(["set true true", "320.00", "370.00", "nil false 0.0"], Printed(simulation));
    }

    // facts: a unit dies when a hit leaves it below 0.405; the death event runs inside the call after DAMAGED and names the killer, a dead one too
    [Fact]
    public void A_killing_hit_has_its_killer_and_a_dead_source_still_deals()
    {
        using var simulation = Run("""
            local a, t, u = foot(), foot(), foot()
            local d = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(d, Player(0), EVENT_PLAYER_UNIT_DEATH, nil)
            TriggerAddAction(d, function() say("died", GetUnitName(GetDyingUnit()), tostring(GetKillingUnit() == a), f2(GetWidgetLife(GetDyingUnit()))) end)
            watch(t)
            say(UnitDamageTarget(a, t, 1000, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS), UnitAlive(t))
            KillUnit(a)
            UnitDamageTarget(a, u, 100, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_UNIVERSAL, WEAPON_TYPE_WHOKNOWS)
            say(f2(GetWidgetLife(u)), GetKillingUnit())
            say(UnitDamageTarget(a, t, 5, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS), UnitDamageTarget(nil, u, 5, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS), f2(GetWidgetLife(u)))
            """);

        Assert.Equal(["damaging 1000.00 Footman", "damaged 892.86 420.00", "died Footman true 0.00", "true false", "died Footman false 0.00", "320.00 nil", "true false 320.00"], Printed(simulation));
    }

    // facts: a hit dealt inside DAMAGED runs fully inside the call and lowers the life at once
    [Fact]
    public void A_hit_inside_a_hit_runs_inside_the_call()
    {
        using var simulation = Run("""
            local a, t = foot(), foot()
            local d = CreateTrigger()
            TriggerRegisterUnitEvent(d, t, EVENT_UNIT_DAMAGED)
            local first = true
            TriggerAddAction(d, function()
                say("outer", f2(GetEventDamage()), f2(GetWidgetLife(t)))
                if first then
                    first = false
                    UnitDamageTarget(a, t, 50, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_UNIVERSAL, WEAPON_TYPE_WHOKNOWS)
                    say("outer again", f2(GetEventDamage()), f2(GetWidgetLife(t)))
                end
            end)
            UnitDamageTarget(a, t, 120, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_UNIVERSAL, WEAPON_TYPE_WHOKNOWS)
            say(f2(GetWidgetLife(t)))
            """);

        Assert.Equal(["outer 120.00 420.00", "outer 50.00 420.00", "outer again 120.00 370.00", "250.00"], Printed(simulation));
    }

    // facts: invulnerable fires DAMAGED reading 0; spell-immune fires DAMAGING only for the damage and attack types it stops, and loses nothing
    [Fact]
    public void Invulnerable_and_spell_immune_units_take_their_hits_as_the_facts_say()
    {
        using var simulation = Run("""
            local a, t, m = foot(), foot(), foot()
            SetUnitInvulnerable(t, true)
            watch(t)
            say(UnitDamageTarget(a, t, 100, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS), f2(GetWidgetLife(t)))
            UnitAddAbility(m, FourCC("ACmi"))
            watch(m)
            UnitDamageTarget(a, m, 100, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_MAGIC, WEAPON_TYPE_WHOKNOWS)
            UnitDamageTarget(a, m, 100, false, false, ATTACK_TYPE_MAGIC, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS)
            say(f2(GetWidgetLife(m)))
            UnitDamageTarget(a, m, 100, false, false, ATTACK_TYPE_PIERCE, DAMAGE_TYPE_UNIVERSAL, WEAPON_TYPE_WHOKNOWS)
            say(f2(GetWidgetLife(m)))
            """);

        Assert.Equal([
            "damaging 100.00 Footman", "damaged 0.00 420.00", "true 420.00",
            "damaging 100.00 Footman", "damaging 100.00 Footman", "420.00",
            "damaging 100.00 Footman", "damaged 100.00 420.00", "320.00",
        ], Printed(simulation));
    }

    // facts: the player-unit events fire for the target's owner, after the unit's own death-style order (player first)
    [Fact]
    public void The_player_unit_damage_events_see_every_hit_on_the_players_units()
    {
        using var simulation = Run("""
            local a, t = foot(), foot()
            local g = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(g, Player(0), EVENT_PLAYER_UNIT_DAMAGING, nil)
            TriggerAddAction(g, function() say("player damaging", GetEventDamage(), GetTriggerUnit() == t, BlzGetEventDamageTarget() == t) end)
            local d = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(d, Player(0), EVENT_PLAYER_UNIT_DAMAGED, nil)
            TriggerAddAction(d, function() say("player damaged", f2(GetEventDamage())) end)
            UnitDamageTarget(a, t, 10, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_UNIVERSAL, WEAPON_TYPE_WHOKNOWS)
            say(GetEventDamage(), GetEventDamageSource(), BlzGetEventDamageType())
            """);

        Assert.Equal(["player damaging 10.0 true true", "player damaged 10.00", "0.0 nil nil"], Printed(simulation));
    }

    // UnitDamagePoint: after its delay, each living unit within the radius takes the damage
    [Fact]
    public void A_point_hit_lands_after_its_delay_on_the_units_in_range()
    {
        using var simulation = Run("""
            local a, near, far = foot(), foot(100), foot(500)
            say(UnitDamagePoint(a, 1, 200, 0, 0, 30, false, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_UNIVERSAL, WEAPON_TYPE_WHOKNOWS))
            TimerStart(CreateTimer(), 0.5, false, function() say("early", f2(GetWidgetLife(near))) end)
            TimerStart(CreateTimer(), 1.5, false, function() say("late", f2(GetWidgetLife(a)), f2(GetWidgetLife(near)), f2(GetWidgetLife(far))) end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(["true", "early 420.00", "late 390.00 390.00 420.00"], Printed(simulation));
    }
}
