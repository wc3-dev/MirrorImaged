using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>What a kill gives: the bounty to the killer's owner and experience to its heroes in range.</summary>
public sealed class RewardTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard()
    {
        var data = new StandardData()
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100;umvs=270;uaen=0;ulev=2;ubba=3;ubdi=0;ubsi=1"))
            .Add(ObjectKind.Unit, "ngnl", Values("unam=Gnoll;uhpm=50;uaen=0;ulev=2;ubba=10;ubdi=2;ubsi=1"))
            .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;uhpm=100;ustr=22;uagi=13;uint=17;upra=STR;uaen=0;ulev=5;umvs=270"));
        foreach (var (key, value) in Values("GrantNormalXP=25,40,60;GrantHeroXP=100,120,160;HeroExpRange=1200;NeedHeroXP=200,500;NeedHeroXPFormulaA=1;NeedHeroXPFormulaB=100;NeedHeroXPFormulaC=0;StrHitPointBonus=25;IntManaBonus=15;AgiDefenseBase=-2;AgiDefenseBonus=0.3"))
            data.Constants[key] = value;
        return data;
    }

    [Fact]
    public void A_kill_gives_the_bounty_and_shares_experience_among_the_heroes_in_range()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
                local p = Player(0)
                SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 0)
                local killer = CreateUnit(p, FourCC("hfoo"), 0, 0, 0)
                local h1, h2, far = CreateUnit(p, FourCC("Hpal"), 100, 0, 0), CreateUnit(p, FourCC("Hpal"), -100, 0, 0), CreateUnit(p, FourCC("Hpal"), 5000, 0, 0)
                local creep = CreateUnit(Player(PLAYER_NEUTRAL_AGGRESSIVE), FourCC("ngnl"), 50, 50, 0)
                UnitDamageTarget(killer, creep, 1000, true, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS)
                print(GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD) >= 12, GetHeroXP(h1), GetHeroXP(h2), GetHeroXP(far))
                local enemy = CreateUnit(Player(1), FourCC("hfoo"), 0, 100, 0)
                UnitDamageTarget(killer, enemy, 1000, true, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS)
                print(GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD) >= 12, GetHeroXP(h1))
            end
            """), "", data: Standard());
        var lines = simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..]).ToList();

        Assert.Equal(["true\t20\t20\t0", "true\t40"], lines);
    }
}
