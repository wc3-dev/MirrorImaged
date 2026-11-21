using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// GetPlayerScore, against the probe units-scores (facts, "Players and the game"): units trained count each unit made, structures built
/// each structure made, a kill by a unit of an enemy counts as killed, razed or a hero killed, each adding the victim's gold and lumber
/// cost to the unit total, the hero total is 8 for each point of experience a kill gave the player's heroes, the resource total is the
/// gold mined, and the total adds the three; the most food made and used are kept; the rest read 0.
/// </summary>
public sealed class ScoreTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard()
    {
        var data = new StandardData()
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;ugol=135;ufoo=2;ulev=2;umvt=foot;ubld=20;uaen=0;umvs=270;ucol=31"))
            .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;uhpm=100;ugol=425;ulum=100;ufoo=5;ulev=1;ustr=22;uint=17;umpm=0;umpi=100"))
            .Add(ObjectKind.Unit, "hbar", Values("unam=Barracks;ubdg=1;uhpm=1500;ugol=160;ulum=60;ulev=3;utra=hfoo;ubld=10;ucol=80;umvs=0"))
            .Add(ObjectKind.Unit, "hhou", Values("unam=Farm;ubdg=1;uhpm=500;ugol=80;ulum=20;ubld=5;ufma=6;umvs=0"))
            .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;umvs=190;umvt=foot;ugol=75;ufoo=1;ucol=31;ubui=hbar,hhou;uaen=0"))
            .Add(ObjectKind.Unit, "htow", Values("unam=Town Hall;uhpm=1500;ubdg=1;utyp=townhall;ucol=100;umvs=0;ufma=12"))
            .Add(ObjectKind.Unit, "ngol", Values("unam=Gold Mine;ubdg=1;uhpm=2000;uabi=Agld;umvs=0;ucol=80"))
            .Add(ObjectKind.Ability, "Agld", Values("anam=Gold Mine;alev=1;Gld1:1=12500"));
        foreach (var (key, value) in Values("GrantNormalXP=25,40,60;GrantHeroXP=100,120,160;HeroExpRange=1200;StrHitPointBonus=25;IntManaBonus=15"))
            data.Constants[key] = value;
        return data;
    }

    // The scores of Player 0 after the script, as 25 numbers a line, one line a call of show
    List<string> Run(string main, double seconds = 1)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function scores() local s = {} for i = 0, 24 do s[#s + 1] = GetPlayerScore(Player(0), ConvertPlayerScore(i)) end return table.concat(s, " ") end
            local function hit(by, victim) UnitDamageTarget(by, victim, 100000, true, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS) end
            local enemy = Player(PLAYER_NEUTRAL_AGGRESSIVE)
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // The 25 scores in the order of PLAYER_SCORE_*'s numbers: trained, killed, built, razed, tech, food made, food used, heroes killed, items,
    // mercenaries, gold mined, mined after upkeep, lost to upkeep, lost to tax, given, received, lumber (five), unit total, hero total, resource total, total
    static string Scores(int trained = 0, int killed = 0, int built = 0, int razed = 0, int foodMade = 0, int foodUsed = 0, int heroesKilled = 0, int mined = 0,
        int unit = 0, int hero = 0) =>
        $"{trained} {killed} {built} {razed} 0 {foodMade} {foodUsed} {heroesKilled} 0 0 {mined} {mined} 0 0 0 0 0 0 0 0 0 {unit} {hero} {mined} {unit + hero + mined}";

    [Fact]
    public void Nothing_has_happened_at_the_start()
    {
        Assert.Equal([Scores()], Run("print(scores())"));
    }

    [Fact]
    public void Each_unit_and_structure_made_counts_with_its_cost_and_the_most_food_made_and_used_stays()
    {
        var lines = Run("""
            SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_FOOD_CAP, 100)
            CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            CreateUnit(Player(0), FourCC("Hpal"), 100, 0, 0)
            CreateUnit(Player(0), FourCC("hbar"), 500, 500, 0)
            CreateUnit(Player(0), FourCC("htow"), -500, 500, 0)
            print(scores())
            KillUnit(CreateUnit(Player(0), FourCC("hfoo"), 300, 0, 0))
            print(scores())
            """);

        // 135 + 525 + 220 + 0, the food made 100 and 12 for the hall, the used food 2 + 5 and, with a footman more, 9 at most
        Assert.Equal([Scores(trained: 2, built: 2, foodMade: 112, foodUsed: 7, unit: 880), Scores(trained: 3, built: 2, foodMade: 112, foodUsed: 9, unit: 1015)], lines);
    }

    [Fact]
    public void A_kill_by_a_unit_of_the_player_counts_the_victims_cost_and_gives_the_heroes_experience_8_times()
    {
        var lines = Run("""
            local foot = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            CreateUnit(Player(0), FourCC("Hpal"), 100, 0, 0)
            print(scores())
            hit(foot, CreateUnit(enemy, FourCC("hfoo"), 300, 300, 0))
            print(scores())
            hit(foot, CreateUnit(enemy, FourCC("Hpal"), 300, 300, 0))
            print(scores())
            hit(foot, CreateUnit(enemy, FourCC("hbar"), 300, 300, 0))
            print(scores())
            KillUnit(CreateUnit(enemy, FourCC("hfoo"), 300, 300, 0))
            print(scores())
            """);

        // a footman of level 2 gives 40, a hero of level 1 100, and neither a building (level 3, 60 if it did) nor a unit killed by no one does
        Assert.Equal([
            Scores(trained: 2, foodUsed: 7, unit: 135 + 525),
            Scores(trained: 2, killed: 1, foodUsed: 7, unit: 660 + 135, hero: 320),
            Scores(trained: 2, killed: 2, heroesKilled: 1, foodUsed: 7, unit: 795 + 525, hero: 320 + 800),
            Scores(trained: 2, killed: 2, razed: 1, heroesKilled: 1, foodUsed: 7, unit: 1320 + 220, hero: 1120),
            Scores(trained: 2, killed: 2, razed: 1, heroesKilled: 1, foodUsed: 7, unit: 1540, hero: 1120)], lines);
    }

    [Fact]
    public void A_kill_of_an_own_or_allied_unit_counts_nothing()
    {
        var lines = Run("""
            local foot = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            CreateUnit(Player(0), FourCC("Hpal"), 100, 0, 0)
            hit(foot, CreateUnit(Player(0), FourCC("hfoo"), 300, 300, 0))
            print(scores())
            """);

        Assert.Equal([Scores(trained: 3, foodUsed: 9, unit: 135 + 525 + 135)], lines);
    }

    [Fact]
    public void A_unit_trained_or_a_structure_built_by_orders_counts_when_it_finishes()
    {
        var lines = Run("""
            local p = Player(0)
            SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 1000)
            SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, 500)
            SetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP, 20)
            local bar = CreateUnit(p, FourCC("hbar"), -300, 0, 0)
            local w = CreateUnit(p, FourCC("hpea"), 0, 0, 0)
            IssueImmediateOrderById(bar, FourCC("hfoo"))
            IssueBuildOrder(w, "hhou", 600, 0)
            TimerStart(CreateTimer(), 3, false, function() print(scores()) end)
            TimerStart(CreateTimer(), 10, false, function() print(scores()) end)
            TimerStart(CreateTimer(), 25, false, function() print(scores()) end)
            """, 26);

        // the barracks and the peasant made by the script at once; the farm (80 + 20) when it is done, and the footman (135) when its 20 s are
        Assert.Equal([Scores(trained: 1, built: 1, foodMade: 20, foodUsed: 1, unit: 295), Scores(trained: 1, built: 2, foodMade: 26, foodUsed: 3, unit: 395), Scores(trained: 2, built: 2, foodMade: 26, foodUsed: 3, unit: 530)], lines);
    }

    [Fact]
    public void The_gold_a_worker_delivers_is_the_gold_mined_and_the_resource_total()
    {
        var lines = Run("""
            local p = Player(0)
            CreateUnit(p, FourCC("htow"), 0, 0, 0)
            local mine = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("ngol"), 800, 0, 0)
            SetResourceAmount(mine, 25)
            local w = CreateUnit(p, FourCC("hpea"), 200, 0, 0)
            IssueTargetOrder(w, "harvest", mine)
            TimerStart(CreateTimer(), 60, false, function() print(scores()) end)
            """, 61);

        Assert.Equal([Scores(trained: 1, built: 1, foodMade: 12, foodUsed: 1, mined: 25, unit: 75)], lines);
    }
}
