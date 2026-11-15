using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>Transports: loading by the transport's order and by the unit's own, carrying, unloading, the room, and sinking with the ship.</summary>
public sealed class TransportTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static Field Column(string column, string type, params string[] specific) => new(column, PerLevel.Column, -1, type, false, [.. specific.Select(Ids.Of)]);

    static StandardData Standard()
    {
        var data = new StandardData();
        foreach (var (id, field) in new (string, Field)[]
        {
            ("alev", new("levels", PerLevel.None, -1, "int")), ("code", new("code", PerLevel.None, -1)), ("Dta1", Column("DataA", "unreal", "Sch5")),
        })
            data.Add(ObjectKind.Ability, id, field);
        return data
            .Add(ObjectKind.Ability, "Sch5", Values("code=Sch5;alev=1;Dta1:1=2"))
            .Add(ObjectKind.Unit, "hbot", Values("unam=Transport Ship;uhpm=500;umvs=300;umvt=float;uabi=Sch5;uaen=0;ucol=31"))
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;umvs=270;umvt=foot;uaen=0;ucol=31;udef=0"));
    }

    List<string> Run(string main, double seconds)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f0(x) return string.format("%.0f", x) end
            local function make(id, x, y) return CreateUnit(Player(0), FourCC(id), x, y, 0) end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    [Fact]
    public void A_transport_loads_up_to_its_room_carries_and_unloads_what_it_holds()
    {
        var lines = Run("""
            local ship = make("hbot", 0, 0)
            local a, b, c = make("hfoo", 100, 0), make("hfoo", 150, 0), make("hfoo", 200, 0)
            local t = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_LOADED, nil)
            TriggerAddAction(t, function() print("loaded", GetUnitName(GetLoadedUnit()), GetUnitName(GetTransportUnit())) end)
            print(IssueTargetOrder(ship, "load", a), IssueTargetOrder(ship, "load", b), IssueTargetOrder(ship, "load", c))
            print(IsUnitLoaded(a), IsUnitInTransport(b, ship), IsUnitLoaded(c))
            IssuePointOrder(ship, "move", 1000, 0)
            TimerStart(CreateTimer(), 5, false, function() print("carried", f0(GetUnitX(ship)) , f0(GetUnitX(a)), IsUnitLoaded(a)) IssueImmediateOrder(ship, "unloadall") end)
            TimerStart(CreateTimer(), 6, false, function() print("out", IsUnitLoaded(a), IsUnitLoaded(b), GetUnitX(a) > 900) end)
            """, 7);

        Assert.Equal(["loaded\tFootman\tTransport Ship", "loaded\tFootman\tTransport Ship", "true\ttrue\tfalse", "true\ttrue\tfalse", "carried\t1000\t1000\ttrue", "out\tfalse\tfalse\ttrue"], lines);
    }

    [Fact]
    public void A_unit_ordered_onto_a_transport_goes_in_and_cargo_dies_with_the_ship()
    {
        var lines = Run("""
            local ship = make("hbot", 0, 0)
            local a = make("hfoo", 400, 0)
            IssueTargetOrder(a, "smart", ship)
            TimerStart(CreateTimer(), 6, false, function() print("in", IsUnitLoaded(a)) KillUnit(ship) end)
            TimerStart(CreateTimer(), 7, false, function() print("sunk", UnitAlive and 0 or 0, GetWidgetLife(a) <= 0.405) end)
            """, 8);

        Assert.Equal(["in\ttrue", "sunk\t0\ttrue"], lines);
    }
}
