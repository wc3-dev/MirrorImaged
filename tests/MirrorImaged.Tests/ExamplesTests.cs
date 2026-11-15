namespace MirrorImaged.Tests;

/// <summary>A14: the worked examples of the guide run: each map is built from its script and its Lua tests pass with <c>mirror test</c>.</summary>
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

    static string Examples()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
            if (Directory.Exists(Path.Combine(folder.FullName, "examples"))) return Path.Combine(folder.FullName, "examples");
        throw new DirectoryNotFoundException("No examples folder above " + AppContext.BaseDirectory);
    }

    [InstallTheory]
    [InlineData("lua/gold", "war3map.lua", false)]
    [InlineData("jass/gold", "war3map.j", true)]
    [InlineData("typescript/gold", "../../lua/gold/war3map.lua", false)]
    public void An_example_map_passes_its_lua_tests(string example, string script, bool jass)
    {
        var source = Path.Combine(Examples(), example);
        var text = File.ReadAllText(Path.GetFullPath(Path.Combine(source, script)));
        var map = _maps.Build(text, jass ? War3Net.Build.Info.ScriptLanguage.Jass : War3Net.Build.Info.ScriptLanguage.Lua);
        File.Copy(map, Path.Combine(_folder, "gold.w3x"));
        File.Copy(Path.Combine(source, "gold.test.lua"), Path.Combine(_folder, "gold.test.lua"));

        var (exit, output, error) = CoverageTests.Mirror("test", _folder);

        Assert.True(exit == 0, output + error);
        Assert.Contains("PASS gold.test.lua", output);
    }
}
