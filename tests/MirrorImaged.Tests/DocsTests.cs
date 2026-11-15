using System.Text.RegularExpressions;

namespace MirrorImaged.Tests;

/// <summary>A14: the guide has a page for each part of the tool, its links go somewhere, and it names every command the command line has.</summary>
public sealed partial class DocsTests
{
    static string Guide()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
            if (Directory.Exists(Path.Combine(folder.FullName, "docs", "guide"))) return Path.Combine(folder.FullName, "docs", "guide");
        throw new DirectoryNotFoundException("No docs/guide folder above " + AppContext.BaseDirectory);
    }

    [GeneratedRegex(@"\]\(([^)#]+\.md)(#[^)]*)?\)")]
    private static partial Regex Link();

    [Fact]
    public void Every_link_in_the_guide_goes_to_a_page_that_exists()
    {
        var guide = Guide();
        foreach (var page in Directory.GetFiles(guide, "*.md"))
            foreach (Match link in Link().Matches(File.ReadAllText(page)))
                Assert.True(File.Exists(Path.Combine(guide, link.Groups[1].Value)), $"{Path.GetFileName(page)} links to {link.Groups[1].Value}");
    }

    [Fact]
    public void The_index_lists_every_page()
    {
        var guide = Guide();
        var index = File.ReadAllText(Path.Combine(guide, "index.md"));
        foreach (var page in Directory.GetFiles(guide, "*.md").Select(Path.GetFileName).Where(name => name != "index.md"))
            Assert.Contains($"]({page})", index);
    }

    [Fact]
    public void The_command_line_page_names_every_command_the_usage_lists()
    {
        var usage = CoverageTests.Mirror("nothing").Error;
        var commands = Regex.Matches(usage, @"^\s+mirror (\w+)", RegexOptions.Multiline).Select(match => match.Groups[1].Value).Distinct().ToList();
        var page = File.ReadAllText(Path.Combine(Guide(), "cli.md"));

        Assert.NotEmpty(commands);
        foreach (var command in commands) Assert.Contains($"`mirror {command}", page);
    }
}
