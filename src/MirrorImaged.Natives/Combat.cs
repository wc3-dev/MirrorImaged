using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal unit? AttackTarget;          // the unit it is fighting, by order or by acquiring it
    internal double NextAttack, SwingEnds; // when it may start another attack, and when the one under way lets it move again
    internal int Weapon;                  // the weapon (0 or 1) the target is attacked with
    internal float PatrolX, PatrolY;      // where a patrol turns back to
    internal float OrderX, OrderY;        // the place of the current order
    internal widget? OrderTarget;         // its target
}

// Movement and combat for units with orders and for idle ones that acquire enemies: a 1/32 s step in which a unit walks toward the
// place or target of its order at its speed, attacks in range with its weapon (a swing, the damage at the damage point, then the cooldown)
// and acquires the nearest enemy it may attack in its acquisition range when it has no order, holds position or attack-moves. Close:
// units walk straight lines (no pathfinding or collision, only the unwalkable cells and the world's edge stop them), the damage
// reaches a target at its damage point without a missile's flight, and the facts' orders and events of waves 3 are the ones kept
// (docs/decisions.md, D352).
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
        if (Seconds < u.NextAttack || u.SwingEnds > Seconds) return;
        u.Facing.Go(MathF.Atan2(t.Y - u.Y, t.X - u.X), Seconds, 0);
        var point = BlzGetUnitWeaponRealField(u, Convert<unitweaponrealfield>(Code("udp1")), weapon);
        var cooldown = Math.Max(BlzGetUnitAttackCooldown(u, weapon) / AttackRate(u), 0.1f);
        (u.NextAttack, u.SwingEnds) = (Seconds + cooldown, Seconds + point);
        FireUnit(new(t, Other: u), UnitAttacked, PlayerUnitAttacked);
        _game.At(_game.Step + Math.Max(1, Game.Steps(point)), () => Hit(u, t, weapon));
    }

    // The attack lands: base damage and the dice, as the weapon's type and sound give them, through the damage events of a real attack
    void Hit(unit u, unit t, int weapon)
    {
        if (u.Dead || t.Dead || t.Removed || u.Removed) return;
        var damage = (float)BlzGetUnitBaseDamage(u, weapon);
        for (var die = 0; die < BlzGetUnitDiceNumber(u, weapon); die++) damage += _game.RandomInt(1, Math.Max(BlzGetUnitDiceSides(u, weapon), 1));
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
