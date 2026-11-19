using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal unit? AttackTarget;          // the unit it is fighting, by order or by acquiring it
    internal double NextAttack, SwingEnds; // when it may start another attack, and when the one under way lets it move again
    internal long SwingDue;               // the step its next swing is looked at, when one is waited for
    internal int Weapon;                  // the weapon (0 or 1) the target is attacked with
    internal float PatrolX, PatrolY;      // where a patrol turns back to
    internal float OrderX, OrderY;        // the place of the current order
    internal widget? OrderTarget;         // its target
}

// Movement and combat for units with orders and for idle ones that acquire enemies: a 1/32 s step in which a unit walks toward the
// place or target of its order at its speed, attacks in range with its weapon (a swing, the damage at the damage point, then the cooldown)
// and acquires the nearest enemy it may attack in its acquisition range when it has no order, holds position or attack-moves. Close:
// units walk straight lines (no pathfinding or collision, only the unwalkable cells and the world's edge stop them), a missile flies
// level, and the facts' orders and events of waves 3 are the ones kept (docs/decisions.md, D352). Attacks come a
// cooldown apart to the clock's step, and a hero's agility quickens them (probe units-attack-timing, D452 to D454). A missile is a thing in
// flight that follows its target, and an artillery shell one that lands where its target stood (probe units-missile-homing, D620 to D622).
public sealed partial class GameNatives
{
    const double TickSeconds = 1.0 / 32;
    const int UnitAttacked = 62, PlayerUnitAttacked = 18, UnitAcquiredTarget = 60, UnitTargetInRange = 61, OrderMove = 851986, OrderAIMove = 851987, OrderAttack = 851983, OrderAttackOnce = 851985, OrderPatrol = 851990, OrderHold = 851993, ScanEvery = 4;

    bool _engine;
    long _ticks;

    // The engine runs while some living unit could walk or fight; each unit made or ordered wakes it
    void Wake()
    {
        if (_engine) return;
        _engine = true;
        _game.At(_game.Step + Math.Max(1, Game.Steps(TickSeconds)), Tick);
    }

    void Tick()
    {
        _engine = false;
        _ticks++;
        var busy = false;
        foreach (var u in _units.Values.ToArray())
            if (!u.Dead && !u.Removed) busy |= Act(u);
        if (busy) Wake();
    }

    // The weapon the unit attacks the target with, the first enabled one whose targets allow it; -1 for none
    int WeaponFor(unit u, unit t)
    {
        var on = u.Type.Integer(WeaponsOn);
        for (var weapon = 0; weapon < 2; weapon++)
        {
            if ((on & (1 << weapon)) == 0) continue;
            var allowed = BlzGetUnitWeaponIntegerField(u, Convert<unitweaponintegerfield>(Code("ua1g")), weapon);
            var kind = Flies(t) ? 4 : IsBuildingNow(t) ? 8 : 2;
            if ((allowed & kind) != 0) return weapon;
        }
        return -1;
    }

    bool Hostile(unit u, unit t) =>
        t != u && !t.Dead && !t.Hidden && !t.Removed && !t.Locust && t.Owner != u.Owner && GetPlayerId(u.Owner) != GetPlayerNeutralPassive()
        && GetPlayerId(t.Owner) != GetPlayerNeutralPassive() && IsPlayerEnemy(u.Owner, t.Owner) && Find(t, Invulnerable) is null && !IsInvulnerableByBuff(t);

    // A unit without its Attack and an ethereal one attack nothing, and only a weapon of attack type magic reaches an ethereal unit (probe abilities-banish)
    bool Armed(unit u, unit t) => WeaponFor(u, t) is >= 0 and var weapon && !u.Disarmed && !IsEthereal(u)
        && (!IsEthereal(t) || BlzGetUnitWeaponIntegerField(u, Convert<unitweaponintegerfield>(Code("ua1t")), weapon) == MagicAttack);

    float Reach(unit u, int weapon) => BlzGetUnitWeaponRealField(u, Convert<unitweaponrealfield>(Code("ua1r")), weapon);

    float Distance(unit a, unit b) => MathF.Sqrt(Distance2(a.X - b.X, a.Y - b.Y));

    // The nearest enemy within the range the unit may attack, or null
    unit? Nearest(unit u, float range)
    {
        unit? best = null;
        var bestAt = float.MaxValue;
        foreach (var t in _units.Values)
        {
            var near = Distance2(u.X - t.X, u.Y - t.Y);
            if (near > range * range || near >= bestAt * bestAt || !Hostile(u, t) || !Armed(u, t)) continue;
            (best, bestAt) = (t, MathF.Sqrt(near));
        }
        return best;
    }

    // One step of a unit; true while it could still do something next step
    bool Act(unit u)
    {
        if (u.Constructing || u.Swarming) return false;
        if (u.Channel is not null) return true;
        if (u.Casting is not null && u.Order != 0 && !u.Paused) return Cast(u);
        var armed = u.Type.Integer(WeaponsOn) != 0;
        var mobile = GetUnitMoveSpeed(u) > 0;
        if (u.Paused || u.Hidden || IsStunned(u) || Asleep(u)) return armed || mobile;
        if (u.AttackTarget is { } stale && (stale.Dead || stale.Removed || stale.Hidden || !Armed(u, stale))) u.AttackTarget = null;
        var scan = _ticks % ScanEvery == 0;
        var order = u.Order;
        if (order == OrderHarvest) return Harvest(u);
        if (order == OrderRepair) return Repairing(u);
        if (order == OrderReturn) return ReturnLoad(u);
        if (order != 0 && !OrderNames.ContainsKey(order) && Data.Object(ObjectKind.Unit, order) is not null)
        {
            // A build order: walk to the site, then make the building and stay with it until it is done
            if (u.Building is { Constructing: true }) return true;
            if (Distance2(u.OrderX - u.X, u.OrderY - u.Y) > 100 * 100) return Walk(u, u.OrderX, u.OrderY, follow: true);
            return Place(u, order, u.OrderX, u.OrderY) || Finish(u);
        }
        if (order == 0 || order == OrderHold)
        {
            if (order == 0 && GoBackToPost(u)) return true;
            if (u.AttackTarget is null && armed && scan && !Sneaks(u) && Nearest(u, order == OrderHold ? HoldRange(u) : GetUnitAcquireRange(u)) is { } found) Acquired(u, found);
            if (u.AttackTarget is { } t) Engage(u, t, chase: order == 0);
            return armed;
        }
        if (order is OrderAttack or Smart or OrderAttackOnce or OrderMove or OrderAIMove or OrderPatrol && u.Queue.Count >= 0)
        {
            var (x, y, target) = (u.OrderX, u.OrderY, u.OrderTarget as unit);
            if (order is OrderAttack or Smart or OrderAttackOnce)
            {
                if (target is not null && Hostile(u, target) && Armed(u, target)) { (u.AttackTarget, u.Weapon) = (target, Math.Max(WeaponFor(u, target), 0)); Engage(u, target, chase: true); return true; }
                if (target is not null && order == Smart && GoToTransport(u, target) && u.Transport is null) return Walk(u, target.X, target.Y, follow: true);
                if (target is not null && order == Smart && u.Transport is not null) return false;
                if (target is not null && order == Smart) return Walk(u, target.X, target.Y, follow: true);
                if (target is not null && order is OrderAttack or OrderAttackOnce) return Finish(u);
                // An attack-move: fight what it comes upon, else walk on
                if (u.AttackTarget is null && armed && scan && Nearest(u, GetUnitAcquireRange(u)) is { } near) Acquired(u, near);
                if (u.AttackTarget is { } fighting) { Engage(u, fighting, chase: true); return true; }
                return order == Smart ? Walk(u, x, y, follow: false) : Walk(u, x, y, follow: false);
            }
            return Walk(u, x, y, follow: false, patrol: order == OrderPatrol);
        }
        return armed;
    }

    // The unit takes the enemy as its target: the acquired-target event fires with it (the order stays what it was)
    void Acquired(unit u, unit target)
    {
        (u.AttackTarget, u.Weapon) = (target, Math.Max(WeaponFor(u, target), 0));
        FireUnit(new(u, Other: target), UnitAcquiredTarget, UnitAcquiredTarget);
    }

    // A unit hit with no order of its own and nothing to fight hits back at what hit it, however far it stood (facts, 11-14)
    void HitBack(unit victim, unit attacker)
    {
        if (victim.Order != 0 || victim.AttackTarget is not null || victim.Dead || victim.Paused || !Hostile(victim, attacker) || !Armed(victim, attacker)) return;
        Acquired(victim, attacker);
    }

    float HoldRange(unit u) => Math.Max(Reach(u, 0), Reach(u, 1));

    // Walks toward the place at the unit's speed; true while it goes on, and the order ends on arrival (a patrol turns back)
    bool Walk(unit u, float x, float y, bool follow, bool patrol = false)
    {
        if (IsRooted(u)) return true;
        var speed = GetUnitMoveSpeed(u);
        if (speed <= 0) return Finish(u);
        var (dx, dy) = (x - u.X, y - u.Y);
        var left = MathF.Sqrt(dx * dx + dy * dy);
        var step = speed * (float)TickSeconds;
        if (left > 0) u.Facing.Go(MathF.Atan2(dy, dx), Seconds, 0);
        if (u.SwingEnds > Seconds) return true;
        if (left <= step)
        {
            if (!Blocked(u, x, y)) Move(u, x, y);
            if (patrol)
            {
                (u.OrderX, u.OrderY, u.PatrolX, u.PatrolY) = (u.PatrolX, u.PatrolY, u.OrderX, u.OrderY);
                return true;
            }
            return follow ? true : Finish(u);
        }
        return Go(u, x, y, step);
    }

    // Whether the ground the unit would step onto stops it: walkers and riders can't enter unwalkable cells or leave the world
    bool Blocked(unit u, float x, float y) => Blocked(u, BlzGetUnitIntegerField(u, Convert<unitintegerfield>(MoveType)), x, y);

    bool Blocked(unit u, int type, float x, float y)
    {
        if (Data.Terrain is { } terrain)
        {
            var (minX, minY, maxX, maxY) = terrain.Bounds;
            if (x < minX || x > maxX || y < minY || y > maxY) return true;
        }
        if (type is 1 or 4 && Data.Map?.Files.PathingMap is not null && IsTerrainPathable(x, y, Convert<pathingtype>(1))) return true;
        return type is not 2 && BlockedByBuilding(u, x, y);
    }

    // The order done: the unit is idle, or takes the next queued order
    bool Finish(unit u)
    {
        u.Order = 0;
        u.AttackTarget = null;
        if (u.Queue.Count == 0) return u.Type.Integer(WeaponsOn) != 0 || GetUnitMoveSpeed(u) > 0;
        var (order, x, y, target) = u.Queue[0];
        u.Queue.RemoveAt(0);
        Give(u, order, x, y, target, target is not null ? 2 : x != 0 || y != 0 ? 1 : 0);
        return true;
    }

    // The unit at its target: it walks into range, then swings when its cooldown is over, the damage landing at the damage point
    void Engage(unit u, unit t, bool chase)
    {
        var weapon = Math.Max(WeaponFor(u, t), 0);
        var range = Reach(u, weapon) + t.Type.Real(Collision);
        var at = Distance(u, t);
        if (at > range)
        {
            if (!chase) { u.AttackTarget = null; return; }
            Walk(u, t.X, t.Y, follow: true);
            return;
        }
        var ready = Math.Max(u.NextAttack, u.SwingEnds);
        if (Seconds < ready - HalfStep) { SwingWhenReady(u, ready); return; }
        u.Facing.Go(MathF.Atan2(t.Y - u.Y, t.X - u.X), Seconds, 0);
        // The cooldown and the damage point are both divided by the attack rate (a Paladin of agility 50 hit 0.22 s after its swing began, of 0 0.43)
        var rate = AttackRate(u);
        var point = BlzGetUnitWeaponRealField(u, Convert<unitweaponrealfield>(Code("udp1")), weapon) / rate;
        var cooldown = Math.Max(BlzGetUnitAttackCooldown(u, weapon) / rate, 0.1f);
        // A swing that was waited for starts from when it was due, so that the steps' rounding doesn't add up
        var from = Seconds - ready < 2 * HalfStep ? ready : Seconds;
        (u.NextAttack, u.SwingEnds) = (from + cooldown, Seconds + point);
        FireUnit(new(t, Other: u), UnitAttacked, PlayerUnitAttacked);
        _game.At(_game.Step + Math.Max(1, Game.Steps(point)), () => Launch(u, t, weapon));
    }

    const double HalfStep = 0.5 / Game.StepsPerSecond;

    // The unit acts again the step its cooldown is over, not at the engine's next step: the game's hits came exactly a cooldown apart
    void SwingWhenReady(unit u, double ready)
    {
        var due = Math.Max(_game.Step + 1, Game.Steps(ready));
        if (u.SwingDue == due) return;
        u.SwingDue = due;
        _game.At(due, () => { if (u.SwingDue == due && !u.Dead && !u.Removed) Act(u); });
    }

    static readonly int LaunchAhead = Code("ulpy");

    /// <summary>A missile in flight: where it is, what it follows or where it lands, and what its landing does.</summary>
    internal sealed class Missile
    {
        public int Id, Player;             // the run's own count of missiles, and the player of the unit it left
        public unit? Target;               // the unit it follows; none for a shell, which flies to a place
        public float Speed, ToX, ToY;      // where it flies to: the target's place when last looked at, or the shell's place
        public double X, Y, Vx, Vy;        // where it was at the step Seen, and how far it flies a second
        public double FromX, FromY;        // where it left
        public long Seen, Left;
        public (float X, float Y)? Was;    // the target's place a look ago
        public required Action<Missile> Land;

        public (double X, double Y) Place(long step) => (X + Vx * (step - Seen) / Game.StepsPerSecond, Y + Vy * (step - Seen) / Game.StepsPerSecond);
    }

    readonly List<Missile> _missiles = [];
    int _missilesMade;

    /// <summary>The missiles in flight, the first to leave first.</summary>
    internal IReadOnlyList<Missile> Missiles => _missiles;

    /// <summary>The flights that landed, each as <see cref="Flight"/> gives it; kept only once a recorder set a list here.</summary>
    internal List<double[]>? Flights;

    /// <summary>A missile's flight so far: when it left and now in seconds, its player, and where it left and is.</summary>
    internal double[] Flight(Missile m)
    {
        var (x, y) = m.Place(_game.Step);
        return [(double)m.Left / Game.StepsPerSecond, (double)_game.Step / Game.StepsPerSecond, m.Player, Math.Round(m.FromX, 1), Math.Round(m.FromY, 1), Math.Round(x, 1), Math.Round(y, 1)];
    }

    // A missile leaves the unit for the target (probes units-attack-timing and units-missile-homing): it starts as far ahead of the unit as its
    // missiles do (launchY: an archer's 62, an Archmage's 0), flies at the speed, and lands when it reaches the target, which it follows, or
    // the place the target stood at as it left when it doesn't follow. A short way lands at once: an archer's 88 did and an Archmage's 100
    // flew, and the middle is taken here
    void Fire(unit from, unit to, float speed, bool follows, Action<Missile> land)
    {
        var (dx, dy) = (to.X - from.X, to.Y - from.Y);
        var (between, ahead) = (Distance(from, to), from.Type.Real(LaunchAhead));
        var missile = new Missile { Player = GetPlayerId(from.Owner), Target = follows ? to : null, Speed = speed, ToX = to.X, ToY = to.Y, X = to.X, Y = to.Y, Seen = _game.Step, Left = _game.Step, Land = land };
        if (between - ahead < 94 || speed <= 0) { land(missile); return; }
        (missile.X, missile.Y) = (from.X + dx / between * ahead, from.Y + dy / between * ahead);
        (missile.FromX, missile.FromY, missile.Id) = (missile.X, missile.Y, ++_missilesMade);
        _missiles.Add(missile);
        Fly(missile);
    }

    // The missile's look at its target, every step of the engine: it turns to where the target is now, and lands in this step's time when
    // it gets there in it, the target counted as walking on as it did since the last look (the game's archer closed on a footman walking
    // away at the difference of their speeds). A target moved faster than the missile flies was put somewhere, not walking, and counts as standing
    void Fly(Missile m)
    {
        var (vx, vy) = (0.0, 0.0);
        if (m.Target is { Removed: false } t)
        {
            if (m.Was is var (wasX, wasY)) (vx, vy) = ((t.X - wasX) / TickSeconds, (t.Y - wasY) / TickSeconds);
            if (vx * vx + vy * vy >= m.Speed * m.Speed) (vx, vy) = (0, 0);
            (m.ToX, m.ToY, m.Was) = (t.X, t.Y, (t.X, t.Y));
        }
        var (dx, dy) = (m.ToX - m.X, m.ToY - m.Y);
        // The time t at which the way to the target, walking, is what the missile flies in t: (d + v t)^2 = (speed t)^2
        var (a, b, c) = ((double)m.Speed * m.Speed - vx * vx - vy * vy, dx * vx + dy * vy, dx * dx + dy * dy);
        var reach = (b + Math.Sqrt(b * b + a * c)) / a;
        m.Seen = _game.Step;
        if (reach <= TickSeconds)
        {
            (m.Vx, m.Vy) = reach > 0 ? ((dx + vx * reach) / reach, (dy + vy * reach) / reach) : (0, 0);
            void Lands()
            {
                (m.X, m.Y, m.Vx, m.Vy, m.Seen) = m.Target is { Removed: false } hit ? (hit.X, hit.Y, 0, 0, _game.Step) : (m.ToX, m.ToY, 0, 0, _game.Step);
                _missiles.Remove(m);
                Flights?.Add(Flight(m));
                m.Land(m);
            }
            if (Game.Steps(reach) > 0) _game.At(_game.Step + Game.Steps(reach), Lands);
            else Lands();
            return;
        }
        var left = Math.Sqrt(c);
        (m.Vx, m.Vy) = (dx / left * m.Speed, dy / left * m.Speed);
        _game.At(_game.Step + Game.Steps(TickSeconds), () =>
        {
            (m.X, m.Y) = m.Place(_game.Step);
            Fly(m);
        });
    }

    static readonly int FullArea = Code("ua1f"), MediumArea = Code("ua1h"), SmallArea = Code("ua1q"), MediumFactor = Code("uhd1"), SmallFactor = Code("uqd1");

    // The share of a shell's damage a unit takes that far from where it landed (probe units-missile-homing: a mortar team's areas of 25, 150 and
    // 250 with factors 0.40 and 0.10 gave a footman 221 away a tenth)
    float Share(unit u, int weapon, float away)
    {
        float Field(int code) => BlzGetUnitWeaponRealField(u, Convert<unitweaponrealfield>(code), weapon);
        return away <= Field(FullArea) ? 1 : away <= Field(MediumArea) ? Field(MediumFactor) : away <= Field(SmallArea) ? Field(SmallFactor) : 0;
    }

    // The damage point: a missile leaves and lands on its target, a shell leaves and lands where the target stood, any other attack lands now
    // shortcut: a shell hits its target alone, by how far it got from where the shell landed; upgrade when area damage is modelled
    void Launch(unit u, unit t, int weapon)
    {
        var speed = BlzGetUnitWeaponRealField(u, Convert<unitweaponrealfield>(Code("ua1z")), weapon);
        switch (u.Type.Text(Code($"ua{weapon + 1}w")))
        {
            case "missile" or "mbounce" or "mline" or "msplash": Fire(u, t, speed, true, _ => Hit(u, t, weapon)); break;
            case "artillery" or "aline" or "alsplash": Fire(u, t, speed, false, m => Hit(u, t, weapon, Share(u, weapon, MathF.Sqrt(Distance2(t.X - m.ToX, t.Y - m.ToY))))); break;
            default: Hit(u, t, weapon); break;
        }
    }

    // The attack lands: base damage and the dice, as the weapon's type and sound give them, through the damage events of a real attack
    void Hit(unit u, unit t, int weapon, float share = 1)
    {
        if (u.Dead || t.Dead || t.Removed || u.Removed || share <= 0) return;
        var damage = (float)BlzGetUnitBaseDamage(u, weapon);
        for (var die = 0; die < BlzGetUnitDiceNumber(u, weapon); die++) damage += _game.RandomInt(1, Math.Max(BlzGetUnitDiceSides(u, weapon), 1));
        damage *= share;
        var attackType = BlzGetUnitWeaponIntegerField(u, Convert<unitweaponintegerfield>(Code("ua1t")), weapon);
        var sound = BlzGetUnitWeaponIntegerField(u, Convert<unitweaponintegerfield>(Code("ucs1")), weapon);
        var kind = u.Type.Text(Code($"ua{weapon + 1}w")) ?? "normal";
        var ranged = kind is "missile" or "mbounce" or "mline" or "msplash" or "artillery" or "aline" or "alsplash";
        if (ranged && DamageBonusOf(u) is > 0 and var bonus) damage *= 1 + bonus;
        if (u.Buffs.Find(b => b.Sneaks) is { } wind)
        {
            // Wind Walk ends as the attack lands: a hit of 0, then the attack with the buff's damage more (probe abilities-spells)
            u.Buffs.Remove(wind);
            UnitDamageTarget(u, t, 0, false, false, Convert<attacktype>(0), Convert<damagetype>(0), Convert<weapontype>(0));
            damage += wind.Backstab;
        }
        if (Evades(t, u)) return;
        var stun = false;
        PassivesOfAttack(u, t, ranged, ref damage, ref stun);
        Strike(new Hit(u, t, damage, true, ranged, Convert<attacktype>(attackType), Convert<damagetype>(NormalDamage), Convert<weapontype>(sound)) { Real = true }, t);
        if (stun && !t.Dead) Apply(t, u, StunBuff, StunSeconds(u, t), false, b => b.Stun = true);
        if (!ranged && !u.Dead && t.Abilities.Find(x => x.Type.BaseId == Code("AEah")) is { } thorns) Strike(new Hit(t, u, damage * Datum(thorns, 'A'), false, false, Convert<attacktype>(0), Convert<damagetype>(14), Convert<weapontype>(0)) { Real = true }, u);
        if (!t.Dead) HitBack(t, u);
    }

    /// <summary>The unit that attacks in an attacked event, the event's own unit being the one attacked.</summary>
    [Native(Level.Close)]
    public unit? GetAttacker() => Response(UnitAttacked, PlayerUnitAttacked)?.Other;

    /// <summary>The unit an acquired-target or target-in-range event is about; nil outside one.</summary>
    [Native(Level.Close)]
    public unit? GetEventTargetUnit() => Response(UnitAcquiredTarget, UnitTargetInRange)?.Other;
}
