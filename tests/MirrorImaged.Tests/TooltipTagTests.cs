using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// ParseTags and the tags of extended tooltips, against the facts' "Spells and abilities" (game-facts.md, 10-29 and 11-08,
/// MirrorImaged probe data-tags): each <c>&lt;id,column&gt;</c> filled with the object's value, whole, in the map's changes too.
/// </summary>
public sealed class TooltipTagTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static StandardData Standard() =>
        AbilityTests.Standard()
            .Add(ObjectKind.Unit, "hrif", new Dictionary<string, string> { ["unam"] = "Rifleman", ["uhpm"] = "230", ["ua1b"] = "11", ["ua1d"] = "1", ["ua1s"] = "2", ["umvs"] = "270" });

    List<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // facts: whole numbers, ",%" times 100, ",." one decimal and a whole value without one, other text after the column ignored
    [Fact]
    public void Tags_show_whole_numbers_percentages_and_one_decimal()
    {
        var lines = Run("""
            print(ParseTags("<AHhb,DataA1>|<AHhb,DataA2>|<AHhb,DataA2,x>|<AHhb,DataA1,%>|<AHhb,Cool1>|<AHhb,Cost1>|<AHhb,Rng1,.>"))
            print(ParseTags("<Adef,DataA1>|<Adef,DataA1,%>|<Adef,DataA1,.>|<AHhb,dataa3>"))
            """);

        Assert.Equal(["200|400|400|20000|5|65|800", "0|50|0.5|600"], lines);
    }

    // facts: a missing object, an id in the wrong case, a level the object lacks, a text column and an unfilled tag all show 0, a bare id nothing, an unclosed tag drops the rest
    [Fact]
    public void Tags_that_cannot_be_filled_show_zero_and_the_text_around_them_stays()
    {
        var lines = Run("""
            print(ParseTags("<Zzzz,DataA1>|<ahhb,DataA2>|<AHhb,DataA9>|<AHhb>|<AHhb,Name>|<<AHhb,DataA2>>|100%d|<AHhb,DataA2"))
            print(ParseTags(""), ParseTags("plain"))
            """);

        Assert.Equal(["0|0|0||0|0>|100%d|", "\tplain"], lines);
    }

    // facts: a unit's <id,HP>, <id,realHP>, <id,mindmg1> and <id,maxdmg1> fill, and its other columns (spd) read 0
    [Fact]
    public void A_unit_tag_reads_its_life_and_damage()
    {
        var lines = Run("""print(ParseTags("<hrif,HP>|<hrif,realHP>|<hrif,mindmg1>|<hrif,maxdmg1>|<hrif,spd>|<hrif,mindmg2>"))""");

        Assert.Equal(["230|230|12|13|0|0"], lines);
    }

    // The extended tooltips of an ability, and its learn tooltip, come back with their tags filled, level by level
    [Fact]
    public void Extended_tooltips_come_with_their_tags_filled()
    {
        var lines = Run("""
            local hb = FourCC("AHhb")
            print(BlzGetAbilityExtendedTooltip(hb, 0), BlzGetAbilityExtendedTooltip(hb, 1), BlzGetAbilityExtendedTooltip(hb, 2), BlzGetAbilityResearchExtendedTooltip(hb, 0))
            BlzSetAbilityExtendedTooltip(hb, "now <AHhb,DataA3,%>", 0)
            print(BlzGetAbilityExtendedTooltip(hb, 0))
            """);

        Assert.Equal(["heal 200\theal 400\theal 600\tlearn 200", "now 60000"], lines);
    }
}
