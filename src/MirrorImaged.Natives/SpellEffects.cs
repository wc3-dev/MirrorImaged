using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal bool Illusion;               // an image of Mirror Image
    internal float Deals = 1, Takes = 1;  // what its hits take and what a hit takes from it, times these (an illusion's)
    internal Action<bool>? Channel;       // ends the spell it channels: true when its time is over, false when an order broke it
}

// More abilities (wave 4): the spells of the melee heroes that damage, heal, slow, root, shield or stun, and the auras of the heroes that change armour,
// speed, attack rate, regeneration, life steal and damage for the allies near. Each reads its numbers from the ability's own data (DataA..DataE by
// level, its duration, area and range), as the game's tooltips name them. Close: the numbers' meanings are from the tooltips of the game's data and
// the abilities of channels, summons, passives of an attack's chance (bash, critical strike, evasion), and the transformations are not here
// (docs/decisions.md, D380). Flame Strike, Silence, Wind Walk, Immolation, Mirror Image, Earthquake and Starfall are as probe abilities-spells
// measured them (D408 to D415).
public sealed partial class GameNatives
{
    static readonly int InvulnerableBuff = Code("BHds"), RootBuff = Code("BEer"), SleepBuff = Code("BUsl"), FrostArmorBuff = Code("BUfa"), FrostNovaBuff = Code("BUfn"),
        AvatarBuff = Code("BHav"), DevotionBuff = Code("BHad"), EnduranceBuff = Code("BOae"), UnholyBuff = Code("BUau"), VampiricBuff = Code("BUav"), TrueshotBuff = Code("BEar");

    const float AuraPulseSeconds = 1, AuraBuffSeconds = 1.6f;

    // The data of the ability by letter, at its level
    float Datum(ability a, char letter) => TagValue(a.Type, "Data" + letter + a.Level);

    void Spell(unit caster, unit target, float amount, int damageType = 14) =>
        UnitDamageTarget(caster, target, amount, false, false, Convert<attacktype>(0), Convert<damagetype>(damageType), Convert<weapontype>(0));

    // The foes in an area: those whose edge is, so a foe's collision size counts (probe abilities-spells: a footman 225 from a Flame Strike of 200
    // and 185 from an Immolation of 160 burns, a Priest 225 from a Silence of 200 isn't silenced, a footman 1030 from a Starfall of 1000 is hit)
    IEnumerable<unit> Foes(unit caster, float x, float y, float area) =>
        _units.Values.Where(u => Hostile(caster, u) && MathF.Sqrt(Distance2(u.X - x, u.Y - y)) <= area + u.Type.Real(Collision)).ToArray();

    // A channelled spell (probe abilities-spells: Starfall, Earthquake): its wave comes after the first seconds and then every so many, until the
    // ability's time is over, the caster dies or it is given another order. The end-cast event fires then, and the finish one only at the time's end
    void Channelled(unit caster, ability a, double first, double every, Action wave)
    {
        var (cast, start, lasts) = (new Happening(caster, Skill: a.Type.Id), _game.Step, Math.Max(Lasts(a, caster), 0.001));
        Action<bool> end = finished =>
        {
            if (finished) FireUnit(cast, UnitSpellFinish, PlayerSpellFinish);
            FireUnit(cast, UnitSpellEndCast, PlayerSpellEndCast);
        };
        caster.Channel = end;
        for (var n = 0; first + n * every <= lasts; n++)
            _game.At(start + Game.Steps(first + n * every), () => { if (caster.Channel == end && !caster.Dead) wave(); });
        _game.At(start + Game.Steps(lasts), () => { if (caster.Channel == end) { EndChannel(caster, true); Finish(caster); } });
    }

    void EndChannel(unit u, bool finished)
    {
        if (u.Channel is not { } end) return;
        u.Channel = null;
        end(finished);
    }

    const float ImmolationSteps = 128;

    // Immolation (probe abilities-spells): on until it is ordered off or the mana is out. DataA to the ground foes in the area the moment it comes
    // on (0.01 s in) and every duration (a second) after, a paused foe only the first; DataB mana a second, taken evenly (here 128 times a second),
    // and it goes off when two of those steps can't be paid (the game's went off with 0.10 mana left)
    void Immolate(unit caster, ability a)
    {
        var on = Apply(caster, caster, ImmolationBuff, 0, true, _ => { });
        var (start, gap, bit, area) = (_game.Step, Field(a, Code("adur")) is > 0 and var seconds ? seconds : 1, Datum(a, 'B') / ImmolationSteps, Field(a, CastArea));
        bool Burning() => !caster.Dead && caster.Buffs.Contains(on);
        void Burn(int n) => _game.At(start + Game.Steps(0.01 + n * gap), () =>
        {
            if (!Burning()) return;
            foreach (var near in Foes(caster, caster.X, caster.Y, area <= 0 ? 160 : area).Where(u => !Flies(u) && (n == 0 || !u.Paused))) Spell(caster, near, Datum(a, 'A'), 8);
            Burn(n + 1);
        });
        void Drain(int n) => _game.At(start + Game.Steps(n / ImmolationSteps), () =>
        {
            if (!Burning()) return;
            SetMana(caster, caster.Mana - bit);
            if (caster.Mana < 2 * bit) caster.Buffs.Remove(on);
            else Drain(n + 1);
        });
        Burn(0);
        if (bit > 0) Drain(1);
    }

    // Immolation's off order (probe abilities-spells): true, and it burns no more; false for a unit that isn't burning, which is not measured
    bool? ImmolationOff(unit u, int order) =>
        OrderNames.GetValueOrDefault(order) == "unimmolation" ? u.Buffs.RemoveAll(b => b.Id == ImmolationBuff) > 0 : null;

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
            case var id when id == Code("AHwe") || id == Code("AEfn") || id == Code("AOsw") || id == Code("AEst") || id == Code("AEsv"):
                // A unit or two called near the caster or the target place: Water Elemental, Force of Nature, Serpent Ward, Scout, Vengeance
                Summon(caster, a, target is unit tu ? tu.X : x != 0 || y != 0 ? x : caster.X, target is unit tv ? tv.Y : x != 0 || y != 0 ? y : caster.Y, Math.Max((int)Datum(a, 'A'), 1));
                return true;
            case var id when id == Code("AOsf"):
                Summon(caster, a, caster.X, caster.Y, Math.Max((int)Datum(a, 'B'), 1));
                return true;
            case var id when id == Code("AHbz"):
                // Blizzard: a wave of damage each second for DataA waves, to the ground units round the place
                var (bx, by, bradius, waves, wave) = (target is unit bt ? bt.X : x, target is unit bu ? bu.Y : y, area <= 0 ? 200 : area, Math.Max((int)Datum(a, 'A'), 1), Datum(a, 'B'));
                for (var n = 1; n <= waves; n++)
                    _game.At(_game.Step + Game.Steps(n), () => { if (!caster.Dead) foreach (var near in Foes(caster, bx, by, bradius).Where(u => !Flies(u))) Spell(caster, near, wave); });
                return true;
            case var id when id == Code("AUan"):
                // Animate Dead: up to DataA corpses near the place, not heroes, stand up for the caster's owner for the ability's time
                var (ax, ay) = target is unit at ? (at.X, at.Y) : x != 0 || y != 0 ? (x, y) : (caster.X, caster.Y);
                foreach (var corpse in _units.Values.Where(u => u.Dead && !u.Removed && !IsHero(u.Type.Id) && !IsBuildingNow(u) && Distance2(u.X - ax, u.Y - ay) <= (area <= 0 ? 900 : area) * (area <= 0 ? 900 : area)).OrderBy(u => u.Id).Take(Math.Max((int)Datum(a, 'A'), 1)).ToArray())
                {
                    var (cx, cy, type) = (corpse.X, corpse.Y, corpse.Type.Id);
                    RemoveUnit(corpse);
                    if (Create(caster.Owner, type, cx, cy, 270, type) is not { } risen) continue;
                    UnitApplyTimedLife(risen, Code("BTLF"), Field(a, Code("adur")));
                    FireUnit(new(caster, Other: risen, Skill: a.Type.Id), UnitSummon, PlayerUnitSummon);
                }
                return true;
            case var id when (id == Code("AOhx") && target is unit hexed && !hexed.Dead) || id == Code("AEme"):
                // Hex turns another unit into the ability's creature, Metamorphosis the caster into its form, for the ability's time, and they come back as they were
                var changed = id == Code("AOhx") ? (unit)target! : caster;
                if (SummonTypes(a).FirstOrDefault() is { } formId && formId != 0 && Data.Object(ObjectKind.Unit, formId) is { } form && !changed.Dead)
                {
                    var (original, mana) = (changed.Type, changed.Mana);
                    var back = _game.Step + Game.Steps(Math.Max(Lasts(a, changed), 0.1));
                    Transform(changed, form);
                    _game.At(back, () => { if (!changed.Dead && !changed.Removed && changed.Type == form) { Transform(changed, original); SetMana(changed, Math.Min(Math.Max(mana, changed.Mana), changed.MaxMana)); } });
                }
                return true;
            case var id when id == Code("AHav"):
                // Avatar: more armour and life for its time
                var (gain, lifeGain) = (Datum(a, 'A'), Datum(a, 'B'));
                Apply(caster, caster, AvatarBuff, Lasts(a, caster), true, b => b.ArmorBonus = gain);
                caster.MaxLife += lifeGain;
                SetLife(caster, caster.Life + lifeGain);
                _game.At(_game.Step + Game.Steps(Math.Max(Lasts(a, caster), 0.1)), () => { caster.MaxLife -= lifeGain; if (caster.Life > caster.MaxLife) SetLife(caster, caster.MaxLife); });
                return true;
            case var id when id == Code("AHre"):
                // Resurrection: up to DataA corpses of the caster's friends near it, not heroes, get up again at full life as they were
                foreach (var corpse in _units.Values.Where(u => u.Dead && !u.Removed && !IsHero(u.Type.Id) && !IsBuildingNow(u) && (u.Owner == caster.Owner || IsPlayerAlly(caster.Owner, u.Owner))
                        && Distance2(u.X - caster.X, u.Y - caster.Y) <= (area <= 0 ? 900 : area) * (area <= 0 ? 900 : area)).OrderBy(u => u.Id).Take(Math.Max((int)Datum(a, 'A'), 1)).ToArray())
                {
                    var (cx, cy, type, owner) = (corpse.X, corpse.Y, corpse.Type.Id, corpse.Owner);
                    RemoveUnit(corpse);
                    Create(owner, type, cx, cy, 270, type);
                }
                return true;
            case var id when id == Code("AUin"):
                // Inferno: an infernal lands on the place, hurting the units round it for DataA, and stays for the ability's time
                var (ix, iy) = target is unit it ? (it.X, it.Y) : x != 0 || y != 0 ? (x, y) : (caster.X, caster.Y);
                foreach (var near in Foes(caster, ix, iy, area <= 0 ? 250 : area)) Spell(caster, near, Datum(a, 'A'));
                Summon(caster, a, ix, iy, 1);
                return true;
            case var id when id == Code("AHfs"):
                // Flame Strike (probe abilities-spells): from its casting time on, which the cast waits before this (Spells: Effected), DataA every
                // DataB seconds for the hero duration, then DataC every DataD seconds until the duration is over, of fire, to the ground foes at
                // the place; a paused one takes the first hit alone
                var (fx, fy) = target is unit ft ? (ft.X, ft.Y) : x != 0 || y != 0 ? (x, y) : (caster.X, caster.Y);
                var (begins, full, whole) = (0d, Field(a, Code("ahdu")), Field(a, Code("adur")));
                void Burn(double at, float amount, bool first) => _game.At(_game.Step + Game.Steps(at), () =>
                {
                    if (!caster.Dead) foreach (var near in Foes(caster, fx, fy, area <= 0 ? 200 : area).Where(u => !Flies(u) && (first || !u.Paused))) Spell(caster, near, amount, 8);
                });
                var (strong, weak) = (Datum(a, 'B') is > 0 and var b ? b : 1, Datum(a, 'D') is > 0 and var d ? d : 1);
                for (var n = 0; n * strong <= full; n++) Burn(begins + n * strong, Datum(a, 'A'), n == 0);
                for (var n = 1; full + n * weak <= whole; n++) Burn(begins + full + n * weak, Datum(a, 'C'), false);
                return true;
            case var id when id == Code("AEsf"):
                // Starfall (probe abilities-spells): channelled; DataA to the foes round the caster every DataB seconds. In the game each foe's star
                // lands at a time of its own, 4 to 7 in 10 s
                var stars = Datum(a, 'B') is > 0 and var apart ? apart : 1.5f;
                Channelled(caster, a, stars, stars, () => { foreach (var near in Foes(caster, caster.X, caster.Y, area <= 0 ? 800 : area)) Spell(caster, near, Datum(a, 'A'), 26); });
                return true;
            case var id when id == Code("ANsi"):
                // Silence (probe abilities-spells): the foes at the place that have mana can't cast for the ability's time, a hero for its own. A
                // footman got no buff; that mana is what decides it is this model's reading
                var (sx, sy) = target is unit st ? (st.X, st.Y) : x != 0 || y != 0 ? (x, y) : (caster.X, caster.Y);
                foreach (var near in Foes(caster, sx, sy, area <= 0 ? 300 : area).Where(u => u.MaxMana > 0)) Apply(near, caster, SilenceBuff, Lasts(a, near), false, b => b.Silence = true);
                return true;
            case var id when id == Code("AOwk"):
                // Wind Walk (probe abilities-spells): DataB faster for its time and unseen by foes from DataA seconds in; the unit attacks nothing by
                // itself, and the buff ends as its next attack lands, which takes DataC more
                var wind = Apply(caster, caster, WindWalkBuff, Lasts(a, caster), true, b => (b.SpeedFactor, b.Sneaks, b.Backstab) = (1 + Datum(a, 'B'), true, Datum(a, 'C')));
                _game.At(_game.Step + Game.Steps(Datum(a, 'A')), () => wind.Invisible = true);
                return true;
            case var id when id == BeetlesBase && target is unit body && IsCorpse(body):
                Beetles(caster, a, body);
                return true;
            case var id when id == Code("AUls"):
                LocustSwarm(caster, a);
                return true;
            case var id when id == Code("AHbn") && target is unit banished && !banished.Dead:
                // Banish (probe abilities-banish): the unit is ethereal and slower by DataA for the ability's time, a hero's own for a hero,
                // and takes a hit of 0 as the buff comes and as its time ends
                void Nothing() => UnitDamageTarget(caster, banished, 0, false, false, Convert<attacktype>(0), Convert<damagetype>(0), Convert<weapontype>(0));
                Nothing();
                Apply(banished, caster, BanishBuff, Lasts(a, banished), false, b => (b.SpeedFactor, b.Ethereal, b.Ended) = (1 - Datum(a, 'A'), true, Nothing));
                return true;
            case var id when id == Code("AEim"):
                Immolate(caster, a);
                return true;
            case var id when id == Code("AOmi"):
                // Mirror Image (probe abilities-spells): DataD seconds in, DataA images of the caster stand by it for the ability's time, illusions
                // of its type with its life and mana, whose hits take DataB of their damage and that lose DataC times a hit. In the game the caster
                // and its image go to two places near, which of them where changing from run to run; here the caster stays. A summon event fires
                // for each image and one more with no summoned unit (probe abilities-cast-timing: 2, 3 and 4 events at levels 1 to 3), the first
                // DataD in and the last 0.11 to 0.18 s later, which of them is the one without changing; here the images' come first and it last
                _game.At(_game.Step + Game.Steps(Datum(a, 'D')), () =>
                {
                    for (var n = 0; n < Math.Max((int)Datum(a, 'A'), 1) && !caster.Dead && !caster.Removed; n++)
                    {
                        if (Create(caster.Owner, caster.Type.Id, caster.X + 60 * (n + 1), caster.Y, 270, caster.Type.Id) is not { } image) continue;
                        (image.Illusion, image.Deals, image.Takes) = (true, Datum(a, 'B'), Datum(a, 'C') is > 0 and var times ? times : 1);
                        (image.MaxLife, image.MaxMana) = (caster.MaxLife, caster.MaxMana);
                        SetLife(image, caster.Life);
                        SetMana(image, caster.Mana);
                        UnitApplyTimedLife(image, Code("BTLF"), Field(a, Code("adur")));
                        FireUnit(new(caster, Other: image, Skill: a.Type.Id), UnitSummon, PlayerUnitSummon);
                    }
                });
                _game.At(_game.Step + Game.Steps(Datum(a, 'D') + 0.18), () => { if (!caster.Dead && !caster.Removed) FireUnit(new(caster, Skill: a.Type.Id), UnitSummon, PlayerUnitSummon); });
                return true;
            case var id when id == Code("AOeq"):
                // Earthquake (probe abilities-spells): channelled; from DataA seconds in and every second, the buildings at the place take DataB
                // and the other foes there are slowed by DataC for two seconds
                var (qx, qy) = target is unit qt ? (qt.X, qt.Y) : x != 0 || y != 0 ? (x, y) : (caster.X, caster.Y);
                Channelled(caster, a, Datum(a, 'A'), 1, () =>
                {
                    foreach (var near in Foes(caster, qx, qy, area <= 0 ? 300 : area))
                        if (IsBuildingNow(near)) Spell(caster, near, Datum(a, 'B'), 26);
                        else Apply(near, caster, QuakeBuff, 2, false, slow => slow.SpeedFactor = 1 - Datum(a, 'C'));
                });
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
        else if (id == Code("AHab")) { if (!receiver.Dead) SetMana(receiver, Math.Min(receiver.Mana + Datum(a, 'A') * AuraPulseSeconds, receiver.MaxMana)); }
        else if (id == Code("AEar")) Apply(receiver, giver, TrueshotBuff, AuraBuffSeconds, true, b => b.DamageBonus = Datum(a, 'A'));
        else return false;
        return true;
    }

    static readonly int WindWalkBuff = Code("BOwk"), SilenceBuff = Code("BNsi"), BanishBuff = Code("BHbn"), ImmolationBuff = Code("BEim"), QuakeBuff = Code("BOeq");

    bool _aurasScheduled;

    // The pulse is a timer of its own, a second apart, for as long as some unit has an aura (the unit engine rests when no unit has anything to do)
    void EnsureAuras()
    {
        if (_aurasScheduled) return;
        _aurasScheduled = true;
        _game.At(_game.Step + Game.Steps(AuraPulseSeconds), AuraPulse);
    }

    void AuraPulse()
    {
        _aurasScheduled = false;
        var any = false;
        foreach (var giver in _units.Values.Where(u => !u.Dead && !u.Removed && !u.Constructing && u.Abilities.Count > 0).ToArray())
            foreach (var a in giver.Abilities.Where(a => !a.Disabled && a.Level > 0 && AuraBases.Contains(a.Type.BaseId)).ToArray())
            {
                any = true;
                var area = Field(a, CastArea);
                foreach (var receiver in Friends(giver, giver.X, giver.Y, area <= 0 ? 900 : area)) AuraEffect(a, giver, receiver);
            }
        if (any || _units.Values.Any(u => !u.Removed && u.Abilities.Any(a => AuraBases.Contains(a.Type.BaseId)))) EnsureAuras();
    }

    static readonly HashSet<int> AuraBases = [Code("AHab"), Code("AHad"), Code("AOae"), Code("AUau"), Code("AUav"), Code("AEar")];

    // What the buffs on a unit add to its attacks
    float LifeStealOf(unit u) => u.Buffs.Count == 0 ? 0 : u.Buffs.Max(b => b.LifeSteal);
    float DamageBonusOf(unit u) => u.Buffs.Count == 0 ? 0 : u.Buffs.Max(b => b.DamageBonus);
    bool IsInvulnerableByBuff(unit u) => u.Buffs.Any(b => b.Invulnerable);
    bool IsRooted(unit u) => u.Buffs.Any(b => b.Root);
}
