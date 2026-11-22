using MirrorImaged.Data;

namespace MirrorImaged.GameTests;

public sealed class ProbeComparisonTests : IDisposable
{
    readonly string _file = Path.GetTempFileName();

    public void Dispose() => File.Delete(_file);

    // Each probe in each graphics mode it has lines for (decisions D683)
    static TheoryData<string, GraphicsMode> InModes(Func<string, bool> which)
    {
        var data = new TheoryData<string, GraphicsMode>();
        foreach (var name in ProbeComparison.Names.Where(which))
            foreach (var mode in ProbeComparison.Modes(name))
                data.Add(name, mode);
        return data;
    }

    public static TheoryData<string, GraphicsMode> Probes => InModes(name => !ProbeComparison.ReadsData(name));

    public static TheoryData<string, GraphicsMode> DataProbes => InModes(ProbeComparison.ReadsData);

    // Without the game: MirrorImaged against the game's lines in probes/expected (decisions D120, D121).
    [Theory]
    [MemberData(nameof(Probes))]
    public void A_probe_gives_the_games_lines_in_MirrorImaged_but_for_its_recorded_differences(string name, GraphicsMode mode) =>
        ProbeComparison.AssertMirror(name, mode);

    // The probes that read the game's data, with the install's standard data (decisions D213).
    [Trait("Needs", "Game")]
    [InstallTheory]
    [MemberData(nameof(DataProbes))]
    public void A_data_probe_gives_the_games_lines_in_MirrorImaged_with_the_installs_data_but_for_its_recorded_differences(string name, GraphicsMode mode) =>
        ProbeComparison.AssertMirror(name, mode);

    // The probes with recorded differences that an Exact native's fact cites (A2; decisions D841)
    static Func<string, bool> Cited(Func<string, bool> which) => name => which(name) && ProbeComparison.ExactCiting(name).Count > 0
        && Enum.GetValues<GraphicsMode>().Any(mode => File.Exists(ProbeComparison.Differences(name, mode)));

    public static TheoryData<string, GraphicsMode> CitedProbes => InModes(Cited(name => !ProbeComparison.ReadsData(name)));

    public static TheoryData<string, GraphicsMode> CitedDataProbes => InModes(Cited(ProbeComparison.ReadsData));

    static void AssertNoExactTouched(string name, GraphicsMode mode)
    {
        var touched = ProbeComparison.ExactTouched(name, mode);
        Assert.True(touched.Count == 0,
            "These Exact natives' facts cite the probe, and its recorded differences touch them: make the native match, name a Close native that explains the line in its note, or make the native Close:\n" +
            string.Join('\n', touched));
    }

    // An Exact native has no recorded difference in a probe its fact cites (A2; decisions D841).
    [Theory]
    [MemberData(nameof(CitedProbes))]
    public void An_exact_native_has_no_recorded_difference_in_the_probes_its_fact_cites(string name, GraphicsMode mode) =>
        AssertNoExactTouched(name, mode);

    [Trait("Needs", "Game")]
    [InstallTheory]
    [MemberData(nameof(CitedDataProbes))]
    public void An_exact_native_has_no_recorded_difference_in_the_data_probes_its_fact_cites(string name, GraphicsMode mode) =>
        AssertNoExactTouched(name, mode);

    // Every probe has the game's lines for both graphics modes, in one file or one a mode (decisions D683).
    [Fact]
    public void Every_probe_has_lines_for_both_graphics_modes() =>
        Assert.All(ProbeComparison.Names, name => Assert.Equal(Enum.GetValues<GraphicsMode>(), ProbeComparison.Modes(name)));

    List<Difference> Compare(string expected, params string[] run)
    {
        File.WriteAllText(_file, expected);
        return ProbeComparison.Compare(_file, run);
    }

    [Fact]
    public void A_varying_line_matches_any_number_anywhere_and_the_other_lines_come_in_order()
    {
        const string expected = "# a note\na\n~ w {t} true\nb\n";

        Assert.Empty(Compare(expected, "a", "b", "w 1.5 true"));
        Assert.Empty(Compare(expected, "w -2 true", "a", "b"));
        Assert.Equal([new(0, null, "w 1.5 false"), new(3, "~ w {t} true", null)], Compare(expected, "w 1.5 false", "a", "b"));
        Assert.Equal([new(2, "a", null), new(4, null, "a")], Compare(expected, "b", "a", "w 1 true"));
        Assert.Equal([new(3, "~ w {t} true", null)], Compare(expected, "a", "b"));
    }

    [Fact]
    public void Lines_that_differ_between_the_same_common_lines_are_paired_in_order()
    {
        const string expected = "a\nb\nc\nd\ne\n";

        Assert.Equal([new(2, "b", "B"), new(3, "c", "C")], Compare(expected, "a", "B", "C", "d", "e"));
        Assert.Equal([new(2, "b", "B"), new(3, "c", null), new(5, null, "f")], Compare(expected, "a", "B", "d", "e", "f"));
        Assert.Equal([new(1, null, "x"), new(5, "e", null)], Compare(expected, "a", "x", "b", "c", "d"));
        Assert.Equal([new(1, "a", null), new(2, "b", null), new(3, "c", null), new(4, "d", null), new(5, "e", null)], Compare(expected));
    }

    [Theory]
    [InlineData("~ w done")]
    [InlineData("~ {t}")]
    [InlineData("~ {w} {t}")]
    public void A_moving_line_without_both_varying_and_fixed_text_is_refused(string line)
    {
        var error = Assert.Throws<InvalidDataException>(() => Compare($"a\n{line}\n", "a"));

        Assert.Equal($"{_file}:2: a line starting \"~ \" moves, and needs a {{t}} or {{w}} for what varies and some text that doesn't", error.Message);
    }

    [Fact]
    public void A_varying_word_is_ascii_letters_digits_and_underscores()
    {
        const string expected = "slot 0 RACE_{w} team 0\n";

        Assert.Empty(Compare(expected, "slot 0 RACE_UNDEAD team 0"));
        Assert.Empty(Compare(expected, "slot 0 RACE_night_elf2 team 0"));
        Assert.Equal([new(1, "slot 0 RACE_{w} team 0", "slot 0 RACE_UNDEAD x team 0")], Compare(expected, "slot 0 RACE_UNDEAD x team 0"));
        Assert.Equal([new(1, "slot 0 RACE_{w} team 0", "slot 0 RACE_é team 0")], Compare(expected, "slot 0 RACE_é team 0"));
    }

    [Fact]
    public void A_line_with_a_varying_part_that_does_not_move_keeps_its_place()
    {
        const string expected = "a\nw {t} {w}\nb\n";

        Assert.Empty(Compare(expected, "a", "w 1.5 true", "b"));
        Assert.Equal([new(2, "w {t} {w}", null), new(3, null, "w 1.5 true")], Compare(expected, "a", "b", "w 1.5 true"));
    }

    // A differences line with no reason fails A2 and A6 (decisions D790).
    [Fact]
    public void Every_recorded_difference_has_a_reason()
    {
        var unreasoned = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "probes", "differences"), "*.txt")
            .SelectMany(file => ProbeComparison.Unreasoned(File.ReadAllLines(file)).Select(line => $"{Path.GetFileName(file)}:{line}"))
            .ToList();

        Assert.True(unreasoned.Count == 0,
            "These lines of probes/differences record a difference with no note above it giving the reason (line 0: the file doesn't open with its note on the form):\n" +
            string.Join('\n', unreasoned));
    }

    [Fact]
    public void A_recorded_line_needs_a_note_after_the_opening_one_and_a_file_needs_the_opening_one()
    {
        string[] opening = ["# Where MirrorImaged differs from the game's lines", "# starting - is the game's", ""];

        Assert.Empty(ProbeComparison.Unreasoned([.. opening, "# why", "- a", "+ b", "", "- c"]));
        Assert.Equal([4, 5], ProbeComparison.Unreasoned([.. opening, "- a", "+ b", "# why", "- c"]));
        Assert.Equal([3], ProbeComparison.Unreasoned(["# Where MirrorImaged differs", "", "- a"]));
        Assert.Equal([0], ProbeComparison.Unreasoned(["# why", "- a"]));
    }

    // Every expected and differences file belongs to a probe in probes/.
    [Fact]
    public void Every_record_has_its_probe()
    {
        var names = ProbeComparison.Names.ToHashSet();
        var records = new[] { "expected", "differences" }
            .SelectMany(folder => Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "probes", folder), "*.txt"))
            .Select(file => Path.GetFileNameWithoutExtension(file)!)
            .Select(name => Enum.GetValues<GraphicsMode>().Select(mode => "." + mode.ToString().ToLowerInvariant()).FirstOrDefault(name.EndsWith) is { } mode ? name[..^mode.Length] : name);

        Assert.All(records, name => Assert.Contains(name!, names));
    }

    [Fact]
    public void A_difference_names_the_line_and_both_sides_and_is_recorded_as_a_diff()
    {
        Assert.Equal("line 2: the game gave \"b\", MirrorImaged \"B\"", new Difference(2, "b", "B").ToString());
        Assert.Equal("line 3: the game gave \"c\", MirrorImaged nothing", new Difference(3, "c", null).ToString());
        Assert.Equal("after line 5: the game gave nothing, MirrorImaged \"f\"", new Difference(5, null, "f").ToString());
        Assert.Equal("MirrorImaged stopped without lines: war3map.lua:3: no", new Difference(0, null, null, "war3map.lua:3: no").ToString());
        Assert.Equal(["- b", "+ B"], new Difference(2, "b", "B").Recorded);
        Assert.Equal(["- c"], new Difference(3, "c", null).Recorded);
        Assert.Equal(["+ f"], new Difference(5, null, "f").Recorded);
        Assert.Equal(["! war3map.lua:3: no"], new Difference(0, null, null, "war3map.lua:3: no").Recorded);
    }
}

/// <summary>A theory that reads the Warcraft III install on this computer; skipped when there is none.</summary>
public sealed class InstallTheoryAttribute : TheoryAttribute
{
    public InstallTheoryAttribute()
    {
        if (MirrorImaged.Data.GameInstall.Find() is null)
            Skip = $"No Warcraft III install in {MirrorImaged.Data.GameInstall.Place()}; set {MirrorImaged.Data.GameInstall.Variable} to run it";
    }
}
