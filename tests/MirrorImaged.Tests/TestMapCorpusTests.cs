using MirrorImaged.Data;
using Xunit.Abstractions;

namespace MirrorImaged.Tests;

/// <summary>A fact over the maps in this computer's Warcraft III maps folder; skipped where there is no install or no such folder.</summary>
public sealed class TestMapsFactAttribute : FactAttribute
{
    public TestMapsFactAttribute()
    {
        if (GameInstall.Find() is null) Skip = $"No Warcraft III install in {GameInstall.Place()}; set {GameInstall.Variable} to run it";
        else if (!Directory.Exists(TestMapCorpusTests.Folder)) Skip = $"No maps folder at {TestMapCorpusTests.Folder}";
    }
}

/// <summary>
/// The test and probe maps in this computer's Warcraft III maps folder, map files and folder maps alike, but for what is
/// in its <c>amai</c> and <c>Download</c> folders: each runs a minute with no script error (decisions D514). None of the
/// maps is in the repository.
/// </summary>
[Collection(Corpus.Name)]
[Trait("Needs", "Game")]
public sealed class TestMapCorpusTests(ITestOutputHelper output)
{
    internal static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Warcraft III", "Maps");

    static readonly string[] NotOurs = ["amai", "Download"];

    // A file or a folder named as a map is a map; any other folder is looked into
    internal static IEnumerable<string> Maps(string folder, bool top = true)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(folder).Order(StringComparer.Ordinal))
            if (Path.GetExtension(entry).ToLowerInvariant() is ".w3x" or ".w3m") yield return entry;
            else if (Directory.Exists(entry) && !(top && NotOurs.Contains(Path.GetFileName(entry), StringComparer.OrdinalIgnoreCase)))
                foreach (var map in Maps(entry, false)) yield return map;
    }

    [TestMapsFact]
    public void Every_test_map_runs_a_minute_with_no_script_error()
    {
        var maps = Maps(Folder).ToList();
        var failures = new string?[maps.Count];

        Corpus.Each(maps, (at, map) => failures[at] = Failure(map));

        foreach (var (map, failure) in maps.Zip(failures))
            output.WriteLine($"{(failure is null ? "ran   " : "FAILED")} {(Directory.Exists(map) ? "folder" : "file  ")} {Path.GetRelativePath(Folder, map)}{(failure is null ? "" : ": " + failure)}");
        Assert.Empty(maps.Zip(failures).Where(run => run.Second is not null).Select(run => $"{Path.GetRelativePath(Folder, run.First)}: {run.Second}"));
    }

    // What went wrong in the map's minute: what stopped it, or its first script error; null when nothing did
    static string? Failure(string map)
    {
        try
        {
            using var simulation = Simulation.Load(map);
            try { simulation.Advance(TimeSpan.FromSeconds(60)); }
            catch (Exception) when (simulation.Stopped is not null) { }
            return simulation.Stopped?.Message ?? simulation.Log.FirstOrDefault(line => line.Split(' ').ElementAtOrDefault(1) == "error:");
        }
        catch (Exception e) { return e.Message; }
    }
}
