using System.Diagnostics;
using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>A fact that compiles TypeScript: skipped when there is no Warcraft III install or no <c>npm</c> on the path.</summary>
public sealed class NodeFactAttribute : FactAttribute
{
    public NodeFactAttribute()
    {
        if (GameInstall.Find() is null) Skip = $"No Warcraft III install in {GameInstall.Place()}; set {GameInstall.Variable} to run it";
        else if (ExamplesTests.OnPath(ExamplesTests.Npm) is null) Skip = "No npm on the path; install Node.js to run it";
    }
}

/// <summary>
/// A14: the worked examples of the guide run: the Lua and JASS maps are folder maps as they are in the repository, the TypeScript and C# ones are built
/// from their source by their own tools, and each one's Lua tests pass with <c>mirror test</c>.
/// </summary>
[Trait("Needs", "Game")]
public sealed class ExamplesTests : IDisposable
{
    readonly TestMaps _maps = new();
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose()
    {
        _maps.Dispose();
        Directory.Delete(_folder, true);
    }

    internal static string Npm => OperatingSystem.IsWindows() ? "npm.cmd" : "npm";

    internal static string? OnPath(string file) =>
        (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(folder => Path.Combine(folder, file)).FirstOrDefault(File.Exists);

    internal static string Examples()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
            if (Directory.Exists(Path.Combine(folder.FullName, "examples"))) return Path.Combine(folder.FullName, "examples");
        throw new DirectoryNotFoundException("No examples folder above " + AppContext.BaseDirectory);
    }

    // A copy of the example to build in, so the repository's folder gets no build output
    string Copy(string example)
    {
        var from = Path.Combine(Examples(), example);
        var to = Path.Combine(_folder, "copy");
        foreach (var file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetRelativePath(from, file);
            if (name.Split(Path.DirectorySeparatorChar).Any(part => part is "node_modules" or "bin" or "obj" or "build" or "map.w3x" or "gold.w3x")) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(to, name))!);
            File.Copy(file, Path.Combine(to, name));
        }
        return to;
    }

    static void Run(string folder, string file, params string[] arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(file, arguments) { WorkingDirectory = folder, RedirectStandardOutput = true, RedirectStandardError = true })!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"{file} {string.Join(' ', arguments)}: {output}{error.Result}");
    }

    void Passes(string folder)
    {
        var (exit, output, error) = CoverageTests.Mirror("test", folder);

        Assert.True(exit == 0, output + error);
        Assert.Contains("PASS gold.test.lua", output);
    }

    [InstallTheory]
    [InlineData("lua/gold")]
    [InlineData("jass/gold")]
    public void An_example_folder_map_passes_its_lua_tests(string example) => Passes(Path.Combine(Examples(), example));

    [NodeFact]
    public void The_typescript_example_compiles_to_a_map_that_passes_its_lua_tests()
    {
        var copy = Copy("typescript/gold");
        Run(copy, OnPath(Npm)!, "ci");
        Run(copy, OnPath(Npm)!, "run", "build");

        Passes(copy);
    }

    [InstallFact]
    public void The_csharp_example_builds_with_moonwell_into_a_map_that_passes_its_lua_tests()
    {
        var copy = Copy("csharp/gold");
        // The terrain a Moonwell project keeps in map.w3x, which World Editor saves and the repository holds no copy of
        Directory.Move(_maps.Unpack(_maps.Build("")), Path.Combine(copy, "map.w3x"));
        Run(copy, "dotnet", "tool", "restore");
        Run(copy, "dotnet", "moonwell", "build");

        Passes(copy);
    }
}

/// <summary>The Lua and JASS examples are folder maps a reader can run as they are: each reads as a map whose script is its own file.</summary>
public sealed class ExampleMapsTests
{
    [Theory]
    [InlineData("lua/gold", "war3map.lua", false)]
    [InlineData("jass/gold", "war3map.j", true)]
    public void An_example_is_a_folder_map_with_its_script(string example, string script, bool jass)
    {
        var map = Path.Combine(ExamplesTests.Examples(), example, "gold.w3x");

        var archive = MapArchive.Read(map);

        Assert.Equal(jass, archive.Jass);
        Assert.Equal(File.ReadAllBytes(Path.Combine(map, script)), archive.Script);
        Assert.Null(archive.Files.Info);
    }
}
