using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using MirrorImaged.TestAdapter;

namespace MirrorImaged.Tests;

/// <summary>The adapter that lets <c>dotnet test</c> and the test explorers find and run the Lua tests that sit beside a test assembly.</summary>
public sealed class LuaTestAdapterTests : IDisposable
{
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    sealed class Sink : ITestCaseDiscoverySink
    {
        public List<TestCase> Cases { get; } = [];
        public void SendTestCase(TestCase discoveredTest) => Cases.Add(discoveredTest);
    }

    sealed class Handle : IFrameworkHandle
    {
        public List<TestResult> Results { get; } = [];
        public bool EnableShutdownAfterTestRun { get; set; }
        public void RecordResult(TestResult testResult) => Results.Add(testResult);
        public void RecordStart(TestCase testCase) { }
        public void RecordEnd(TestCase testCase, TestOutcome outcome) { }
        public void RecordAttachments(IList<AttachmentSet> attachmentSets) { }
        public int LaunchProcessWithDebuggerAttached(string filePath, string? workingDirectory, string? arguments, IDictionary<string, string?>? environmentVariables) => 0;
        public void SendMessage(TestMessageLevel testMessageLevel, string message) { }
    }

    string Source()
    {
        File.WriteAllText(Path.Combine(_folder, "sums.test.lua"), """
            test("adds", function() assert_eq(1 + 1, 2) end)
            test("fails with a reason", function() assert_eq(1 + 1, 3) end)
            """);
        Directory.CreateDirectory(Path.Combine(_folder, "more"));
        File.WriteAllText(Path.Combine(_folder, "more", "other.test.lua"), "test(\"in a folder below\", function() assert_true(true) end)");
        File.WriteAllText(Path.Combine(_folder, "Tests.dll"), "");
        return Path.Combine(_folder, "Tests.dll");
    }

    [Fact]
    public void The_tests_of_the_lua_files_beside_an_assembly_are_found_with_their_names_and_files()
    {
        var sink = new Sink();

        new LuaTestDiscoverer().DiscoverTests([Source()], null!, null!, sink);

        Assert.Equal(["in a folder below", "adds", "fails with a reason"], sink.Cases.Select(c => c.DisplayName).OrderBy(n => n == "in a folder below" ? 0 : 1).ThenBy(n => n, StringComparer.Ordinal).Select(n => n));
        Assert.All(sink.Cases, c => Assert.EndsWith(".test.lua", c.Source));
        Assert.Equal(new Uri(LuaTestAdapter.ExecutorUri), sink.Cases[0].ExecutorUri);
    }

    [Fact]
    public void The_executor_runs_each_test_and_says_which_passed_and_why_one_failed()
    {
        var handle = new Handle();

        new LuaTestExecutor().RunTests([Source()], null, handle);

        Assert.Equal(["adds:Passed", "fails with a reason:Failed", "in a folder below:Passed"], handle.Results.Select(r => $"{r.TestCase.DisplayName}:{r.Outcome}").Order(StringComparer.Ordinal));
        Assert.Contains("expected 3, got 2", handle.Results.Single(r => r.Outcome == TestOutcome.Failed).ErrorMessage);
    }
}
