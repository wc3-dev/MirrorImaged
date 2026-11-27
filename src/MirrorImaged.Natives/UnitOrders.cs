using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

// The order events the unit natives give without orders, which are wave 3's (probes units, units-more, units-more-2 and
// units-bones; game-facts, "Orders and events"): SetUnitOwner's stop, PauseUnit's stunned order, and the undefend a
// unit's Defend gives it as it dies, goes, is paused, unpaused or given away.
public sealed partial class GameNatives
{
    const int Stop = 851972, Stunned = 851973, Undefending = 852056;
    const double Frame = 0.01; // when PauseUnit(false)'s undefend comes back: 0.01 s later, twice (probes units-more, units-more-2)
    const double OrderBackSeconds = 0.016; // when the order the unit had comes back (game-facts, 11-03: the move, 0.016 s after PauseUnit(false))

    // Whether a pause, a stun or BlzPauseUnitEx holds the unit: it does nothing, and its current order reads stunned
    bool Held(unit u) => u.Paused || u.Pauses > 0 || IsStunned(u);

    // Something begins to hold the unit: what it had been given is kept, so the order it had can be given back when it is let go
    void Holding(unit u)
    {
        if (u.HeldGiven < 0) u.HeldGiven = u.Given;
        u.OrderBack = null;
    }

    // Nothing holds the unit any more: the order it had, unless it was given another since, or is idle or casting, comes back as an order event 0.016
    // s later and is carried out (game-facts, 10-26 and 11-03: a move, an attack and a patrol came back after PauseUnit(false); at a Storm Bolt's end,
    // the move; an idle unit got none, 11-05). The stun's and BlzPauseUnitEx's delay is the pause's, which is this model's reading
    void LetGo(unit u)
    {
        if (Held(u) || u.HeldGiven < 0) return;
        var (given, order, kind) = (u.HeldGiven, u.Order, u.OrderKind);
        u.HeldGiven = -1;
        if (order == 0 || u.Casting is not null || u.Given != given || u.Dead) return;
        Action back = null!;
        back = () =>
        {
            if (u.OrderBack != back) return;
            u.OrderBack = null;
            if (u.Dead || u.Removed || Held(u) || u.Given != given || u.Order != order) return;
            var (unitKind, playerKind) = kind switch { 0 => (UnitIssuedOrder, PlayerUnitIssuedOrder), 1 => (UnitIssuedPointOrder, PlayerUnitIssuedPointOrder), _ => (UnitIssuedTargetOrder, PlayerUnitIssuedTargetOrder) };
            FireUnit(new(u, Order: order, PointX: u.OrderX, PointY: u.OrderY, Target: u.OrderTarget), unitKind, playerKind);
        };
        u.OrderBack = back;
        _game.At(_game.Step + Game.Steps(OrderBackSeconds), back);
    }

    // A stun begins on a unit nothing held: it gets the stunned order (851973) as a target order (game-facts, 11-03: Storm Bolt's, at the hit). Its
    // target is not modelled
    void StunBegins(unit u)
    {
        var free = u.HeldGiven < 0;
        Holding(u);
        if (free) FireUnit(new(u, Order: Stunned), UnitIssuedTargetOrder, PlayerUnitIssuedTargetOrder);
    }

    static readonly int Defend = Code("Adef");

    // How many Defends the unit has: its abilities made from Adef, a map's copies too (game-facts)
    int Defends(unit u) => Abilities(u).Count(a => a?.BaseId == Defend);

    IEnumerable<GameObject?> Abilities(unit u) => u.Abilities.Select(a => a.Type);

    // undefend's order events, on the owner's registrations and the unit's
    void Undefend(unit u, int times)
    {
        for (var i = 0; i < times; i++) FireUnit(new(u, Order: Undefending), UnitIssuedOrder, PlayerUnitIssuedOrder);
    }

    /// <summary>
    /// A flag, not a count: true twice and false once leaves it unpaused (facts, Units; probe units). A living unit
    /// paused gets the stunned order (851973) as a point order, then undefend (852056) for each Defend; unpaused, a
    /// frame later (0.01 s) undefend for each Defend, or the stop it missed if it changed hands while paused; paused
    /// again before that, that order comes at once, with no stunned order. A dead unit's pause does nothing (probes
    /// units-more and units-more-2). While paused its current order reads stunned; let go, the order it had (a move, an
    /// attack, a patrol) comes back as an order event 0.016 s later and is carried out, unless it was idle or given
    /// another (game-facts, 10-26, 11-03, 11-05).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probes units-more and units-more-2 (11-08); game-facts.md, Orders and events: PauseUnit (10-26, 11-03, 11-05)).</remarks>
    [Native(Level.Exact, Fact = MoreProbe + "; game-facts.md, Orders and events: PauseUnit (10-26, 11-03, 11-05)")]
    public void PauseUnit(unit? whichUnit, bool flag)
    {
        if (whichUnit is not { Dead: false } u || u.Paused == flag) return;
        Asleep(u);
        Regenerate(u);
        u.Paused = flag;
        HoldBuffs(u);
        if (flag) Holding(u);
        else LetGo(u);
        if (flag)
        {
            if (u.Unpausing is { } back)
            {
                u.Unpausing = null;
                back();
                return;
            }
            FireUnit(new(u, Order: Stunned), UnitIssuedPointOrder, PlayerUnitIssuedPointOrder);
            // not when the stunned order's event unpaused or killed it (not measured)
            if (u is { Paused: true, Dead: false }) Undefend(u, Defends(u));
            return;
        }
        var given = u.GivenWhilePaused;
        u.GivenWhilePaused = false;
        Action giveBack = () =>
        {
            if (given) FireUnit(new(u, Order: Stop), UnitIssuedOrder, PlayerUnitIssuedOrder);
            else Undefend(u, Defends(u));
        };
        u.Unpausing = giveBack;
        _game.At(Game.Steps(Seconds + Frame), () =>
        {
            if (u.Unpausing != giveBack) return;
            u.Unpausing = null;
            giveBack();
        });
    }
}
