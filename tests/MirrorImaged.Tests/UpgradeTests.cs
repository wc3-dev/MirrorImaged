using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>A building that upgrades into another, and an altar that revives a dead hero.</summary>
public sealed class UpgradeTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "htow", Values("unam=Town Hall;uhpm=1100;ubdg=1;ugol=385;ulum=205;ubld=60;ufma=12;utra=hpea;uupt=hkee;umvs=0;ucol=80"))
        .Add(ObjectKind.Unit, "hkee", Values("unam=Keep;uhpm=1500;ubdg=1;ugol=320;ulum=210;ubld=140;ufma=12;utra=hpea;uupt=hcas;umvs=0;ucol=80"))
        .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;umvs=190;ugol=75;ufoo=1;ubld=15;uaen=0;ucol=31"))
        .Add(ObjectKind.Unit, "halt", Values("unam=Altar;uhpm=900;ubdg=1;ugol=180;ulum=50;ubld=60;utra=Hpal;urev=Hpal;umvs=0;ucol=80"))
        .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;uhpm=100;umvs=320;ugol=425;ufoo=5;ubld=55;uaen=0;ucol=31;upra=STR;ustr=22;uagi=13;uint=17;ustp=2.7;uagp=1.5;uinp=1.8"));

    List<string> Run(string main, double seconds)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local p = Player(0)
            function config() end
            function main()
                SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 2000)
                SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, 1000)
                SetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP, 40)
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    [Fact]
    public void A_town_hall_upgrades_into_a_keep_that_keeps_its_share_of_life_and_cannot_train_meanwhile()
    {
        var lines = Run("""
            local hall = CreateUnit(p, FourCC("htow"), 0, 0, 0)
            SetUnitState(hall, UNIT_STATE_LIFE, 550)
            print(IssueImmediateOrderById(hall, FourCC("hkee")), GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD))
            print(IssueImmediateOrderById(hall, FourCC("hpea")), GetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP))
            TimerStart(CreateTimer(), 100, false, function() print("early", GetUnitName(hall)) end)
            TimerStart(CreateTimer(), 150, false, function() print("done", GetUnitName(hall), GetUnitState(hall, UNIT_STATE_LIFE), GetUnitState(hall, UNIT_STATE_MAX_LIFE), GetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP), GetUnitTypeId(hall) == FourCC("hkee")) end)
            """, 160);

        Assert.Equal(["true\t1680", "false\t52", "early\tTown Hall", "done\tKeep\t750.0\t1500.0\t52\ttrue"], lines);
    }

    [Fact]
    public void An_altar_revives_a_dead_hero_for_a_share_of_its_price_and_then_does_not_train_another()
    {
        var lines = Run("""
            local altar = CreateUnit(p, FourCC("halt"), 0, 0, 0)
            local hero = CreateUnit(p, FourCC("Hpal"), 300, 0, 0)
            print(IssueImmediateOrderById(altar, FourCC("Hpal")), GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD))
            KillUnit(hero)
            print(IssueImmediateOrderById(altar, FourCC("Hpal")), GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD))
            TimerStart(CreateTimer(), 100, false, function() print("alive", IsUnitAliveBJ and 0 or 0, GetUnitState(hero, UNIT_STATE_LIFE) > 0) end)
            """, 110);

        Assert.Equal(["false\t2000", "true\t1830", "alive\t0\ttrue"], lines);
    }
}
