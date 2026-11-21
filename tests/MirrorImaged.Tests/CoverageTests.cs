using System.Diagnostics;
using System.Reflection;
using MirrorImaged.Natives;

namespace MirrorImaged.Tests;

public sealed class CoverageTests
{
    sealed class SomeNatives
    {
        [Native(Level.Missing)] public void Zeta() { }
        [Native(Level.Exact, Fact = "game-facts.md, Timers")] public void Alpha() { }
        [Native(Level.Missing)] public void Beta() { }
        [Native(Level.Recorded, Reason = "Plays a sound")] public void Gamma() { }
        [Native(Level.Close)] public void Delta() { }
        public void Helper() { }
    }

    [Fact]
    public void Counts_the_natives_at_each_level_and_lists_the_missing_ones_sorted()
    {
        var coverage = Coverage.Of(typeof(SomeNatives));

        Assert.Equal(1, coverage.Counts[Level.Exact]);
        Assert.Equal(1, coverage.Counts[Level.Close]);
        Assert.Equal(1, coverage.Counts[Level.Recorded]);
        Assert.Equal(2, coverage.Counts[Level.Missing]);
        Assert.Equal(["Beta", "Zeta"], coverage.Missing);
    }

    [Fact]
    public void A_level_with_no_natives_counts_zero()
    {
        Assert.Equal(0, Coverage.Of(typeof(object)).Counts[Level.Exact]);
    }

    // A Close native whose body only returns, or returns a constant, names why that is the game's answer (decisions D791):
    // otherwise it is a model still to write, or a Recorded native.
    [Fact]
    public void A_Close_native_that_answers_a_constant_gives_the_reason()
    {
        var unexplained = typeof(GameNatives).GetMethods()
            .Where(method => method.GetCustomAttribute<NativeAttribute>() is { Level: Level.Close, Reason: null } && AnswersAConstant(method))
            .Select(method => method.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Empty(unexplained);
    }

    [Fact]
    public void A_body_that_only_returns_or_returns_a_constant_answers_a_constant()
    {
        Assert.True(AnswersAConstant(typeof(SomeNatives).GetMethod(nameof(SomeNatives.Delta))!));
        Assert.True(AnswersAConstant(typeof(Bodies).GetMethod(nameof(Bodies.Zero))!));
        Assert.True(AnswersAConstant(typeof(Bodies).GetMethod(nameof(Bodies.Text))!));
        Assert.False(AnswersAConstant(typeof(Bodies).GetMethod(nameof(Bodies.Same))!));
    }

    sealed class Bodies
    {
        public int Zero() => 0;
        public string Text() => "a";
        public int Same(int x) => x;
    }

    // IL of nops around one load of a constant (or none) and a return
    static bool AnswersAConstant(MethodInfo method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray() ?? [];
        var i = 0;
        while (i < il.Length && il[i] == 0x00) i++;
        if (i < il.Length && il[i] != 0x2A)
            i += il[i] switch
            {
                0x14 or (>= 0x15 and <= 0x1E) => 1, // ldnull, ldc.i4.m1 to ldc.i4.8
                0x1F => 2, // ldc.i4.s
                0x20 or 0x22 or 0x72 => 5, // ldc.i4, ldc.r4, ldstr
                0x23 => 9, // ldc.r8
                _ => il.Length,
            };
        while (i < il.Length && il[i] == 0x00) i++;
        return i == il.Length - 1 && il[i] == 0x2A;
    }

    [Fact]
    public void The_report_gives_the_counts_then_the_missing_natives()
    {
        Assert.Equal("""
            Exact       1
            Close       1
            Recorded    1
            Missing     2
            All         5

            Missing:
            Beta
            Zeta

            """.ReplaceLineEndings("\n"), Coverage.Of(typeof(SomeNatives)).ToString());
    }

    [Fact]
    public void Mirror_coverage_prints_the_report_of_the_natives()
    {
        var (exit, output, _) = Mirror("coverage");

        Assert.Equal(0, exit);
        Assert.Equal(Coverage.Of(typeof(GameNatives)).ToString(), output);
    }

    [Fact]
    public void Mirror_with_no_command_prints_its_usage()
    {
        var (exit, _, error) = Mirror();

        Assert.Equal(2, exit);
        Assert.Contains("mirror coverage", error);
    }

    internal static (int Exit, string Output, string Error) Mirror(params string[] arguments) => Mirror(new Dictionary<string, string?>(), arguments);

    /// <summary>Runs <c>mirror</c> with the <paramref name="environment"/> variables set.</summary>
    internal static (int Exit, string Output, string Error) Mirror(IReadOnlyDictionary<string, string?> environment, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet", [Path.Combine(AppContext.BaseDirectory, "MirrorImaged.Cli.dll"), .. arguments])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var (name, value) in environment) start.Environment[name] = value;
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error.Result);
    }
}
