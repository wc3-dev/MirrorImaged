using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal unit? Transport;                      // the transport the unit is loaded in
    internal readonly List<unit> Cargo = [];       // what a transport holds
}

// Transports (wave 3): a unit with a cargo hold (Cargo Hold, whose DataA is the room) takes friendly units loaded by its load order or by their own order to go to it, holds
// them hidden where it is (they move with it) and puts them down on the unload orders; a transport that dies takes its cargo with it. Close: a unit that is
// ordered to be loaded is taken at once when it is near enough and walks to the transport otherwise; the room a unit takes is one (the game counts by size), and
// the loaded and unloaded events are the loaded one only, as common.j has it (decisions D386).
public sealed partial class GameNatives
{
    const int UnitLoaded = 88, PlayerUnitLoaded = 51, LoadReach = 700, OrderLoad = 852046, OrderUnload = 852047, OrderUnloadAll = 852048;
    static readonly HashSet<int> CargoBases = [Code("Achd"), Code("Sch2"), Code("Sch3"), Code("Sch4"), Code("Sch5"), Code("Sch1")];

    /// <summary>Whether the unit is in a transport.</summary>
    /// <remarks>Close: Reads the model's cargo, in which a unit takes one room and is loaded at once when near enough, where the game counts by size (D386).</remarks>
    [Native(Level.Close)] public bool IsUnitLoaded(unit? whichUnit) => whichUnit?.Transport is not null;

    /// <summary>Whether the unit is in that transport.</summary>
    /// <remarks>Close: Reads the model's cargo, in which a unit takes one room and is loaded at once when near enough, where the game counts by size (D386).</remarks>
    [Native(Level.Close)] public bool IsUnitInTransport(unit? whichUnit, unit? whichTransport) => whichUnit is not null && whichTransport is not null && whichUnit.Transport == whichTransport;

    /// <summary>The unit a loaded event is about.</summary>
    /// <remarks>Close: Only the loaded event exists, as common.j has it (D386).</remarks>
    [Native(Level.Close)] public unit? GetLoadedUnit() => Response(UnitLoaded, PlayerUnitLoaded)?.Other;

    /// <summary>The transport in a loaded event.</summary>
    /// <remarks>Close: Only the loaded event exists, as common.j has it (D386).</remarks>
    [Native(Level.Close)] public unit? GetTransportUnit() => Response(UnitLoaded, PlayerUnitLoaded)?.Unit;

    int Room(unit transport) =>
        transport.Abilities.Where(a => !a.Disabled && CargoBases.Contains(a.Type.BaseId)).Select(a => (int)TagValue(a.Type, "DataA" + a.Level)).DefaultIfEmpty(0).Max() - transport.Cargo.Count;

    bool Loadable(unit transport, unit cargo) =>
        transport != cargo && !transport.Dead && !cargo.Dead && !cargo.Removed && cargo.Transport is null && cargo.Owner == transport.Owner && !IsBuildingNow(cargo) && !Flies(cargo)
        && Room(transport) > 0;

    // The unit goes in: hidden, at the transport, with its order over
    bool Load(unit transport, unit cargo)
    {
        if (!Loadable(transport, cargo)) return false;
        cargo.Order = 0;
        cargo.Queue.Clear();
        cargo.Transport = transport;
        transport.Cargo.Add(cargo);
        (cargo.X, cargo.Y) = (transport.X, transport.Y);
        cargo.Hidden = true;
        FireUnit(new(transport, Other: cargo), UnitLoaded, PlayerUnitLoaded);
        return true;
    }

    // The unit comes out at the place, round it if there are many
    void Unload(unit transport, unit cargo, float x, float y)
    {
        if (cargo.Transport != transport) return;
        transport.Cargo.Remove(cargo);
        cargo.Transport = null;
        cargo.Hidden = false;
        var angle = (cargo.Id % 12) * MathF.PI / 6;
        Move(cargo, Held(x + 90 * MathF.Cos(angle), true), Held(y + 90 * MathF.Sin(angle), false));
    }

    // The transport orders, taken before the order itself: load a unit, unload one or all
    bool CargoOrder(unit u, int order, float x, float y, widget? target, out bool answer)
    {
        answer = false;
        if (order == OrderLoad && target is unit cargo)
        {
            answer = Distance2(cargo.X - u.X, cargo.Y - u.Y) <= LoadReach * LoadReach && Load(u, cargo);
            return true;
        }
        if (order is OrderUnload or OrderUnloadAll && (u.Cargo.Count > 0 || Room(u) > 0))
        {
            var (px, py) = x != 0 || y != 0 ? (x, y) : (u.X, u.Y);
            foreach (var held in (order == OrderUnload && target is unit one ? u.Cargo.Where(c => c == one) : u.Cargo).ToArray()) Unload(u, held, px, py);
            answer = true;
            return true;
        }
        return false;
    }

    // A unit sent to its friendly transport: in reach, it is loaded
    bool GoToTransport(unit u, unit transport)
    {
        if (!Loadable(transport, u)) return false;
        if (Distance2(u.X - transport.X, u.Y - transport.Y) <= (u.Type.Real(Collision) + transport.Type.Real(Collision) + 150) * (u.Type.Real(Collision) + transport.Type.Real(Collision) + 150)) Load(transport, u);
        return true;
    }

    // The cargo goes with its transport, and dies with it
    void CarryCargo(unit transport)
    {
        foreach (var c in transport.Cargo) (c.X, c.Y) = (transport.X, transport.Y);
    }

    void SinkCargo(unit transport)
    {
        foreach (var c in transport.Cargo.ToArray())
        {
            transport.Cargo.Remove(c);
            c.Transport = null;
            c.Hidden = false;
            KillUnit(c);
        }
    }
}
