using MirrorImaged.Data;
using War3Net.Build;
using War3Net.IO.Mpq;

namespace MirrorImaged.Tests;

/// <summary>The frame tree against the facts' "UI: frames" (game-facts.md, 09-29 to 11-14): creation, what the getters read back, the click event.</summary>
public sealed class FrameTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    List<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            function config() end
            function main()
            {{main}}
            end
            """, files: new Dictionary<string, string>
            {
                ["kit.toc"] = "kit.fdf\n",
                ["kit.fdf"] = "Frame \"BACKDROP\" \"KitPanel\" {\n  Width 0.2,\n  Height 0.1,\n}\nFrame \"TEXT\" \"KitLabel\" {\n}\n",
            }), "", data: new StandardData());
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // facts: an unknown template gives nil, a map's own template comes with its TOC; by type always makes a frame
    [Fact]
    public void Frames_are_made_from_a_loaded_toc_or_by_type()
    {
        var lines = Run("""
            print(BlzCreateFrame("KitPanel", BlzGetOriginFrame(ORIGIN_FRAME_GAME_UI, 0), 0, 0), BlzLoadTOCFile("nothing.toc"))
            print(BlzLoadTOCFile("kit.toc"))
            local game = BlzGetOriginFrame(ORIGIN_FRAME_GAME_UI, 0)
            local panel = BlzCreateFrame("KitPanel", game, 0, 0)
            print(panel ~= nil, BlzCreateFrame("Zzz", game, 0, 0), BlzFrameGetName(panel), BlzFrameGetParent(panel) == game, BlzGetFrameByName("KitPanel", 0) == panel, BlzGetFrameByName("Zzz", 0))
            local text = BlzCreateFrameByType("TEXT", "Label", panel, "UnknownTemplate", 0)
            print(text ~= nil, BlzFrameGetChildrenCount(panel), BlzFrameGetChild(panel, 0) == text, BlzFrameGetChild(panel, 1), BlzGetOriginFrame(ORIGIN_FRAME_GAME_UI, 0) == game)
            """);

        Assert.Equal(["nil\tfalse", "true", "true\tnil\tKitPanel\ttrue\ttrue\tnil", "true\t1\ttrue\tnil\ttrue"], lines);
    }

    // facts: text, alpha, enable, value, size and visibility read back; a frame inside a hidden one is not visible; destroying a frame removes its children
    [Fact]
    public void Frames_read_back_what_was_set()
    {
        var lines = Run("""
            local game = BlzGetOriginFrame(ORIGIN_FRAME_GAME_UI, 0)
            local a = BlzCreateFrameByType("BACKDROP", "A", game, "", 0)
            local b = BlzCreateFrameByType("TEXT", "B", a, "", 0)
            BlzFrameSetText(b, "hello")
            BlzFrameAddText(b, "more")
            BlzFrameSetAlpha(b, 300)
            BlzFrameSetEnable(b, false)
            BlzFrameSetValue(b, 2.5)
            BlzFrameSetSize(a, 0.2, 0.1)
            BlzFrameSetAllPoints(b, a)
            print(BlzFrameGetText(b):gsub("\n", "|"), BlzFrameGetAlpha(b), BlzFrameGetEnable(b), BlzFrameGetValue(b), BlzFrameGetWidth(b), BlzFrameGetHeight(b), BlzFrameGetWidth(game))
            print(BlzFrameIsVisible(b))
            BlzFrameSetVisible(a, false)
            print(BlzFrameIsVisible(a), BlzFrameIsVisible(b))
            BlzFrameSetTextSizeLimit(b, 10)
            print(BlzFrameGetTextSizeLimit(b), BlzFrameToPixelX(0.3), BlzPixelToFrameY(900))
            BlzDestroyFrame(a)
            print(BlzFrameIsVisible(b), BlzGetFrameByName("B", 0), BlzFrameGetChildrenCount(game))
            """);

        Assert.Equal(["hello|more	255	false	2.5	0.2	0.1	0.8", "true", "false	false", "10	540	0.5", "false	nil	0"], lines);
    }

    // a click fires the frame's click registrations with the frame the trigger frame; a disabled frame takes none
    [Fact]
    public void A_click_fires_the_frames_event()
    {
        var lines = Run("""
            local button = BlzCreateFrameByType("GLUEBUTTON", "Button", BlzGetOriginFrame(ORIGIN_FRAME_GAME_UI, 0), "", 0)
            local t = CreateTrigger()
            BlzTriggerRegisterFrameEvent(t, button, FRAMEEVENT_CONTROL_CLICK)
            TriggerAddAction(t, function() print("clicked", BlzGetTriggerFrame() == button, BlzGetTriggerFrame() == BlzGetOriginFrame(ORIGIN_FRAME_GAME_UI, 0), GetHandleId(BlzGetTriggerFrameEvent()), BlzGetTriggerFrameText()) end)
            BlzFrameClick(button)
            BlzFrameSetEnable(button, false)
            BlzFrameClick(button)
            print(BlzGetTriggerFrame())
            """);

        Assert.Equal(["clicked\ttrue\tfalse\t1\t", "nil"], lines);
    }
}
