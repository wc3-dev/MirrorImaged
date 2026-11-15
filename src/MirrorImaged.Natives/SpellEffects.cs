using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

// More abilities (wave 4): the spells of the melee heroes that damage, heal, slow, root, shield or stun, and the auras of the heroes that change armour,
// speed, attack rate, regeneration, life steal and damage for the allies near. Each reads its numbers from the ability's own data (DataA..DataE by
// level, its duration, area and range), as the game's tooltips name them. Close: the numbers' meanings are from the tooltips of the game's data and
// the abilities of channels, summons, passives of an attack's chance (bash, critical strike, evasion), and the transformations are not here
// (docs/decisions.md, D380).
public sealed partial class GameNatives
{
    static readonly int InvulnerableBuff = Code("BHds"), RootBuff = Code("BEer"), SleepBuff = Code("BUsl"), FrostArmorBuff = Code("BUfa"), FrostNovaBuff = Code("BUfn"),
        DevotionBuff = Code("BHad"), EnduranceBuff = Code("BOae"), UnholyBuff = Code("BUau"), VampiricBuff = Code("BUav"), TrueshotBuff = Code("BEar");

    const float AuraPulseSeconds = 1, AuraBuffSeconds = 1.6f;

    // The data of the ability by letter, at its level
    float Datum(ability a, char letter) => TagValue(a.Type, "Data" + letter + a.Level);

    void Spell(unit caster, unit target, float amount, int damageType = 14) =>
        UnitDamageTarget(caster, target, amount, false, false, Convert<attacktype>(0), Convert<damagetype>(damageType), Convert<weapontype>(0));

    IEnumerable<unit> Foes(unit caster, float x, float y, float area) =>
        _units.Values.Where(u => Hostile(caster, u) && Distance2(u.X - x, u.Y - y) <= area * area).ToArray();

    IEnumerable<unit> Friends(unit caster, float x, float y, float area) =>
        _units.Values.Where(u => !u.Dead && !u.Removed && !u.Hidden && !u.Locust && (u.Owner == caster.Owner || IsPlayerAlly(caster.Owner, u.Owner)) && Distance2(u.X - x, u.Y - y) <= area * area).ToArray();

    // What a spell of the ability does, if this file models it: true when it did
    bool MoreEffect(unit caster, ability a, float x, float y, widget? target)
    {
        var area = Field(a, CastArea);
        switch (a.Type.BaseId)
        {
            case var id when id == Code("AUdc") && target is unit coil && !coil.Dead:
                // Death Coil: hurts an enemy, heals a friend that is undead
                if (Hostile(caster, coil)) Spell(caster, coil, Datum(a, 'A'));
                else SetLife(coil, Math.Min(coil.Life + Datum(a, 'A'), coil.MaxLife));
                return true;
            case var id when id == Code("AOcl") && target is unit first:
                Chain(caster, a, first, hostile: true, Datum(a, 'A'), (int)Datum(a, 'B'), Datum(a, 'C'), area <= 0 ? 500 : area, 10);
                return true;
            case var id when id == Code("AOhw") && target is unit healed:
                Chain(caster, a, healed, hostile: false, Datum(a, 'A'), (int)Datum(a, 'B'), Datum(a, 'C'), area <= 0 ? 500 : area, 0);
                return true;
            case var id when id == Code("AUfn") && target is unit nova && !nova.Dead:
                // Frost Nova: damage to the target and a smaller one round it, which slows them all
                var radius = area <= 0 ? 200 : area;
                foreach (var near in Foes(caster, nova.X, nova.Y, radius))
                {
                    Spell(caster, near, near == nova ? Datum(a, 'A') : Datum(a, 'B'), 9);
                    if (!near.Dead) Apply(near, caster, FrostNovaBuff, Lasts(a, near), false, b => (b.SpeedFactor, b.AttackSpeedBonus) = (0.5f, -0.25f));
                }
                return true;
            case var id when id == Code("AOws"):
                // War Stomp: damage and a stun round the caster
                foreach (var near in Foes(caster, caster.X, caster.Y, area).Where(u => !Flies(u)))
                {
                    Spell(caster, near, Datum(a, 'A'));
                    if (!near.Dead) Apply(near, caster, StunBuff, Lasts(a, near), false, b => b.Stun = true);
                }
                return true;
            case var id when id == Code("AEfk"):
                foreach (var near in Foes(caster, caster.X, caster.Y, area)) Spell(caster, near, Datum(a, 'A'));
                return true;
            case var id when id == Code("AEmb") && target is unit burned && !burned.Dead:
                // Mana Burn: takes mana away, and hurts for as much (times the damage per mana point)
                var taken = Math.Min(burned.Mana, Datum(a, 'A'));
                SetMana(burned, burned.Mana - taken);
                Spell(caster, burned, taken * Math.Max(Datum(a, 'C'), 1));
                return true;
            case var id when (id == Code("AUcs") || id == Code("AOsh")) && (target is not null || x != 0 || y != 0):
                // A line of damage from the caster toward the place: Carrion Swarm, Shockwave
                var (tx, ty) = target is unit t ? (t.X, t.Y) : (x, y);
                var (dx, dy) = (tx - caster.X, ty - caster.Y);
                var length = MathF.Max(MathF.Sqrt(dx * dx + dy * dy), 1);
                var (reach, half) = (Datum(a, id == Code("AOsh") ? 'B' : 'C') is > 0 and var distance ? distance : 800, MathF.Max(area, 100));
                foreach (var near in Foes(caster, caster.X, caster.Y, reach + half))
                {
                    var along = ((near.X - caster.X) * dx + (near.Y - caster.Y) * dy) / length;
                    var across = MathF.Abs((near.X - caster.X) * dy - (near.Y - caster.Y) * dx) / length;
                    if (along >= 0 && along <= reach && across <= half) Spell(caster, near, Datum(a, 'A'));
                }
                return true;
            case var id when id == Code("AEer") && target is unit rooted && !rooted.Dead:
                // Entangling Roots: holds a unit where it stands and hurts it each second
                Apply(rooted, caster, RootBuff, Lasts(a, rooted), false, b => b.Root = true);
                var seconds = (int)Lasts(a, rooted);
                for (var second = 1; second <= seconds; second++)
                    _game.At(_game.Step + Game.Steps(second), () => { if (!rooted.Dead && rooted.Buffs.Any(b => b.Id == RootBuff)) Spell(caster, rooted, Datum(a, 'A')); });
                return true;
            case var id when id == Code("AUsl") && target is unit slept && !slept.Dead:
                Apply(slept, caster, SleepBuff, Lasts(a, slept), false, b => b.Stun = true);
                return true;
            case var id when id == Code("AHds"):
                Apply(caster, caster, InvulnerableBuff, Lasts(a, caster), true, b => b.Invulnerable = true);
                return true;
            case var id when id == Code("AOvd"):
                foreach (var friend in Friends(caster, caster.X, caster.Y, area)) Apply(friend, caster, InvulnerableBuff, Lasts(a, friend), true, b => b.Invulnerable = true);
                return true;
            case var id when id == Code("AUfu") && target is unit armored && !armored.Dead:
                Apply(armored, caster, FrostArmorBuff, Lasts(a, armored), true, b => b.ArmorBonus = Datum(a, 'B'));
                return true;
        }
        return false;
    }

    // A bolt that jumps: the damage or the healing goes to the target, then to the nearest not yet hit within reach, less each time
    void Chain(unit caster, ability a, unit first, bool hostile, float amount, int count, float reduction, float reach, int damageType)
    {
        var hit = new List<unit>();
        var current = first;
        for (var jump = 0; jump < Math.Max(count, 1) && current is not null; jump++)
        {
            hit.Add(current);
            if (hostile) Spell(caster, current, amount, damageType);
            else SetLife(current, Math.Min(current.Life + amount, current.MaxLife));
            amount *= 1 - reduction;
            var from = current;
            current = (hostile ? Foes(caster, from.X, from.Y, reach) : Friends(caster, from.X, from.Y, reach).Where(u => u.Life < u.MaxLife))
                .Where(u => !hit.Contains(u)).OrderBy(u => Distance2(u.X - from.X, u.Y - from.Y)).FirstOrDefault()!;
        }
    }

    // ---- Auras: once a second, each unit with an aura gives the buff to its allies within its area, for a little longer than the pulse

    bool AuraEffect(ability a, unit giver, unit receiver)
    {
        var id = a.Type.BaseId;
        if (id == Code("AHad")) Apply(receiver, giver, DevotionBuff, AuraBuffSeconds, true, b => b.ArmorBonus = Datum(a, 'A'));
        else if (id == Code("AOae")) Apply(receiver, giver, EnduranceBuff, AuraBuffSeconds, true, b => (b.SpeedFactor, b.AttackSpeedBonus) = (1 + Datum(a, 'A'), Datum(a, 'B')));
        else if (id == Code("AUau"))
        {
            Apply(receiver, giver, UnholyBuff, AuraBuffSeconds, true, b => b.SpeedFactor = 1 + Datum(a, 'A'));
            if (!receiver.Dead) SetLife(receiver, Math.Min(receiver.Life + Datum(a, 'B') * AuraPulseSeconds, receiver.MaxLife));
        }
        else if (id == Code("AUav")) Apply(receiver, giver, VampiricBuff, AuraBuffSeconds, true, b => b.LifeSteal = Datum(a, 'A'));
        else if (id == Code("AEar")) Apply(receiver, giver, TrueshotBuff, AuraBuffSeconds, true, b => b.DamageBonus = Datum(a, 'A'));
        else return false;
        return true;
    }

    long _lastAuraPulse = -1;

    void AuraPulse()
    {
        var second = _ticks / 32;
        if (second == _lastAuraPulse) return;
        _lastAuraPulse = second;
        foreach (var giver in _units.Values.Where(u => !u.Dead && !u.Removed && !u.Constructing && u.Abilities.Count > 0).ToArray())
            foreach (var a in giver.Abilities.Where(a => !a.Disabled && a.Level > 0 && AuraBases.Contains(a.Type.BaseId)).ToArray())
            {
                var area = Field(a, CastArea);
                foreach (var receiver in Friends(giver, giver.X, giver.Y, area <= 0 ? 900 : area)) AuraEffect(a, giver, receiver);
            }
    }

    static readonly HashSet<int> AuraBases = [Code("AHad"), Code("AOae"), Code("AUau"), Code("AUav"), Code("AEar")];

    // What the buffs on a unit add to its attacks
    float LifeStealOf(unit u) => u.Buffs.Count == 0 ? 0 : u.Buffs.Max(b => b.LifeSteal);
    float DamageBonusOf(unit u) => u.Buffs.Count == 0 ? 0 : u.Buffs.Max(b => b.DamageBonus);
    bool IsInvulnerableByBuff(unit u) => u.Buffs.Any(b => b.Invulnerable);
    bool IsRooted(unit u) => u.Buffs.Any(b => b.Root);
}
