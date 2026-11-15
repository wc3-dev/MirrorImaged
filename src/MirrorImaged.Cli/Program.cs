using System.Globalization;
using MirrorImaged;
using MirrorImaged.Lua;
using MirrorImaged.Natives;

if (args is ["coverage"])
{
    Console.Write(Coverage.Of(typeof(GameNatives)));
    return 0;
}

if (args is ["check", var mapPath])
{
    try
    {
        var script = MirrorImaged.Data.MapArchive.Read(mapPath).Script;
        var levels = typeof(GameNatives).GetMethods()
            .Select(method => (method.Name, Level: method.GetCustomAttributes(typeof(NativeAttribute), false).OfType<NativeAttribute>().FirstOrDefault()?.Level))
            .Where(native => native.Level is not null).ToDictionary(native => native.Name, native => native.Level!.Value);
        var text = script is null ? "" : System.Text.Encoding.UTF8.GetString(script);
        var used = System.Text.RegularExpressions.Regex.Matches(text, @"[A-Za-z_][A-Za-z0-9_]*").Select(match => match.Value)
            .Where(levels.ContainsKey).GroupBy(name => name).Select(group => (Name: group.Key, Count: group.Count(), Level: levels[group.Key]))
            .OrderByDescending(native => native.Level).ThenBy(native => native.Name, StringComparer.Ordinal).ToList();
        foreach (var native in used) Console.Write($"{native.Level,-9}{native.Name} x{native.Count}\n");
        Console.Write($"{used.Count} natives called, {used.Count(native => native.Level == Level.Missing)} Missing\n");
        return used.Any(native => native.Level == Level.Missing) ? 1 : 0;
    }
    catch (Exception e)
    {
        Console.Error.Write(e.Message + "\n");
        return 1;
    }
}

if (args is ["run", var map, .. var options])
{
    double seconds = 60;
    int seed = 0;
    string? hashEvery = null;
    for (int i = 0; i < options.Length; i += 2)
    {
        var value = i + 1 < options.Length ? options[i + 1] : "";
        var known = options[i] switch
        {
            "--seconds" => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) && seconds >= 0,
            "--seed" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed),
            "--hash" => (hashEvery = value) is not null,
            _ => false,
        };
        if (!known) return Usage();
    }

    try
    {
        using var simulation = Simulation.Load(map, new SimulationOptions { Seed = seed });
        try
        {
            if (hashEvery is not null && double.TryParse(hashEvery, NumberStyles.Float, CultureInfo.InvariantCulture, out var every) && every > 0)
                for (double at = every; at <= seconds + 1e-9 && simulation.Stopped is null; at += every)
                {
                    simulation.Advance(TimeSpan.FromSeconds(every));
                    Console.Write($"hash {at.ToString("0.###", CultureInfo.InvariantCulture)} {simulation.StateHash()}\n");
                }
            else simulation.Advance(TimeSpan.FromSeconds(seconds));
        }
        catch (LuaException) when (simulation.Stopped is not null) { }
        foreach (var line in simulation.Log) Console.Write(line + "\n");
        if (simulation.Stopped is null) return 0;
        Console.Error.Write(simulation.Stopped.Message + "\n");
        return 1;
    }
    catch (Exception e)
    {
        Console.Error.Write(e.Message + "\n");
        return 1;
    }
}

return Usage();

static int Usage()
{
    Console.Error.Write("""
        usage:
          mirror coverage                                  How many natives are at each trust level, and which are Missing
          mirror check <map>                               Lists the natives a map's script calls with their trust levels
          mirror run <map> [--seconds <n>] [--seed <n>] [--hash <n>]
                                                           Runs a Lua or JASS map for n simulated seconds (60) with a
                                                           seed (0), then prints its log; with --hash, the state hash
                                                           every n seconds

        """);
    return 2;
}
