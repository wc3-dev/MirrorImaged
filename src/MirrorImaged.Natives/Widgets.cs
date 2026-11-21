namespace MirrorImaged.Natives;

// The natives every widget takes: units (W2-2, probe units), destructables (W2-4, probe destructables) and items (W2-3,
// probe items).
public sealed partial class GameNatives
{
    /// <summary>A unit's or a destructable's life; 0 for nil.</summary>
    /// <remarks>Close: Not measured as a widget call; each kind reads as its own life does.</remarks>
    [Native(Level.Close)]
    public float GetWidgetLife(widget? whichWidget) => whichWidget switch
    {
        unit u => u.Life,
        destructable d => d.Life,
        item i => LifeOf(i),
        _ => 0f,
    };

    /// <summary>
    /// A unit's as its life is set (<c>SetLife</c>: below 0.405 it dies; facts, Units: SetWidgetLife clamps and 0.3, 0
    /// and -5 kill), a destructable's as SetDestructableLife (probe destructables).
    /// </summary>
    /// <remarks>Close: Not measured as a widget call; each kind is set as its own life is.</remarks>
    [Native(Level.Close)]
    public void SetWidgetLife(widget? whichWidget, float newLife)
    {
        if (whichWidget is unit u) SetLife(u, newLife);
        else if (whichWidget is destructable d) SetDestructableLife(d, newLife);
        else if (whichWidget is item i) SetItemLife(i, newLife);
    }

    /// <summary>The x of a unit, a destructable or an item; 0 for nil or another kind.</summary>
    /// <remarks>Close: Not measured as a widget call; each kind reads as its own x does.</remarks>
    [Native(Level.Close)]
    public float GetWidgetX(widget? whichWidget) => whichWidget switch
    {
        unit u => u.X,
        destructable d => d.X,
        item i => GetItemX(i),
        _ => 0f,
    };

    /// <summary>The y of a unit, a destructable or an item; 0 for nil or another kind.</summary>
    /// <remarks>Close: Not measured as a widget call; each kind reads as its own y does.</remarks>
    [Native(Level.Close)]
    public float GetWidgetY(widget? whichWidget) => whichWidget switch
    {
        unit u => u.Y,
        destructable d => d.Y,
        item i => GetItemY(i),
        _ => 0f,
    };

    /// <summary>
    /// Fires inside the widget's death: a unit's before its player-unit and unit events (probe units); a destructable's
    /// inside KillDestructable, a set of its life below 0.405 and RemoveDestructable (probe destructables).
    /// </summary>
    /// <remarks>Close: Modelled on the probes units and destructables; not measured as a registration on a widget.</remarks>
    [Native(Level.Close)]
    public @event? TriggerRegisterDeathEvent(trigger? whichTrigger, widget? whichWidget) =>
        RegisterUnit(whichTrigger, whichWidget, null, Convert<widgetevent>(WidgetDeath));

    /// <summary>
    /// The widget of a widget death event, the unit or destructable seen as a widget (Lua's == holds with it); nil in a
    /// unit's other events and outside one.
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Units: probe units (11-08); facts, Destructables: probe destructables (11-08)).</remarks>
    [Native(Level.Exact, Fact = UnitsProbe + "; " + Destructables)]
    public widget? GetTriggerWidget() => (widget?)Current.Destructable ?? Current.Item ?? (widget?)Response(WidgetDeath)?.Unit;
}
