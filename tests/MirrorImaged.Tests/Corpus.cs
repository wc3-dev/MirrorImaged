using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// How the tests that run every map of the install share the work: each test is a theory of <see cref="Shards"/> parts, so a part ends inside
/// the 10 minutes the test run gives a test (tests/Tests.runsettings), and a part runs its maps side by side on half the PC's processors
/// (decisions D440, D441). The two corpus classes are one collection, so they never run at once.
/// </summary>
[CollectionDefinition(Name)]
public sealed class Corpus
{
    public const string Name = "Corpus";
    public const int Shards = 8;

    public static TheoryData<int> Parts => [.. Enumerable.Range(0, Shards)];

    /// <summary>Every <see cref="Shards"/>th map from the part's own, so the long maps (which sort together, by their players) are spread over the parts.</summary>
    public static List<string> Part(IEnumerable<string> maps, int part) => [.. maps.Where((_, at) => at % Shards == part)];

    /// <summary>Does the work for each map, as many at once as half the processors; one map's run never touches another's.</summary>
    public static void Each(IReadOnlyList<string> maps, Action<int, string> work) =>
        Parallel.For(0, maps.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) }, at => work(at, maps[at]));

    /// <summary>The map's bytes from the install; the storage is read by one at a time.</summary>
    public static byte[] Read(CascStorage storage, string name)
    {
        lock (storage) return storage.Read(name) ?? throw new InvalidDataException($"{name} isn't stored locally");
    }
}
