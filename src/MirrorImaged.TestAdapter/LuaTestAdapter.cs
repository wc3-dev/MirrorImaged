using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace MirrorImaged.TestAdapter;

/// <summary>The id by which the test platform knows this adapter's tests.</summary>
public static class LuaTestAdapter
{
    /// <summary>The executor's uri.</summary>
    public const string ExecutorUri = "executor://MirrorImagedLuaTests";

    /// <summary>The Lua test files beside a test assembly: its <c>*.test.lua</c> files and those in the folders below it.</summary>
    public static IEnumerable<string> Files(string source)
    {
        var folder = Directory.Exists(source) ? source : Path.GetDirectoryName(Path.GetFullPath(source));
        return folder is null ? [] : Directory.EnumerateFiles(folder, "*.test.lua", SearchOption.AllDirectories).Order(StringComparer.Ordinal);
    }
}

/// <summary>Finds the tests of the Lua test files that sit beside the test assemblies the platform is given.</summary>
[FileExtension(".dll")]
[FileExtension(".lua")]
[DefaultExecutorUri(LuaTestAdapter.ExecutorUri)]
public sealed class LuaTestDiscoverer : ITestDiscoverer
{
    /// <summary>Makes a test case of each <c>test(name, ...)</c> a file calls.</summary>
    public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
    {
        foreach (var file in sources.SelectMany(source => source.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) ? [source] : LuaTestAdapter.Files(source)).Distinct())
            foreach (var name in LuaTestHost.Names(file))
                discoverySink.SendTestCase(Case(file, name));
    }

    internal static TestCase Case(string file, string name) => new($"{Path.GetFileName(file)}.{name}", new Uri(LuaTestAdapter.ExecutorUri), file)
    {
        DisplayName = name,
        CodeFilePath = file,
    };
}

/// <summary>Runs the Lua tests the platform asks for, or all of those beside the assemblies it is given.</summary>
[ExtensionUri(LuaTestAdapter.ExecutorUri)]
public sealed class LuaTestExecutor : ITestExecutor
{
    bool _cancelled;

    /// <summary>Runs the tests of the sources.</summary>
    public void RunTests(IEnumerable<string>? sources, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        var cases = new List<TestCase>();
        var sink = new Collect(cases);
        new LuaTestDiscoverer().DiscoverTests(sources ?? [], runContext!, frameworkHandle!, sink);
        RunTests(cases, runContext, frameworkHandle);
    }

    /// <summary>Runs the given tests, each file once.</summary>
    public void RunTests(IEnumerable<TestCase>? tests, IRunContext? runContext, IFrameworkHandle? frameworkHandle)
    {
        foreach (var file in (tests ?? []).GroupBy(test => test.Source))
        {
            if (_cancelled) return;
            using var host = new LuaTestHost();
            var wanted = file.Select(test => test.DisplayName).ToHashSet();
            foreach (var result in host.Run(file.Key).Where(r => wanted.Contains(r.Name)))
            {
                var test = file.First(t => t.DisplayName == result.Name);
                frameworkHandle?.RecordResult(new Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult(test)
                {
                    Outcome = result.Passed ? TestOutcome.Passed : TestOutcome.Failed,
                    ErrorMessage = result.Message,
                    Duration = result.Time,
                });
            }
        }
    }

    /// <summary>Stops after the file being run.</summary>
    public void Cancel() => _cancelled = true;

    sealed class Collect(List<TestCase> cases) : ITestCaseDiscoverySink
    {
        public void SendTestCase(TestCase discoveredTest) => cases.Add(discoveredTest);
    }
}
