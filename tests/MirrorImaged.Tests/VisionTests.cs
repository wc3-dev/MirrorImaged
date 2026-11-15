using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>What a player sees, fog modifiers and selection against the facts' "Presentation" (game-facts.md, 10-09, 11-06) and the fog flags.</summary>
public sealed class VisionTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData().Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;usid=1000;usin=1000;uaen=0"));

    List<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function foot(p, x, y) return CreateUnit(p or Player(0), FourCC("hfoo"), x or 0, y or 0, 0) end
            local function at(x, y, p)
                p = p or Player(0)
                return IsVisibleToPlayer(x, y, p) and "visible" or IsFoggedToPlayer(x, y, p) and "fogged" or IsMaskedToPlayer(x, y, p) and "masked" or "?"
            end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // facts: a point is visible within a unit's sight, masked where nothing was seen, fogged where something was
    [Fact]
    public void A_place_is_visible_near_a_unit_fogged_after_it_leaves_and_masked_before_it_comes()
    {
        var lines = Run("""
            local u = foot()
            print(at(500, 0), at(1300, 0), at(3000, 0), IsUnitVisible(u, Player(0)), IsUnitVisible(u, Player(1)), IsUnitMasked(u, Player(1)))
            SetUnitX(u, 3000)
            print(at(3000, 0), at(500, 0), at(-3000, 0), IsUnitFogged(foot(Player(1), -3000, 0), Player(0)))
            SetFogStateRadius(Player(0), FOG_OF_WAR_VISIBLE, -1000, 1000, 200, false)
            print(at(-1000, 1000), IsLocationMaskedToPlayer(Location(-1000, 1000), Player(0)))
            SetFogStateRadius(Player(0), FOG_OF_WAR_MASKED, 500, 0, 100, false)
            print(at(500, 0))
            """);

        Assert.Equal(["visible\tmasked\tmasked\ttrue\tfalse\ttrue","visible\tfogged\tmasked\tfalse", "fogged\tfalse", "masked"], lines);
    }

    // facts: with fog off, what was seen reads visible and what never was masked; with the mask off unseen ground reads fogged; both off, visible
    [Fact]
    public void The_fog_and_mask_flags_change_what_is_read()
    {
        var lines = Run("""
            local u = foot()
            SetUnitX(u, 3000)
            print(at(0, 0), at(-3000, 0))
            FogEnable(false)
            print(at(0, 0), at(-3000, 0))
            FogEnable(true)
            FogMaskEnable(false)
            print(at(0, 0), at(-3000, 0))
            FogEnable(false)
            print(at(0, 0), at(-3000, 0))
            """);

        Assert.Equal(["fogged\tmasked", "visible\tmasked", "fogged\tfogged", "visible\tvisible"], lines);
    }

    // facts: a fog modifier shows its state while it runs, the later started of two on one place wins, a stopped one leaves its area explored and out of sight
    [Fact]
    public void Fog_modifiers_show_their_state_while_they_run()
    {
        var lines = Run("""
            local show = CreateFogModifierRadius(Player(0), FOG_OF_WAR_VISIBLE, 1800, 0, 150, false, false)
            local hide = CreateFogModifierRect(Player(0), FOG_OF_WAR_MASKED, Rect(1750, -50, 1850, 50), false, false)
            print(at(1800, 0))
            FogModifierStart(show)
            print(at(1800, 0), at(1900, 0), at(1990, 0))
            FogModifierStart(hide)
            print(at(1800, 0), at(1900, 0))
            FogModifierStop(hide)
            print(at(1800, 0))
            FogModifierStop(show)
            print(at(1800, 0), at(1900, 0))
            DestroyFogModifier(show)
            DestroyFogModifier(hide)
            print(CreateFogModifierRadius(nil, FOG_OF_WAR_VISIBLE, 0, 0, 1, false, false))
            """);

        Assert.Equal(["masked", "visible\tvisible\tmasked", "masked\tvisible", "visible", "fogged\tfogged", "nil"], lines);
    }

    // sight is shared by an alliance's shared vision and by UnitShareVision; selection is the player's own set
    [Fact]
    public void Sight_is_shared_and_units_are_selected_per_player()
    {
        var lines = Run("""
            local u = foot(Player(0), 0, 0)
            print(at(100, 0, Player(1)))
            UnitShareVision(u, Player(1), true)
            print(at(100, 0, Player(1)))
            UnitShareVision(u, Player(1), false)
            SetPlayerAlliance(Player(0), Player(1), ALLIANCE_SHARED_VISION, true)
            print(at(100, 0, Player(1)), at(100, 0, Player(2)))
            local everyone = CreateForce()
            ForceAddPlayer(everyone, Player(0))
            print(IsUnitSelected(u, Player(0)))
            SelectUnit(u, true)
            local g = CreateGroup()
            GroupEnumUnitsSelected(g, Player(0), nil)
            print(IsUnitSelected(u, Player(0)), IsUnitSelected(u, Player(1)), BlzGroupGetSize(g))
            ClearSelection()
            print(IsUnitSelected(u, Player(0)), IsUnitInForce(u, everyone))
            """);

        Assert.Equal(["masked", "visible", "visible\tmasked", "false", "true\tfalse\t1", "false\ttrue"], lines);
    }
}
