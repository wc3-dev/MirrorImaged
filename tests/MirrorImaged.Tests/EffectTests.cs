using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>Special effects and lightning against the facts' "Presentation" (game-facts.md, 10-19 to 11-12): what the getters read back.</summary>
public sealed class EffectTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    List<string> Run(string main, Action<Simulation>? after = null)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f2(x) return string.format("%.2f", x) end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: AbilityTests.Standard());
        after?.Invoke(simulation);
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // facts: made on the ground (raised by a crater: its z), x or y alone keeps the z, a height reads until a place is set, scale reads back
    [Fact]
    public void An_effect_reads_its_place_and_scale_back()
    {
        var lines = Run("""
            TerrainDeformCrater(512, 0, 256, -200, 0, true)
            TimerStart(CreateTimer(), 0.1, false, function()
                local e = AddSpecialEffect("Abilities\\Spells\\Human\\Thunderclap\\ThunderClapCaster.mdl", 512, 0)
                local flat = AddSpecialEffectLoc("x.mdl", Location(-1000, 0))
                print(f2(BlzGetLocalSpecialEffectX(e)), f2(BlzGetLocalSpecialEffectY(e)), f2(BlzGetLocalSpecialEffectZ(e)), f2(BlzGetLocalSpecialEffectZ(flat)), BlzGetSpecialEffectScale(e))
                BlzSetSpecialEffectX(e, -1000)
                print(f2(BlzGetLocalSpecialEffectZ(e)))
                BlzSetSpecialEffectHeight(e, 50)
                print(f2(BlzGetLocalSpecialEffectZ(e)))
                BlzSetSpecialEffectY(e, 3)
                print(f2(BlzGetLocalSpecialEffectZ(e)), f2(BlzGetLocalSpecialEffectY(e)))
                BlzSetSpecialEffectPosition(e, 1, 2, 7)
                BlzSetSpecialEffectScale(e, 2.5)
                print(f2(BlzGetLocalSpecialEffectX(e)), f2(BlzGetLocalSpecialEffectZ(e)), BlzGetSpecialEffectScale(e))
                DestroyEffect(e)
                print(BlzGetSpecialEffectScale(e), BlzGetSpecialEffectScale(nil))
            end)
            """, simulation => simulation.Advance(TimeSpan.FromSeconds(1)));

        Assert.Equal([
            "512.00\t0.00\t200.00\t0.00\t1.0",
            "200.00",
            "50.00",
            "200.00\t3.00",
            "1.00\t7.00\t2.5",
            "2.5\t0.0",
        ], lines);
    }

    // facts: an effect on a unit reads x, y and z 0 and scale 1 wherever the unit is
    [Fact]
    public void An_effect_on_a_unit_reads_zero_places()
    {
        var lines = Run("""
            local u = CreateUnit(Player(0), FourCC("hfoo"), 300, 400, 0)
            local e = AddSpecialEffectTarget("x.mdl", u, "overhead")
            SetUnitX(u, 900)
            print(BlzGetLocalSpecialEffectX(e), BlzGetLocalSpecialEffectY(e), BlzGetLocalSpecialEffectZ(e), BlzGetSpecialEffectScale(e))
            RemoveUnit(u)
            DestroyEffect(e)
            print("done")
            """);

        Assert.Equal(["0.0\t0.0\t0.0\t1.0", "done"], lines);
    }

    // facts: SetLightningColor answers true and the colour reads back, also after a move; a destroyed bolt answers false
    [Fact]
    public void A_bolt_reads_its_colour_back_and_dies_once()
    {
        var lines = Run("""
            local b = AddLightningEx("DRAL", false, 0, 0, 0, 100, 0, 0)
            print(GetLightningColorR(b), GetLightningColorA(b), SetLightningColor(b, 1, 0, 0, 0.5), MoveLightningEx(b, false, 0, 0, 0, 5, 5, 5))
            print(GetLightningColorR(b), GetLightningColorG(b), GetLightningColorB(b), GetLightningColorA(b))
            print(DestroyLightning(b), DestroyLightning(b), SetLightningColor(b, 0, 0, 0, 0), MoveLightning(b, false, 0, 0, 1, 1), AddLightning("", false, 0, 0, 1, 1))
            """);

        Assert.Equal(["1.0\t1.0\ttrue\ttrue", "1.0\t0.0\t0.0\t0.5", "true\tfalse\tfalse\tfalse\tnil"], lines);
    }

    [Fact]
    public void The_cinematic_filter_flag_and_hero_glow_read_back()
    {
        var lines = Run("""
            local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            print(IsCineFilterDisplayed(), HeroGlowIsAllowedOnUnit(u))
            DisplayCineFilter(true)
            DisallowHeroGlowOnUnit(u)
            print(IsCineFilterDisplayed(), HeroGlowIsAllowedOnUnit(u))
            AllowHeroGlowOnUnit(u)
            print(HeroGlowIsAllowedOnUnit(u), CreateTextTag() ~= nil)
            """);

        Assert.Equal(["false\ttrue", "true\tfalse", "true\ttrue"], lines);
    }
}
