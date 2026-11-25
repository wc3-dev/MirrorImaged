using System.Text.RegularExpressions;

namespace MirrorImaged.Natives;

public partial class framehandle
{
    internal string Name = "", Type = "", Text = "";
    internal framehandle? Parent, Tooltip;
    internal readonly List<framehandle> Children = [];
    internal bool Visible = true, Enabled = true, Destroyed;
    internal int Alpha = 255, Level, TextSizeLimit = 255;
    internal float Width, Height, Value, Min, Max = 1, Step, Scale = 1;
    internal framehandle? Relative; // BlzFrameSetAllPoints's frame, whose size an unsized frame takes
}

// The frame tree: frames made from the map's FDF files (through the TOC the map loads) or by type, the origin frames of the
// game's own UI, and what the getters read back (text, alpha, visibility, size, value, parent and children). Layout is Close: a
// frame has the size set, or the size of the frame it fills. What is drawn (textures, models, fonts, colours) is Recorded; input
// events fire for BlzFrameClick only. Game-facts, "UI: frames".
public sealed partial class GameNatives
{
    const string FrameFacts = "game-facts.md, UI: frames (09-29 to 11-14)";
    const float ScreenWidth = 0.8f, ScreenHeight = 0.6f, ClientHeight = 1080;

    // The frame names the map's FDF files define (those of a loaded TOC), and the origin frames made so far
    readonly HashSet<string> _fdfNames = new(StringComparer.Ordinal);
    readonly Dictionary<(int, int), framehandle> _origins = [];
    readonly List<framehandle> _frames = [];
    (framehandle? Frame, int Type, string Text, float Value)? _frameEvent;

    framehandle? NewFrame(string name, string type, framehandle? owner)
    {
        var frame = Make(new framehandle { Name = name, Type = type, Parent = owner });
        owner?.Children.Add(frame);
        _frames.Add(frame);
        return frame;
    }

    /// <summary>
    /// Reads the TOC file from the map's files, then each FDF it lists, for the frame names they define (<c>Frame "TYPE"
    /// "Name"</c>); false for a file the map doesn't have. Close: the game's own TOC files aren't read, so a frame of its own
    /// UI is found by <c>BlzCreateFrame</c> only through the names the facts record.
    /// </summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game once gave false for a TOC listing one FDF of three frames, cause not separated; this gives true whenever the TOC file exists.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public bool BlzLoadTOCFile(string? TOCFile)
    {
        if (TOCFile is null || Data.Map?.File(TOCFile) is not { } toc) return false;
        foreach (var line in System.Text.Encoding.UTF8.GetString(toc).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            if (Data.Map?.File(line) is { } fdf)
                ReadFdf(System.Text.Encoding.UTF8.GetString(fdf));
        return true;
    }

    // The named frames nested in each top-level frame of an FDF (Frame, Texture, String), made along with it (decisions D398)
    readonly Dictionary<string, List<(string Type, string Name)>> _fdfChildren = new(StringComparer.Ordinal);

    void ReadFdf(string text)
    {
        var (depth, top) = (0, "");
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            // `Frame "TYPE" "Name"`, and in a simple frame `Texture "Name"` and `String "Name" "Font"`
            if (Regex.Match(trimmed, "^(?:Frame\\s+\"(?<type>[A-Z_]+)\"\\s+\"(?<name>[^\"]+)\"|(?<type>Texture|String)\\s+\"(?<name>[^\"]+)\")") is { Success: true } m)
            {
                var name = m.Groups["name"].Value;
                _fdfNames.Add(name);
                if (depth == 0) { top = name; _fdfChildren[top] = []; }
                else if (top != "") _fdfChildren[top].Add((m.Groups["type"].Value, name));
            }
            depth = Math.Max(depth + trimmed.Count(c => c == '{') - trimmed.Count(c => c == '}'), 0);
        }
    }

    framehandle? NewTemplateFrame(string name, string type, framehandle? owner)
    {
        var frame = NewFrame(name, type, owner);
        if (frame is not null && _fdfChildren.TryGetValue(name, out var children))
            foreach (var (childType, childName) in children) NewFrame(childName, childType, frame);
        return frame;
    }

    // The game's own templates the facts saw create without a TOC; any other name is the map's FDF's or nil
    static readonly HashSet<string> GameTemplates = ["ScriptDialogButton", "EscMenuBackdrop", "EscMenuButtonTemplate", "EscMenuControlBackdropTemplate", "EscMenuPopupMenuTemplate", "EscMenuCheckBoxTemplate", "EscMenuEditBoxTemplate", "EscMenuSliderTemplate", "QuestButtonBaseTemplate"];

    /// <summary>A frame made from an FDF name the map defined (or a game template); nil for an unknown name, and for the Esc menu's text templates without a TOC (facts).</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); a frame made from a template has no size, as only the FDF's names and nested frames are read, where the game gave it the size its FDF sets.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public framehandle? BlzCreateFrame(string? name, framehandle? owner, int priority, int createContext) =>
        name is not null && (_fdfNames.Contains(name) || GameTemplates.Contains(name)) ? NewTemplateFrame(name, "FRAME", owner) : null;

    /// <summary>A simple frame made from an FDF name the map defined; nil for an unknown name, a game template included.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); only the map's FDF names make one, and no size is read from the FDF.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public framehandle? BlzCreateSimpleFrame(string? name, framehandle? owner, int createContext) =>
        name is not null && _fdfNames.Contains(name) ? NewTemplateFrame(name, "SIMPLEFRAME", owner) : null;

    /// <summary>A frame of the type, also with an unknown template to inherit (facts, 10-14).</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the inherited template is ignored, and the type's own default size is not modelled.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public framehandle? BlzCreateFrameByType(string? typeName, string? name, framehandle? owner, string? inherits, int createContext) =>
        typeName is null ? null : NewButtonParts(NewFrame(name ?? "", typeName, owner)!);

    // A button frame has its backdrops as children, the normal, pushed and disabled one (what a map's BlzFrameGetChild(button, 0) to 2 reaches; decisions D398)
    static readonly HashSet<string> ButtonTypes = ["GLUEBUTTON", "GLUETEXTBUTTON", "BUTTON", "TEXTBUTTON", "SCRIPT_DIALOG_BUTTON", "BROWSER_BUTTON", "INVIS_BUTTON"];

    framehandle NewButtonParts(framehandle frame)
    {
        if (ButtonTypes.Contains(frame.Type))
            for (var n = 0; n < 3; n++) NewFrame("", "BACKDROP", frame);
        return frame;
    }

    /// <summary>A frame by its FDF name; the nearest made one, nil for none.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game's own frame names are the common ones here, not all of them (D398).</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public framehandle? BlzGetFrameByName(string? name, int createContext)
    {
        if (_frames.LastOrDefault(frame => !frame.Destroyed && frame.Name == name) is { } made) return made;
        if (name is null || !(GameFrameNames.Contains(name) || _fdfNames.Contains(name))) return null;
        var frame = Make(new framehandle { Name = name, Type = "FRAME" });
        _frames.Add(frame);
        return frame;
    }

    // Frames of the game's own UI that exist from the start, found by name (decisions D398); the list is the common ones maps parent their own frames to
    static readonly HashSet<string> GameFrameNames = ["ConsoleUI", "ConsoleUIBackdrop", "UpperButtonBarFrame", "ResourceBarFrame", "InfoPanelIconBackdrop", "MiniMapFrame", "SimpleInventoryCover", "ChatFrame", "UnitMessageFrame", "TimeOfDayIndicator", "MinimapButtonBar", "ConsoleBottomBar", "ConsoleTopBar", "ConsoleUIBackdropGlue", "Leaderboard", "LeaderboardBackdrop", "LeaderboardTitle", "Multiboard", "MultiboardBackdrop", "MultiboardTitle"];

    /// <summary>A frame of the game's own UI, one for each type and index; it exists from the first ask (the facts: origin frames exist in main).</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); only origin frame type 0 has a size (0.8 by 0.6), and an index is never checked against the game's own counts.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public framehandle? BlzGetOriginFrame(originframetype? frameType, int index)
    {
        if (frameType is null) return null;
        if (_origins.TryGetValue((frameType.Id, index), out var known)) return known;
        var frame = _origins[(frameType.Id, index)] = Make(new framehandle { Name = $"Origin{frameType.Id}", Type = "FRAME", Width = frameType.Id == 0 ? ScreenWidth : 0, Height = frameType.Id == 0 ? ScreenHeight : 0 });
        _frames.Add(frame);
        return frame;
    }

    /// <summary>Changes nothing; the game's own UI frames stay as they are.</summary>
    /// <remarks>Recorded: Hides the game's own UI frames; no native reads that back.</remarks>
    [Native(Level.Recorded, Reason = "Hides the game's own UI frames; no native reads that back")]
    public void BlzHideOriginFrames(bool enable) { }

    /// <summary>Destroys the frame and its children (facts, 09-29).</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game left a simple status bar standing when its parent was destroyed (10-14); here every child goes with it.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public void BlzDestroyFrame(framehandle? frame)
    {
        if (frame is null || frame.Destroyed) return;
        frame.Destroyed = true;
        foreach (var child in frame.Children.ToArray()) BlzDestroyFrame(child);
        frame.Parent?.Children.Remove(frame);
    }

    /// <summary>The name the frame was made with, empty for one made by type with no name; nil for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); what the game reads for a frame made by type with no name is not measured.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public string? BlzFrameGetName(framehandle? frame) => frame?.Name;
    /// <summary>The frame it was made under, or the one it was moved to; nil for none.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game's own tree for its origin frames is not modelled, so they have no parent here.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public framehandle? BlzFrameGetParent(framehandle? frame) => frame?.Parent;
    /// <summary>How many children the frame has; 0 for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); counts only children made here (a button's three backdrops, an FDF's nested frames); the game's count for its own frames is not modelled.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public int BlzFrameGetChildrenCount(framehandle? frame) => frame?.Children.Count ?? 0;
    /// <summary>The child at the index, counted from 0; nil for nil or an index out of range.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); counts only children made here (a button's three backdrops, an FDF's nested frames); the game's own frames are not modelled.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public framehandle? BlzFrameGetChild(framehandle? frame, int index) => frame is not null && index >= 0 && index < frame.Children.Count ? frame.Children[index] : null;

    /// <summary>Moves the frame under the parent; nothing for nil or the frame itself.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game kept the frame tied to its old parent (a text moved out was hidden with its old backdrop, 11-03); here visibility follows the new parent only.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public void BlzFrameSetParent(framehandle? frame, framehandle? parent)
    {
        if (frame is null || parent is null || frame == parent) return;
        frame.Parent?.Children.Remove(frame);
        frame.Parent = parent;
        parent.Children.Add(frame);
    }

    /// <summary>Keeps the text for <c>BlzFrameGetText</c>; nil keeps an empty one.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the text is kept whole, whatever the text size limit; the game's cut by frame type is not measured.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetText(framehandle? frame, string? text) { if (frame is not null) frame.Text = text ?? ""; }
    /// <summary>Adds the text after the text kept, on a new line unless the frame had none.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the new line is the model's; the game's separator is not measured.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameAddText(framehandle? frame, string? text) { if (frame is not null) frame.Text += (frame.Text.Length > 0 ? "\n" : "") + (text ?? ""); }
    /// <summary>The text kept; nil for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); a frame never given text reads an empty string; what the game reads for its own frames is not measured.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public string? BlzFrameGetText(framehandle? frame) => frame?.Text;
    /// <summary>Keeps the limit for <c>BlzFrameGetTextSizeLimit</c>; it does not cut the text.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); it only reads back, and cuts no text; the game's own limit by frame type is not modelled.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetTextSizeLimit(framehandle? frame, int size) { if (frame is not null) frame.TextSizeLimit = size; }
    /// <summary>The limit kept, 255 for a new frame; 0 for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); a new frame reads 255; the game's own value is not measured.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public int BlzFrameGetTextSizeLimit(framehandle? frame) => frame?.TextSizeLimit ?? 0;
    /// <summary>Keeps the alpha, held to 0 to 255.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); kept per frame and never passed to children; the game fades a backdrop's children with it but not a simple frame (11-13), which is not modelled.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetAlpha(framehandle? frame, int alpha) { if (frame is not null) frame.Alpha = Math.Clamp(alpha, 0, 255); }
    /// <summary>The alpha kept, 255 for a new frame; 0 for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); a new frame reads 255; the game's own value is not measured.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public int BlzFrameGetAlpha(framehandle? frame) => frame?.Alpha ?? 0;
    /// <summary>Keeps the frame's own visible flag.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the frame's own flag only; the game's twelve command buttons showed again once a unit was selected (11-14), which is not modelled.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetVisible(framehandle? frame, bool visible) { if (frame is not null) frame.Visible = visible; }

    /// <summary>The frame's own flag and its parent's: a frame inside a hidden one is not visible.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); a simple frame owned by a hidden backdrop read true in the game (11-13), and here it reads false; a destroyed frame reads false, not measured.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public bool BlzFrameIsVisible(framehandle? frame)
    {
        for (var f = frame; f is not null; f = f.Parent)
            if (!f.Visible || f.Destroyed) return false;
        return frame is not null;
    }

    /// <summary>Keeps whether the frame is enabled.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); only BlzFrameClick looks at it; the game's other effects (a text turning grey, the hotkeys given back, 11-03) are not modelled.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetEnable(framehandle? frame, bool enabled) { if (frame is not null) frame.Enabled = enabled; }
    /// <summary>Whether the frame is enabled, true for a new one; false for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); a new frame reads true; the game's own value is not measured.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public bool BlzFrameGetEnable(framehandle? frame) => frame?.Enabled ?? false;
    /// <summary>Keeps the level; nothing reads it back.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game's drawing order is not modelled, and nothing reads the level back.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetLevel(framehandle? frame, int level) { if (frame is not null) frame.Level = level; }
    /// <summary>Keeps the scale; no size the getters read changes with it.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game's scale also grows the frame's children (11-13); here it changes no size the getters read.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetScale(framehandle? frame, float scale) { if (frame is not null) frame.Scale = scale; }
    /// <summary>Keeps the value for <c>BlzFrameGetValue</c>.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); kept as given; the game moves a value outside the range to the nearest end and fires the slider's event about 0.1 s later (11-06), and a check box read 0 (11-03), which are not modelled.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetValue(framehandle? frame, float value) { if (frame is not null) frame.Value = value; }
    /// <summary>The value kept, 0 for a new frame and for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the value is what was set, never moved into the range as the game does (11-06).</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public float BlzFrameGetValue(framehandle? frame) => frame?.Value ?? 0;
    /// <summary>Keeps the range; the value is not moved into it.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game moves a value outside a new range into it (11-06), which is not done here.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetMinMaxValue(framehandle? frame, float minValue, float maxValue) { if (frame is not null) (frame.Min, frame.Max) = (minValue, maxValue); }
    /// <summary>Keeps the step; nothing reads it back.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the step rounds no value here; the game's use of it is not measured.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetStepSize(framehandle? frame, float stepSize) { if (frame is not null) frame.Step = stepSize; }
    /// <summary>Keeps the tooltip frame; nothing reads it back.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game shows it on hover, and crashes when one frame is the tooltip of two (11-03), which is not modelled.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetTooltip(framehandle? frame, framehandle? tooltip) { if (frame is not null) frame.Tooltip = tooltip; }
    /// <summary>Sets the width and height the size reads give.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); a frame placed by points reads here only the size set; the game's anchoring is not modelled.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetSize(framehandle? frame, float width, float height) { if (frame is not null) (frame.Width, frame.Height) = (width, height); }

    /// <summary>Fills the other frame: the frame takes its size unless it was given one.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); only the other frame's size is taken, and only while the frame has none; its place is not kept.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public void BlzFrameSetAllPoints(framehandle? frame, framehandle? relative)
    {
        if (frame is not null) frame.Relative = relative;
    }

    /// <summary>Forgets the frame it fills, so an unsized frame reads 0 by 0.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); it only forgets the frame filled; the other points are not kept, so nothing else changes.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public void BlzFrameClearAllPoints(framehandle? frame)
    {
        if (frame is not null) frame.Relative = null;
    }

    /// <summary>The width set; with none set, the width of the frame it fills, else 0; 0 for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); a frame placed by points reads 0 unless sized or filling another; the game's width for those is not measured.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public float BlzFrameGetWidth(framehandle? frame) => frame is null ? 0 : frame.Width > 0 || frame.Relative is null ? frame.Width : BlzFrameGetWidth(frame.Relative);

    /// <summary>The height set; with none set, the height of the frame it fills, else 0; 0 for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); a frame placed by points reads 0 unless sized or filling another; the game's height for those is not measured.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public float BlzFrameGetHeight(framehandle? frame) => frame is null ? 0 : frame.Height > 0 || frame.Relative is null ? frame.Height : BlzFrameGetHeight(frame.Relative);

    /// <summary>The 4:3 area is 0.8 by 0.6 over 1080 pixels high.</summary>
    /// <remarks>Close: the window is taken as 1080 pixels high whatever its size, and the rounding (half to even) is not measured in the game.</remarks>
    [Native(Level.Close)] public int BlzFrameToPixelX(float frameX) => (int)MathF.Round(frameX / ScreenHeight * ClientHeight);
    /// <summary>The pixels a frame height <paramref name="frameY"/> covers on a 1080 pixel high window, rounded.</summary>
    /// <remarks>Close: the window is taken as 1080 pixels high whatever its size, and the rounding (half to even) is not measured in the game.</remarks>
    [Native(Level.Close)] public int BlzFrameToPixelY(float frameY) => (int)MathF.Round(frameY / ScreenHeight * ClientHeight);
    /// <summary>The frame width of <paramref name="pixelX"/> pixels on a 1080 pixel high window.</summary>
    /// <remarks>Close: the window is taken as 1080 pixels high whatever its size, not measured in the game.</remarks>
    [Native(Level.Close)] public float BlzPixelToFrameX(int pixelX) => pixelX * ScreenHeight / ClientHeight;
    /// <summary>The frame height of <paramref name="pixelY"/> pixels on a 1080 pixel high window.</summary>
    /// <remarks>Close: the window is taken as 1080 pixels high whatever its size, not measured in the game.</remarks>
    [Native(Level.Close)] public float BlzPixelToFrameY(int pixelY) => pixelY * ScreenHeight / ClientHeight;

    const string FrameDrawn = "How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back";

    /// <summary>Changes nothing; the place is not kept, and no size read changes.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetAbsPoint(framehandle? frame, framepointtype? point, float x, float y) { }
    /// <summary>Changes nothing; the place is not kept, and no size read changes.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetPoint(framehandle? frame, framepointtype? point, framehandle? relative, framepointtype? relativePoint, float x, float y) { }
    /// <summary>Changes nothing; no font is drawn.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetFont(framehandle? frame, string? fileName, float height, int flags) { }
    /// <summary>Changes nothing; no model is drawn.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetModel(framehandle? frame, string? modelFile, int cameraIndex) { }
    /// <summary>Changes nothing; no animation plays.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetSpriteAnimate(framehandle? frame, int primaryProp, int flags) { }
    /// <summary>Changes nothing; the alignment is not kept.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetTextAlignment(framehandle? frame, textaligntype? vert, textaligntype? horz) { }
    /// <summary>Changes nothing; the colour is not kept.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetTextColor(framehandle? frame, int color) { }
    /// <summary>Changes nothing; no texture is drawn.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetTexture(framehandle? frame, string? texFile, int flag, bool blend) { }
    /// <summary>Changes nothing; no tint is drawn.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetVertexColor(framehandle? frame, int color) { }
    /// <summary>Changes nothing; the focus is not kept.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetFocus(framehandle? frame, bool flag) { }
    /// <summary>Changes nothing; the mouse is not held in the frame.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameCageMouse(framehandle? frame, bool enable) { }
    /// <summary>Changes nothing; the text area does not scroll.</summary>
    /// <remarks>Recorded: How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzTextAreaFrameSetAutoScroll(framehandle? frame, bool value) { }

    // ---- Frame events: registered, and fired for BlzFrameClick (the other events need the mouse and keys: wave 6)

    /// <summary>Registers the trigger for the event of the frame.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); only FRAMEEVENT_CONTROL_CLICK is ever fired, for BlzFrameClick; the mouse and keys are not modelled, so the other events never fire.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public @event? BlzTriggerRegisterFrameEvent(trigger? whichTrigger, framehandle? frame, frameeventtype? eventId)
    {
        if (whichTrigger is null || frame is null || eventId is null) return null;
        var registered = Register(whichTrigger);
        registered.Of = frame;
        _frameEvents.Add((frame, eventId.Id, registered));
        return registered;
    }

    readonly List<(framehandle Frame, int Type, @event Event)> _frameEvents = [];

    /// <summary>A click on the frame: its FRAMEEVENT_CONTROL_CLICK registrations fire, the frame the trigger frame; no input of a test clicks a frame, so this is the way to.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game's click also fires the player's mouse down and up (11-05) and keeps the keyboard focus on the button (09-29), which are not modelled.</remarks>
    [Native(Level.Close, Fact = FrameFacts)]
    public void BlzFrameClick(framehandle? frame)
    {
        if (frame is null || !frame.Enabled || frame.Destroyed) return;
        FireFrame(frame, 1, "", frame.Value);
    }

    void FireFrame(framehandle frame, int type, string text, float value)
    {
        var outer = _frameEvent;
        _frameEvent = (frame, type, text, value);
        try
        {
            foreach (var (registeredFrame, registeredType, registered) in _frameEvents.ToArray())
                if (registeredFrame == frame && registeredType == type) Fire(registered, Convert<dialogevent>(1000 + type));
        }
        finally
        {
            _frameEvent = outer;
        }
    }

    /// <summary>The frame of the frame event that is running; nil outside one.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); only a click raises a frame event here.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public framehandle? BlzGetTriggerFrame() => _frameEvent?.Frame;
    /// <summary>The type of the frame event that is running; nil outside one.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); only FRAMEEVENT_CONTROL_CLICK is raised here.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public frameeventtype? BlzGetTriggerFrameEvent() => _frameEvent is { } e ? Convert<frameeventtype>(e.Type) : null;
    /// <summary>The text the running frame event carries, always empty here; empty outside one too.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game gives an edit box's whole text in its events (11-03); none of those fire here.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public string? BlzGetTriggerFrameText() => _frameEvent?.Text ?? "";
    /// <summary>The value the running frame event carries, the frame's value for a click; 0 outside one.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: frames (09-29 to 11-14)); the game sends 120 or -120 for the wheel and the slider's value (11-03); only a click fires here.</remarks>
    [Native(Level.Close, Fact = FrameFacts)] public float BlzGetTriggerFrameValue() => _frameEvent?.Value ?? 0;
}
