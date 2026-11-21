using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>What the melee AI scripts ask before they attack, the units they make from others, the creeps' posts and the computers' difficulty.</summary>
public sealed class AiTownTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    const string Armed = "umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=10;ua1d=0;ua1s=1;ua1c=1;ua1r=90;udp1=0.5;udef=0;udty=normal";

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;umvs=190;umvt=foot;utyp=peon;ucol=31;ugol=75;ufoo=1;ubld=15;ubui=htow"))
        .Add(ObjectKind.Unit, "htow", Values("unam=Town Hall;uhpm=1500;ubdg=1;utyp=townhall;ucol=100;umvs=0;utra=hpea;uupt=hkee;ugol=385;ubld=10"))
        .Add(ObjectKind.Unit, "hkee", Values("unam=Keep;uhpm=2000;ubdg=1;utyp=townhall;ucol=100;umvs=0;utra=hpea;ugol=320;ulum=210;ubld=10"))
        .Add(ObjectKind.Unit, "halt", Values("unam=Altar;uhpm=900;ubdg=1;ucol=80;umvs=0;utra=Hpal;urev=1"))
        .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;uhpm=100;ustr=22;uint=17;umvs=270;ugol=425;ubld=20;ufoo=5"))
        .Add(ObjectKind.Unit, "ngol", Values("unam=Gold Mine;ubdg=1;uhpm=2000;uabi=Agld;umvs=0;ucol=80"))
        .Add(ObjectKind.Ability, "Agld", Values("anam=Gold Mine;alev=1;Gld1:1=12500"))
        .Add(ObjectKind.Unit, "uaco", Values("unam=Acolyte;uhpm=220;umvs=235;umvt=foot;utyp=peon;ucol=16;ugol=75;ufoo=1;ubld=15;ubui=ugol"))
        .Add(ObjectKind.Unit, "ugol", Values("unam=Haunted Gold Mine;ubdg=1;uhpm=800;uabi=Abgm;umvs=0;ucol=80;ugol=225;ulum=210;ubld=5"))
        .Add(ObjectKind.Ability, "Abgm", Values("anam=Blighted Gold Mine;alev=1"))
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100000;umvs=200;ucol=31;ugol=135;ufoo=2;ubld=20;" + Armed))
        .Add(ObjectKind.Unit, "hwtw", Values("unam=Guard Tower;uhpm=500;ubdg=1;umvs=0;ucol=60;" + Armed))
        .Add(ObjectKind.Unit, "ngno", Values("unam=Gnoll;uhpm=1000;umvs=300;ucol=31;" + Armed))
        .Add(ObjectKind.Unit, "earc", Values("unam=Archer;uhpm=245;umvs=270;ucol=31"))
        .Add(ObjectKind.Unit, "ehip", Values("unam=Hippogryph;uhpm=500;umvs=350;ucol=31"))
        .Add(ObjectKind.Unit, "ehpr", Values("unam=Hippogryph Rider;uhpm=765;umvs=350;ucol=31;ugol=0"))
        .Add(ObjectKind.Unit, "uobs", Values("unam=Obsidian Statue;uhpm=550;umvs=270;ucol=31;uabi=Aave"))
        .Add(ObjectKind.Unit, "ubsp", Values("unam=Destroyer;uhpm=900;umvs=320;ucol=31;ugol=100;ulum=50"))
        .Add(ObjectKind.Ability, "Aave", Values("anam=Destroyer Form;alev=1;Emeu=ubsp"));

    const string Foes = """
        local p, e = Player(0), Player(1)
        SetPlayerAlliance(p, e, ALLIANCE_PASSIVE, false)
        SetPlayerAlliance(e, p, ALLIANCE_PASSIVE, false)
        local function id(s) return FourCC(s) end
        """;

    List<string> Run(string main, double seconds = 0)
    {
        using var simulation = Simulation.Load(_maps.Build("function config() end\nfunction main()\n" + Foes + "\n" + main + "\nend"), "", data: Standard());
        if (seconds > 0) simulation.Advance(TimeSpan.FromSeconds(seconds));
        Assert.Null(simulation.Stopped);
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    [Fact]
    public void A_player_has_a_queue_of_commands_the_map_sent_it_that_its_script_reads_and_pops()
    {
        var lines = Run("""
            print(CommandsWaiting(), GetLastCommand(), GetLastData())
            CommandAI(p, 5, 50)
            CommandAI(p, 6, 60)
            CommandAI(e, 7, 70)
            print(CommandsWaiting(), GetLastCommand(), GetLastData())
            PopLastCommand()
            print(CommandsWaiting(), GetLastCommand(), GetLastData())
            PopLastCommand()
            PopLastCommand()
            print(CommandsWaiting(), GetLastCommand(), GetLastData())
            """);

        Assert.Equal(["0\t0\t0", "2\t5\t50", "1\t6\t60", "0\t0\t0"], lines);
    }

    [Fact]
    public void A_town_is_threatened_while_an_armed_enemy_stands_near_one_of_its_halls()
    {
        var lines = Run("""
            print(TownThreatened())
            CreateUnit(p, id("htow"), 0, 0, 0)
            local foe = CreateUnit(e, id("hfoo"), 5000, 0, 0)
            local worker = CreateUnit(e, id("hpea"), 500, 0, 0)
            CreateUnit(Player(PLAYER_NEUTRAL_AGGRESSIVE), id("ngno"), 0, 700, 0)
            print(TownThreatened())
            SetUnitPosition(foe, 900, 0)
            print(TownThreatened())
            KillUnit(foe)
            print(TownThreatened())
            """);

        // No hall, then an enemy far off with an unarmed one and a creep near, then the armed enemy near, then dead
        Assert.Equal(["false", "false", "true", "false"], lines);
    }

    [Fact]
    public void A_target_is_towered_while_an_enemy_building_that_attacks_stands_near_it()
    {
        var lines = Run("""
            local hall = CreateUnit(e, id("htow"), 3000, 0, 0)
            CreateUnit(e, id("hfoo"), 3100, 0, 0)
            CreateUnit(p, id("hwtw"), 3000, 300, 0)
            print(IsTowered(hall), IsTowered(nil))
            local tower = CreateUnit(e, id("hwtw"), 3000, -800, 0)
            print(IsTowered(hall))
            SetUnitPosition(tower, 3000, -2000)
            print(IsTowered(hall))
            """);

        // The enemy's footman and the AI player's own tower don't count; the enemy's tower does within 900
        Assert.Equal(["false\tfalse", "true", "false"], lines);
    }

    [Fact]
    public void Creeps_are_on_the_map_while_one_lives_and_a_plain_unit_is_not_hidden()
    {
        var lines = Run("""
            print(CreepsOnMap())
            local creep = CreateUnit(Player(PLAYER_NEUTRAL_AGGRESSIVE), id("ngno"), 0, 0, 0)
            print(CreepsOnMap(), UnitInvis(creep), UnitInvis(nil))
            KillUnit(creep)
            print(CreepsOnMap())
            """);

        Assert.Equal(["false", "true\tfalse\tfalse", "false"], lines);
    }

    [Fact]
    public void An_archer_and_a_hippogryph_merge_and_a_statue_takes_its_other_form_for_its_price()
    {
        var lines = Run("""
            SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 150)
            SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, 500)
            CreateUnit(p, id("earc"), 100, 0, 0)
            print(MergeUnits(1, id("earc"), id("ehip"), id("ehpr")), GetUnitCount(id("earc")))
            local mount = CreateUnit(p, id("ehip"), 200, 300, 0)
            print(MergeUnits(1, id("earc"), id("ehip"), id("ehpr")), GetUnitCount(id("ehpr")), GetUnitCount(id("earc")), GetUnitCount(id("ehip")))
            print(MergeUnits(1, id("earc"), id("ehip"), id("ehpr")))
            for i = 1, 3 do CreateUnit(p, id("uobs"), 400, i * 100, 0) end
            print(ConvertUnits(3, id("uobs")), GetUnitCount(id("ubsp")), GetUnitCount(id("uobs")), GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD), GetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER))
            print(ConvertUnits(1, id("uobs")), ConvertUnits(1, id("hpea")), ConvertUnits(1, id("earc")))
            """);

        Assert.Equal([
            "false\t1",          // no hippogryph to mount
            "true\t1\t0\t0",     // the pair became a rider
            "true",              // it has the one asked for: nothing more is made
            "false\t1\t2\t50\t450", // gold for one Destroyer (100 gold, 50 lumber) of the three asked for
            "true\tfalse\tfalse",   // it has one; a type with no other form, and one the player has none of, convert nothing
        ], lines);
    }

    [Fact]
    public void A_creep_walks_back_to_its_post_and_follows_no_further_than_the_guard_return_distance()
    {
        var lines = Run("""
            local hostile = Player(PLAYER_NEUTRAL_AGGRESSIVE)
            local moved = CreateUnit(hostile, id("ngno"), 0, 3000, 0)
            local freed = CreateUnit(hostile, id("ngno"), 0, 4000, 0)
            local posted = CreateUnit(hostile, id("ngno"), 0, -1500, 0)
            local chaser = CreateUnit(hostile, id("ngno"), 0, 0, 0)
            local runner = CreateUnit(p, id("hfoo"), 300, 0, 0)
            local far = 0
            TimerStart(CreateTimer(), 1, false, function()
                SetUnitPosition(moved, 600, 3000)
                SetUnitPosition(freed, 600, 4000)
                RemoveGuardPosition(freed)
                SetUnitPosition(posted, 600, -1500)
                RecycleGuardPosition(posted)
                IssuePointOrder(runner, "move", 1900, 0)
            end)
            TimerStart(CreateTimer(), 0.25, true, function() far = math.max(far, GetUnitX(chaser)) end)
            TimerStart(CreateTimer(), 40, false, function()
                print(math.abs(GetUnitX(moved)) < 70, GetUnitX(freed), GetUnitX(posted))
                print(far > 600, far < 1100, math.abs(GetUnitX(chaser)) < 70, GetUnitX(runner) > 1800)
            end)
            """, 41);

        Assert.Equal(["true\t600.0\t600.0", "true\ttrue\ttrue\ttrue"], lines);
    }

    [Fact]
    public void A_unit_guards_a_post_once_told_to_and_a_creep_stops_once_told_not_to()
    {
        var lines = Run("""
            local own = CreateUnit(p, id("ngno"), 0, 0, 0)
            local creep = CreateUnit(Player(PLAYER_NEUTRAL_AGGRESSIVE), id("ngno"), 0, 3000, 0)
            SetUnitCreepGuard(own, true)
            SetUnitCreepGuard(creep, false)
            TimerStart(CreateTimer(), 1, false, function()
                SetUnitPosition(own, 600, 0)
                SetUnitPosition(creep, 600, 3000)
            end)
            TimerStart(CreateTimer(), 20, false, function() print(math.abs(GetUnitX(own)) < 70, GetUnitX(creep)) end)
            """, 21);

        Assert.Equal(["true\t600.0"], lines);
    }

    [Fact]
    public void The_ai_makes_a_keep_out_of_its_town_hall_and_goes_on_past_what_nothing_of_its_trains()
    {
        var lines = Run("""
            SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 1000)
            SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, 1000)
            local hall = CreateUnit(p, id("htow"), 0, 0, 0)
            CreateUnit(p, id("hpea"), 200, 0, 0)
            print(SetProduce(1, id("hfoo"), -1), GetUnitCount(id("hfoo")))
            print(SetProduce(1, id("hkee"), 0), GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD))
            TimerStart(CreateTimer(), 12, false, function() print(GetUnitTypeId(hall) == id("hkee"), GetUnitCount(id("hkee")), GetUnitCount(id("htow"))) end)
            """, 13);

        // No barracks: the footman isn't waited for. The Keep is the hall's own upgrade, paid for at once, and no second hall is built
        Assert.Equal(["true\t0", "true\t680", "true\t1\t0"], lines);
    }

    [Fact]
    public void The_ai_revives_its_dead_hero_at_a_building_the_data_marks_as_reviving()
    {
        var lines = Run("""
            SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 1000)
            SetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP, 20)
            CreateUnit(p, id("halt"), 0, 0, 0)
            local hero = CreateUnit(p, id("Hpal"), 300, 0, 0)
            KillUnit(hero)
            print(GetUnitCount(id("Hpal")), SetProduce(1, id("Hpal"), -1))
            TimerStart(CreateTimer(), 30, false, function() print(GetUnitCount(id("Hpal")), GetUnitState(hero, UNIT_STATE_LIFE) > 0) end)
            """, 31);

        Assert.Equal(["0\ttrue", "1\ttrue"], lines);
    }

    [Fact]
    public void One_worker_at_a_time_goes_to_build_the_expansion()
    {
        var lines = Run("""
            SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 1000)
            CreateUnit(p, id("htow"), 0, 0, 0)
            local mine = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), id("ngol"), 1500, 0, 0)
            SetResourceAmount(mine, 5000)
            local a, b = CreateUnit(p, id("hpea"), 200, 0, 0), CreateUnit(p, id("hpea"), 200, 100, 0)
            print(SetExpansion(a, id("htow")), GetUnitCurrentOrder(a) == id("htow"))
            print(SetExpansion(b, id("htow")), GetUnitCurrentOrder(b) == id("htow"))
            """);

        Assert.Equal(["true\ttrue", "true\tfalse"], lines);
    }

    [Fact]
    public void A_town_stays_where_its_hall_died_and_its_gold_is_what_its_mines_hold()
    {
        var lines = Run("""
            SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 1000)
            local hall = CreateUnit(p, id("htow"), 0, 0, 0)
            SetResourceAmount(CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), id("ngol"), 500, 0, 0), 4000)
            SetResourceAmount(CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), id("ngol"), 5000, 0, 0), 7000)
            CreateUnit(p, id("hpea"), 200, 0, 0)
            print(TownHasHall(0), TownHasMine(0), GetMinesOwned(), GetGoldOwned(), TownWithMine())
            KillUnit(hall)
            print(TownHasHall(0), TownHasMine(0), GetMinesOwned(), GetGoldOwned(), TownWithMine())
            print(SetProduce(1, id("htow"), 0))
            TimerStart(CreateTimer(), 20, false, function() print(TownHasHall(0), TownHasHall(1), GetMinesOwned(), GetGoldOwned(), GetUnitCount(id("htow"))) end)
            """, 21);

        Assert.Equal([
            "true\ttrue\t1\t4000\t0",   // the mine beside the hall is the one it has; the far one is nobody's
            "false\ttrue\t0\t0\t-1",    // the hall gone, the town is still town 0 and still has a mine, which the player no longer works
            "true",                        // so a hall is built there again, as the scripts ask (MeleeTownHall)
            "true\tfalse\t1\t4000\t1",
        ], lines);
    }

    [Fact]
    public void The_next_expansion_is_the_nearest_free_mine_the_towns_land_reaches()
    {
        // A ring of unwalkable cells round the near mine (1500, 0): the far one (-1800, 0) is the one to walk to
        var lines = Run("""
            for i = -12, 12 do
                for _, j in ipairs({ -12, 12 }) do
                    SetTerrainPathable(1500 + i * 32, j * 32, PATHING_TYPE_WALKABILITY, false)
                    SetTerrainPathable(1500 + j * 32, i * 32, PATHING_TYPE_WALKABILITY, false)
                end
            end
            CreateUnit(p, id("htow"), 0, 0, 0)
            SetResourceAmount(CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), id("ngol"), 1500, 0, 0), 5000)
            SetResourceAmount(CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), id("ngol"), -1800, 0, 0), 5000)
            print(GetNextExpansion(), GetExpansionX(), GetExpansionY())
            """, 1);

        Assert.Equal(["0	-1800	0"], lines);
    }

    [Fact]
    public void A_haunted_mine_is_made_of_the_gold_mine_and_is_a_town_of_its_own()
    {
        var lines = Run("""
            SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 1000)
            SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, 1000)
            CreateUnit(p, id("htow"), 0, 0, 0)
            local mine = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), id("ngol"), 1500, 0, 0)
            SetResourceAmount(mine, 5000)
            local a, b = CreateUnit(p, id("uaco"), 1300, 0, 0), CreateUnit(p, id("uaco"), -200, -1400, 0)
            print(GetNextExpansion(), SetExpansion(a, id("ugol")), IssueBuildOrderById(b, id("ugol"), -200, -1500))
            TimerStart(CreateTimer(), 10, false, function()
                local g = CreateGroup()
                GroupEnumUnitsOfPlayer(g, p, nil)
                local haunted
                for i = 0, BlzGroupGetSize(g) - 1 do if GetUnitTypeId(BlzGroupUnitAt(g, i)) == id("ugol") then haunted = BlzGroupUnitAt(g, i) end end
                print(GetUnitCount(id("ugol")), GetUnitX(haunted), GetResourceAmount(haunted), GetUnitTypeId(mine), GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD))
                print(TownHasHall(1), TownHasMine(1), GetMinesOwned(), GetGoldOwned(), GetNextExpansion())
            end)
            """, 11);

        Assert.Equal([
            "0\ttrue\ttrue",
            "1\t1500.0\t5000\t0\t775",   // one haunted mine, on the gold mine, with its gold, the gold mine gone; none was made or paid for where no mine stands
            "false\ttrue\t1\t5000\t-1",  // town 1 is the mine with no hall, the player's own, so it counts, and no mine is left to expand to
        ], lines);
    }

    // D821: Korea's site, 157 off the mine toward the town, touched the mine's footprint, so the search for the acolyte's way round a cliff never ended there and stopped at 40,000 cells (D822)
    [Fact]
    public void A_haunted_mine_site_moves_off_the_gold_mine_s_footprint_so_a_way_round_a_wall_ends_on_it()
    {
        var data = Standard()
            .Add(ObjectKind.Unit, "ngol", Values("unam=Gold Mine;ubdg=1;uhpm=2000;uabi=Agld;umvs=0;ucol=50;upat=Goldmine.tga"))
            .Add(ObjectKind.Unit, "ugol", Values("unam=Haunted Gold Mine;ubdg=1;uhpm=800;uabi=Abgm;umvs=0;ucol=50;ugol=225;ulum=210;ubld=5"));
        // the game's gold mine: 16 cells across, the middle 8 unwalkable
        data.Footprints["Goldmine.tga"] = new Footprint(16, 16, [.. Enumerable.Range(0, 256).Select(i => i % 16 is >= 4 and < 12 && i / 16 is >= 4 and < 12)]);
        var map = _maps.Build("function config() end\nfunction main()\n" + Foes + "\n" + """
            SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 1000)
            SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, 1000)
            CreateUnit(p, id("htow"), 1500, 300, 0)
            SetResourceAmount(CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), id("ngol"), 0, 0, 0), 5000)
            print(SetExpansion(CreateUnit(p, id("uaco"), 1000, 0, 0), id("ugol")))
            TimerStart(CreateTimer(), 120, false, function() print(GetUnitCount(id("ugol"))) end)
            end
            """, map: m =>
        {
            // a map 96 tiles across (-6144 to 6144) and a wall of unwalkable cells at x 288 to 352 from its bottom edge to y 4992, between the
            // acolyte and the site
            m.Info = War3Net.Build.MapFactory.Info(96, 96);
            m.Info.ScriptLanguage = War3Net.Build.Info.ScriptLanguage.Lua;
            m.Environment = War3Net.Build.MapFactory.Environment(m.Info);
            var pathing = new War3Net.Build.Environment.MapPathingMap(War3Net.Build.Environment.MapPathingMapFormatVersion.v0) { Width = 384, Height = 384 };
            for (var i = 0; i < 384 * 384; i++) pathing.Cells.Add(0);
            for (var row = 0; row <= 348; row++)
                foreach (var column in new[] { 201, 202 }) pathing.Cells[row * 384 + column] = War3Net.Build.Environment.PathingType.Walk;
            // and walls along y -600 and 600 from it to x 2500, a bay the acolyte stands in, whose end is the nearest place to the site
            for (var column = 201; column <= 270; column++)
                foreach (var row in new[] { 173, 206 }) pathing.Cells[row * 384 + column] = War3Net.Build.Environment.PathingType.Walk;
            m.PathingMap = pathing;
        });
        using var simulation = Simulation.Load(map, "", data: data);
        simulation.Advance(TimeSpan.FromSeconds(121));

        Assert.Equal(["true", "1"], simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..]));
    }

    [Fact]
    public void A_player_s_town_halls_are_counted_whatever_their_tier_and_while_they_are_built()
    {
        var lines = Run("""
            SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 1000)
            local hall = CreateUnit(p, id("htow"), 0, 0, 0)
            CreateUnit(p, id("hkee"), 1500, 0, 0)
            CreateUnit(p, id("halt"), 0, 1500, 0)
            print(BlzGetPlayerTownHallCount(p), BlzGetPlayerTownHallCount(e), BlzGetPlayerTownHallCount(nil))
            IssueBuildOrderById(CreateUnit(p, id("hpea"), -1500, 0, 0), id("htow"), -1500, 50)
            TimerStart(CreateTimer(), 1, false, function()
                KillUnit(hall)
                print(BlzGetPlayerTownHallCount(p), GetUnitCountDone(id("htow")))
            end)
            """, 2);

        Assert.Equal(["2\t0\t0", "2\t0"], lines);
    }
}
