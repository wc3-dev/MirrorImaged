namespace MirrorImaged.Natives;

// What a kill gives: the bounty of the victim's type to the killer's owner when the victim's owner gives bounty, and experience, shared by the
// killer's owner's heroes within the experience range of the victim, from the constants' lists by the victim's level (decisions D360). Close: the
// bounty fields, the shares and the range are the data's and the common ones, not measured; summoned units and the upkeep penalty aren't counted.
public sealed partial class GameNatives
{
    static readonly int BountyBase = Code("ubba"), BountyDice = Code("ubdi"), BountySides = Code("ubsi");

    int Listed(string key, int level, int otherwise)
    {
        var list = (Data.Constant(key) ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return list.Length == 0 ? otherwise : int.TryParse(list[Math.Clamp(level - 1, 0, list.Length - 1)], out var value) ? value : otherwise;
    }

    void Reward(unit killer, unit victim)
    {
        if (killer.Owner == victim.Owner || !IsPlayerEnemy(killer.Owner, victim.Owner)) return;
        ScoreKill(killer.Owner, victim);
        if (victim.Owner.States[GivesBounty] != 0)
        {
            var gold = victim.Type.Integer(BountyBase);
            for (var die = 0; die < victim.Type.Integer(BountyDice); die++) gold += _game.RandomInt(1, Math.Max(victim.Type.Integer(BountySides), 1));
            if (gold > 0) SetPlayerState(killer.Owner, Convert<playerstate>(Gold), killer.Owner.States[Gold] + gold);
        }
        var level = Math.Max(GetUnitLevel(victim), 1);
        // A structure razed gave the hero score nothing (probe units-scores: 1120 before and after a barracks), so no experience
        var xp = IsBuildingNow(victim) ? 0 : IsHero(victim.Type.Id) ? Listed("GrantHeroXP", level, 0) : Listed("GrantNormalXP", level, 0);
        if (xp <= 0) return;
        var range = Data.Constant("HeroExpRange", 1200);
        var heroes = _units.Values.Where(h => h.Owner == killer.Owner && IsHero(h.Type.Id) && !h.Dead && !h.Removed && Distance2(h.X - victim.X, h.Y - victim.Y) <= range * range).ToList();
        foreach (var hero in heroes)
        {
            AddHeroXP(hero, xp / heroes.Count, true);
            killer.Owner.HeroExperience += xp / heroes.Count;
        }
    }
}
