using MirrorImaged.Data;
using War3Net.Build.Info;

namespace MirrorImaged;

/// <summary>Where two players' runs of one map first differ.</summary>
/// <param name="Step">The step the games first differ in.</param>
/// <param name="Part">The part of the world that differs: the time, a player, a unit, <c>random numbers</c>, <c>handle ids</c>, a trigger, a frame or <c>sync events</c>.</param>
/// <param name="What">What in that part differs, such as <c>PLAYER_STATE_RESOURCE_GOLD</c>.</param>
/// <param name="Players">The slots whose games were compared; the lists below have a place for each, in this order.</param>
/// <param name="Values">The value in each player's game; null where that game has no such thing (a frame destroyed there).</param>
/// <param name="Stacks">
/// The Lua stack, as <c>debug.traceback</c> writes it, of the call that made the value differ in each player's game; null for a game whose
/// script made no such call in the step.
/// </param>
/// <param name="Hashes">Each game's state hash at the step.</param>
/// <param name="LogLine">The first line of the log that differs.</param>
public sealed record DesyncReport(long Step, string Part, string What, IReadOnlyList<int> Players, IReadOnlyList<string?> Values,
    IReadOnlyList<string?> Stacks, IReadOnlyList<string> Hashes, string? LogLine);

/// <summary>
/// The desync check: the map runs once for each person's slot, as that player's own game on that player's own machine, and the runs are compared
/// through the time asked. A map that changes the game from what only one machine knows (<c>GetLocalPlayer</c>, where its camera looks, the size
/// of its window) makes its runs differ, and the check says in which step, what differs and which call of the script made it.
/// </summary>
public static class Desync
{
    /// <summary>The slots of the map that a person plays.</summary>
    public static IReadOnlyList<int> UserSlots(string path) =>
        [.. MapArchive.Read(path).Files.Info?.Players.Where(p => p.Controller == PlayerController.User).Select(p => p.Id) ?? []];

    /// <summary>
    /// Runs the map for each person's slot and compares the runs every <paramref name="every"/> (a second, unless given); null when they stay the
    /// same to the end. When they differ, the games are run twice more to find the step and the calls, so a report costs three runs.
    /// </summary>
    public static DesyncReport? Check(string path, TimeSpan time, int seed = 0, TimeSpan? every = null)
    {
        var slots = UserSlots(path);
        if (slots.Count < 2) return null;
        List<Simulation> Load(Func<int, Action<Simulation>>? called = null) => [.. slots.Select((slot, machine) => Simulation.Load(path, new SimulationOptions
        {
            Seed = seed, Users = [slot, .. slots.Where(other => other != slot)], Machine = machine, Called = called?.Invoke(machine),
        }))];
        var games = Load();
        try
        {
            var slice = every ?? TimeSpan.FromSeconds(1);
            for (var spent = TimeSpan.Zero; spent < time; spent += slice)
            {
                var from = games[0].Step;
                foreach (var game in games) Run(game, () => game.Advance(slice));
                if (Differ(games)) return Report(slots, games, Load, from);
            }
            return null;
        }
        finally
        {
            foreach (var game in games) game.Dispose();
        }
    }

    static void Run(Simulation game, Action advance)
    {
        try { advance(); }
        catch (Lua.LuaException) when (game.Stopped is not null) { }
    }

    static bool Differ(List<Simulation> games) => games.Select(game => game.StateHash()).Distinct().Count() > 1;

    // The games differ now and didn't at the step given, or only as loaded: they are run again to that step and on a step at a time to the one
    // they differ in, then once more through that step, watching what differs for the calls that change it
    static DesyncReport Report(IReadOnlyList<int> slots, List<Simulation> late, Func<Func<int, Action<Simulation>>?, List<Simulation>> load, long from)
    {
        var again = load(null);
        try
        {
            foreach (var game in again) Run(game, () => game.AdvanceSteps(from));
            for (var left = late.Max(game => game.Step) - from; left > 0 && !Differ(again); left--)
                foreach (var game in again) Run(game, () => game.AdvanceSteps(1));
            // A map whose runs aren't the same each time may not differ again: then the report is of the games as they were compared
            var found = Differ(again);
            var games = found ? again : late;
            var step = games.Max(game => game.Step);
            var hashes = games.Select(game => game.StateHash()).ToList();
            var fields = games.Select(game => game.Natives.Fields().ToList()).ToList();
            var values = fields.Select(list => list.ToDictionary(field => (field.Part, field.What), field => field.Value)).ToList();
            var (part, what) = fields.SelectMany(list => list.Select(field => (field.Part, field.What))).Distinct()
                .FirstOrDefault(key => values.Select(of => of.GetValueOrDefault(key)).Distinct().Count() > 1, ("state", "hash"));
            var logs = games.Select(game => game.Log).ToList();
            var line = Enumerable.Range(0, logs.Max(log => log.Count)).Select(i => logs.Select(log => i < log.Count ? log[i] : null).ToList())
                .FirstOrDefault(row => row.Distinct().Count() > 1)?.FirstOrDefault(entry => entry is not null);
            return new(step, part, what, slots, part == "state" ? [.. hashes] : [.. values.Select(of => of.GetValueOrDefault((part, what)))],
                found ? Stacks(slots, load, step, part, what) : [.. slots.Select(_ => (string?)null)], hashes, line);
        }
        finally
        {
            foreach (var game in again) game.Dispose();
        }
    }

    // Each game's changes of the value through the step, in order; the first that isn't the same in every game is the call that made them differ
    static List<string?> Stacks(IReadOnlyList<int> slots, Func<Func<int, Action<Simulation>>?, List<Simulation>> load, long step, string part, string what)
    {
        var watches = slots.Select(_ => new Watch(step, part, what)).ToList();
        var games = load(machine => watches[machine].Called);
        try
        {
            if (step > 0)
                foreach (var (game, watch) in games.Zip(watches))
                {
                    Run(game, () => game.AdvanceSteps(step - 1));
                    watch.Seen(game);
                    Run(game, () => game.AdvanceSteps(1));
                }
            var changes = watches.Select(watch => watch.Changes).ToList();
            var at = Enumerable.Range(0, changes.Max(of => of.Count)).FirstOrDefault(i => changes.Select(of => i < of.Count ? of[i] : default).Distinct().Count() > 1, -1);
            return [.. changes.Select(of => at >= 0 && at < of.Count ? of[at].Stack : null)];
        }
        finally
        {
            foreach (var game in games) game.Dispose();
        }
    }

    // Reads one value of a game after each call its script makes in one step, and keeps the Lua stack of each call that changed it
    sealed class Watch(long step, string part, string what)
    {
        string? _last;
        bool _seen;

        public List<(string? Value, string Stack)> Changes { get; } = [];

        string? Read(Simulation game) => game.Natives.Fields().FirstOrDefault(field => field.Part == part && field.What == what).Value;

        // The value before the step's calls
        public void Seen(Simulation game) => (_last, _seen) = (Read(game), true);

        // Runs once before the script, which is the value before step 0's calls, and then after each call
        public void Called(Simulation game)
        {
            if (game.Step != step) return;
            var now = Read(game);
            if (_seen && now != _last) Changes.Add((now, game.Stack()));
            (_last, _seen) = (now, true);
        }
    }
}
