using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal double? TimedEnd, TimedLeft; // when its timed life ends, while it runs; what is left of it, while paused
    internal int TimedBuff;               // the buff its timed life was given with
}

// Timed life, as the probes units-more and units-more-2 measure it (facts, "Units"): a death like KillUnit's,
// the killer nil, the body decaying as any other.
public sealed partial class GameNatives
{
    /// <summary>
    /// Makes the living unit summoned (its classification, as UnitAddType does) and kills it the duration later, from
    /// the step it is applied; applied again, the sooner end holds (3 then 1 s and 1 then 2 s both kill 1 s later); a
    /// duration of 0 or less makes it summoned and never kills; a dead unit gets nothing. Any buff id, a hero too (probes
    /// units-more and units-more-2; game-facts: 2.0 s). With a buff other than BTLF (the Water Elemental's BHwe) the body
    /// has no decay event and is taken away at the decay's time, as a removed body goes (probe units-more); with BTLF or
    /// none it decays as any other's.
    /// </summary>
    /// <remarks>Close: only BHwe was measured beside BTLF and none; that every other buff's body goes as BHwe's did is the model's reading.</remarks>
    [Native(Level.Close, Reason = "only BHwe was measured beside BTLF and none; that every other buff's body goes as BHwe's did is the model's reading")]
    public void UnitApplyTimedLife(unit? whichUnit, int buffId, float duration)
    {
        if (whichUnit is not { Dead: false } u) return;
        Classify(u, Convert<unittype>(10), true); // UNIT_TYPE_SUMMONED
        if (duration <= 0) return;
        u.TimedBuff = buffId;
        if (u.TimedLeft is { } left) u.TimedLeft = Math.Min(left, duration);
        else if (u.TimedEnd is not { } end || Seconds + duration < end) EndTimedLife(u, Seconds + duration);
    }

    static readonly int GenericTimedLife = Code("BTLF");

    // Whether the body of a unit that had timed life goes at its decay time with no decay event (probe units-more: BHwe's did; BTLF's and one with
    // no buff decayed)
    static bool Vanishes(unit u) => u.TimedBuff != 0 && u.TimedBuff != GenericTimedLife;

    // A step after its time, as the game's timers due then run first (probe units-more: a timer due at 1.40 ran before
    // the deaths there), as a body goes (Rot)
    void EndTimedLife(unit u, double end)
    {
        u.TimedEnd = end;
        _game.At(Game.Steps(end) + 1, () =>
        {
            if (u.TimedEnd == end && _game.Handle(u.Key) is not null) Kill(u);
        });
    }

    /// <summary>Stops the timed life's clock, or runs it on from where it stopped; with none it does nothing (probes units-more and units-more-2).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probes units-more and units-more-2 (11-08)).</remarks>
    [Native(Level.Exact, Fact = MoreProbe)]
    public void UnitPauseTimedLife(unit? whichUnit, bool flag)
    {
        if (whichUnit is null) return;
        if (flag && whichUnit.TimedEnd is { } end)
            (whichUnit.TimedLeft, whichUnit.TimedEnd) = (end - Seconds, null);
        else if (!flag && whichUnit.TimedLeft is { } left)
        {
            whichUnit.TimedLeft = null;
            EndTimedLife(whichUnit, Seconds + left);
        }
    }

    /// <summary>Ends the timed life now: the unit dies inside the call, paused or not; with none it does nothing (probes units-more and units-more-2).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probes units-more and units-more-2 (11-08)).</remarks>
    [Native(Level.Exact, Fact = MoreProbe)]
    public void BlzUnitCancelTimedLife(unit? whichUnit)
    {
        if (whichUnit is { TimedEnd: not null } or { TimedLeft: not null }) Kill(whichUnit);
    }
}
