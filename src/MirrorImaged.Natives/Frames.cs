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
    [Native(Level.Close, Fact = FrameFacts)]
    public bool BlzLoadTOCFile(string? TOCFile)
    {
        if (TOCFile is null || Data.Map?.File(TOCFile) is not { } toc) return false;
        foreach (var line in System.Text.Encoding.UTF8.GetString(toc).Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            if (Data.Map?.File(line) is { } fdf)
                foreach (Match frame in Regex.Matches(System.Text.Encoding.UTF8.GetString(fdf), "(?m)^\\s*(?:Frame|Texture|String|Layer)\\s+\"[A-Z_]+\"\\s+\"([^\"]+)\""))
                    _fdfNames.Add(frame.Groups[1].Value);
        return true;
    }

    // The game's own templates the facts saw create without a TOC; any other name is the map's FDF's or nil
    static readonly HashSet<string> GameTemplates = ["ScriptDialogButton", "EscMenuBackdrop", "EscMenuButtonTemplate", "EscMenuControlBackdropTemplate", "EscMenuPopupMenuTemplate", "EscMenuCheckBoxTemplate", "EscMenuEditBoxTemplate", "EscMenuSliderTemplate", "QuestButtonBaseTemplate"];

    /// <summary>A frame made from an FDF name the map defined (or a game template); nil for an unknown name, and for the Esc menu's text templates without a TOC (facts).</summary>
    [Native(Level.Close, Fact = FrameFacts)]
    public framehandle? BlzCreateFrame(string? name, framehandle? owner, int priority, int createContext) =>
        name is not null && (_fdfNames.Contains(name) || GameTemplates.Contains(name)) ? NewFrame(name, "FRAME", owner) : null;

    [Native(Level.Close, Fact = FrameFacts)]
    public framehandle? BlzCreateSimpleFrame(string? name, framehandle? owner, int createContext) =>
        name is not null && _fdfNames.Contains(name) ? NewFrame(name, "SIMPLEFRAME", owner) : null;

    /// <summary>A frame of the type, also with an unknown template to inherit (facts, 10-14).</summary>
    [Native(Level.Close, Fact = FrameFacts)]
    public framehandle? BlzCreateFrameByType(string? typeName, string? name, framehandle? owner, string? inherits, int createContext) =>
        typeName is null ? null : NewFrame(name ?? "", typeName, owner);

    /// <summary>A frame by its FDF name; the nearest made one, nil for none.</summary>
    [Native(Level.Close, Fact = FrameFacts)]
    public framehandle? BlzGetFrameByName(string? name, int createContext) => _frames.LastOrDefault(frame => !frame.Destroyed && frame.Name == name);

    /// <summary>A frame of the game's own UI, one for each type and index; it exists from the first ask (the facts: origin frames exist in main).</summary>
    [Native(Level.Close, Fact = FrameFacts)]
    public framehandle? BlzGetOriginFrame(originframetype? frameType, int index)
    {
        if (frameType is null) return null;
        if (_origins.TryGetValue((frameType.Id, index), out var known)) return known;
        var frame = _origins[(frameType.Id, index)] = Make(new framehandle { Name = $"Origin{frameType.Id}", Type = "FRAME", Width = frameType.Id == 0 ? ScreenWidth : 0, Height = frameType.Id == 0 ? ScreenHeight : 0 });
        _frames.Add(frame);
        return frame;
    }

    [Native(Level.Recorded, Reason = "Hides the game's own UI frames; no native reads that back")]
    public void BlzHideOriginFrames(bool enable) { }

    /// <summary>Destroys the frame and its children (facts, 09-29).</summary>
    [Native(Level.Close, Fact = FrameFacts)]
    public void BlzDestroyFrame(framehandle? frame)
    {
        if (frame is null || frame.Destroyed) return;
        frame.Destroyed = true;
        foreach (var child in frame.Children.ToArray()) BlzDestroyFrame(child);
        frame.Parent?.Children.Remove(frame);
    }

    [Native(Level.Close, Fact = FrameFacts)] public string? BlzFrameGetName(framehandle? frame) => frame?.Name;
    [Native(Level.Close, Fact = FrameFacts)] public framehandle? BlzFrameGetParent(framehandle? frame) => frame?.Parent;
    [Native(Level.Close, Fact = FrameFacts)] public int BlzFrameGetChildrenCount(framehandle? frame) => frame?.Children.Count ?? 0;
    [Native(Level.Close, Fact = FrameFacts)] public framehandle? BlzFrameGetChild(framehandle? frame, int index) => frame is not null && index >= 0 && index < frame.Children.Count ? frame.Children[index] : null;

    [Native(Level.Close, Fact = FrameFacts)]
    public void BlzFrameSetParent(framehandle? frame, framehandle? parent)
    {
        if (frame is null || parent is null || frame == parent) return;
        frame.Parent?.Children.Remove(frame);
        frame.Parent = parent;
        parent.Children.Add(frame);
    }

    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetText(framehandle? frame, string? text) { if (frame is not null) frame.Text = text ?? ""; }
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameAddText(framehandle? frame, string? text) { if (frame is not null) frame.Text += (frame.Text.Length > 0 ? "\n" : "") + (text ?? ""); }
    [Native(Level.Close, Fact = FrameFacts)] public string? BlzFrameGetText(framehandle? frame) => frame?.Text;
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetTextSizeLimit(framehandle? frame, int size) { if (frame is not null) frame.TextSizeLimit = size; }
    [Native(Level.Close, Fact = FrameFacts)] public int BlzFrameGetTextSizeLimit(framehandle? frame) => frame?.TextSizeLimit ?? 0;
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetAlpha(framehandle? frame, int alpha) { if (frame is not null) frame.Alpha = Math.Clamp(alpha, 0, 255); }
    [Native(Level.Close, Fact = FrameFacts)] public int BlzFrameGetAlpha(framehandle? frame) => frame?.Alpha ?? 0;
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetVisible(framehandle? frame, bool visible) { if (frame is not null) frame.Visible = visible; }

    /// <summary>The frame's own flag and its parent's: a frame inside a hidden one is not visible.</summary>
    [Native(Level.Close, Fact = FrameFacts)]
    public bool BlzFrameIsVisible(framehandle? frame)
    {
        for (var f = frame; f is not null; f = f.Parent)
            if (!f.Visible || f.Destroyed) return false;
        return frame is not null;
    }

    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetEnable(framehandle? frame, bool enabled) { if (frame is not null) frame.Enabled = enabled; }
    [Native(Level.Close, Fact = FrameFacts)] public bool BlzFrameGetEnable(framehandle? frame) => frame?.Enabled ?? false;
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetLevel(framehandle? frame, int level) { if (frame is not null) frame.Level = level; }
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetScale(framehandle? frame, float scale) { if (frame is not null) frame.Scale = scale; }
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetValue(framehandle? frame, float value) { if (frame is not null) frame.Value = value; }
    [Native(Level.Close, Fact = FrameFacts)] public float BlzFrameGetValue(framehandle? frame) => frame?.Value ?? 0;
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetMinMaxValue(framehandle? frame, float minValue, float maxValue) { if (frame is not null) (frame.Min, frame.Max) = (minValue, maxValue); }
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetStepSize(framehandle? frame, float stepSize) { if (frame is not null) frame.Step = stepSize; }
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetTooltip(framehandle? frame, framehandle? tooltip) { if (frame is not null) frame.Tooltip = tooltip; }
    [Native(Level.Close, Fact = FrameFacts)] public void BlzFrameSetSize(framehandle? frame, float width, float height) { if (frame is not null) (frame.Width, frame.Height) = (width, height); }

    /// <summary>Fills the other frame: the frame takes its size unless it was given one.</summary>
    [Native(Level.Close, Fact = FrameFacts)]
    public void BlzFrameSetAllPoints(framehandle? frame, framehandle? relative)
    {
        if (frame is not null) frame.Relative = relative;
    }

    [Native(Level.Close, Fact = FrameFacts)]
    public void BlzFrameClearAllPoints(framehandle? frame)
    {
        if (frame is not null) frame.Relative = null;
    }

    [Native(Level.Close, Fact = FrameFacts)]
    public float BlzFrameGetWidth(framehandle? frame) => frame is null ? 0 : frame.Width > 0 || frame.Relative is null ? frame.Width : BlzFrameGetWidth(frame.Relative);

    [Native(Level.Close, Fact = FrameFacts)]
    public float BlzFrameGetHeight(framehandle? frame) => frame is null ? 0 : frame.Height > 0 || frame.Relative is null ? frame.Height : BlzFrameGetHeight(frame.Relative);

    /// <summary>The 4:3 area is 0.8 by 0.6 over 1080 pixels high.</summary>
    [Native(Level.Close)] public int BlzFrameToPixelX(float frameX) => (int)MathF.Round(frameX / ScreenHeight * ClientHeight);
    [Native(Level.Close)] public int BlzFrameToPixelY(float frameY) => (int)MathF.Round(frameY / ScreenHeight * ClientHeight);
    [Native(Level.Close)] public float BlzPixelToFrameX(int pixelX) => pixelX * ScreenHeight / ClientHeight;
    [Native(Level.Close)] public float BlzPixelToFrameY(int pixelY) => pixelY * ScreenHeight / ClientHeight;

    const string FrameDrawn = "How the frame is drawn or placed (texture, model, font, colour, anchors, focus); no native reads it back";

    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetAbsPoint(framehandle? frame, framepointtype? point, float x, float y) { }
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetPoint(framehandle? frame, framepointtype? point, framehandle? relative, framepointtype? relativePoint, float x, float y) { }
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetFont(framehandle? frame, string? fileName, float height, int flags) { }
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetModel(framehandle? frame, string? modelFile, int cameraIndex) { }
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetSpriteAnimate(framehandle? frame, int primaryProp, int flags) { }
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetTextAlignment(framehandle? frame, textaligntype? vert, textaligntype? horz) { }
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetTextColor(framehandle? frame, int color) { }
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetTexture(framehandle? frame, string? texFile, int flag, bool blend) { }
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetVertexColor(framehandle? frame, int color) { }
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameSetFocus(framehandle? frame, bool flag) { }
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzFrameCageMouse(framehandle? frame, bool enable) { }
    [Native(Level.Recorded, Reason = FrameDrawn)] public void BlzTextAreaFrameSetAutoScroll(framehandle? frame, bool value) { }

    // ---- Frame events: registered, and fired for BlzFrameClick (the other events need the mouse and keys: wave 6)

    /// <summary>Registers the trigger for the event of the frame.</summary>
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

    /// <summary>A click on the frame: its FRAMEEVENT_CONTROL_CLICK registrations fire, the frame the trigger frame (the player's own click is wave 6's).</summary>
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

    [Native(Level.Close, Fact = FrameFacts)] public framehandle? BlzGetTriggerFrame() => _frameEvent?.Frame;
    [Native(Level.Close, Fact = FrameFacts)] public frameeventtype? BlzGetTriggerFrameEvent() => _frameEvent is { } e ? Convert<frameeventtype>(e.Type) : null;
    [Native(Level.Close, Fact = FrameFacts)] public string? BlzGetTriggerFrameText() => _frameEvent?.Text ?? "";
    [Native(Level.Close, Fact = FrameFacts)] public float BlzGetTriggerFrameValue() => _frameEvent?.Value ?? 0;
}
