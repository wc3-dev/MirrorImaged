namespace MirrorImaged.Natives;

// The event responses and registrations of events whose sources come with later waves (spells, training, building, selling,
// summoning, attacks: waves 3 and 4; the keyboard, mouse and chat: wave 6): the registrations answer an event that never fires
// and the responses read nil or 0 outside one, as they do outside any event. Sync data is sent to the local player's
// registrations a step later.
public sealed partial class GameNatives
{
    // ---- Spells and the orders that come with them (wave 4)


    const string ItemUseFacts = "probe items-use-at (11-21, D794)";

    [Native(Level.Close, Fact = ItemUseFacts, Reason = "Not modelled: refused, no order. The game answered false in all of 20 calls, but gave the order of the item's slot (852008 for the first) as a point order for an item whose ability takes a place (a Wand of Negation, a Staff of Silence), cast its ability, firing the cast events and the use event and spending a charge, at once for a place in range and after the walk for one out of it; nothing for another item")]
    public bool UnitUseItemPoint(unit? whichUnit, item? whichItem, float x, float y) => false;

    [Native(Level.Close, Fact = ItemUseFacts, Reason = "Not modelled: refused, no order. The game answered true and gave the slot's order as a target order for a target the item's ability takes (a Wand of Lightning Shield on another unit, near, far or an enemy; a Scroll of Town Portal on a building), casting at once or after the walk, and false, with no order, for one it doesn't (the hero itself, a footman for Town Portal) and for an item whose ability takes none")]
    public bool UnitUseItemTarget(unit? whichUnit, item? whichItem, widget? target) => false;

    [Native(Level.Close)]
    public @event? TriggerRegisterCommandEvent(trigger? whichTrigger, int whichAbility, string? order) => whichTrigger is null ? null : Register(whichTrigger);

    [Native(Level.Close)]
    public @event? TriggerRegisterUpgradeCommandEvent(trigger? whichTrigger, int whichUpgrade) => whichTrigger is null ? null : Register(whichTrigger);

    // ---- Combat, buildings, training, shops and summons (wave 3)

    const string NoAbsorbing = "No item is absorbed here, as none was in the game (facts, Items: stacking was never seen, and what makes items stack isn't known), so every read is outside one: nil and false (probe natives-test-run)";

    [Native(Level.Close, Fact = TestRunProbe, Reason = NoAbsorbing)] public item? BlzGetAbsorbingItem() => null;
    [Native(Level.Close, Fact = TestRunProbe, Reason = NoAbsorbing)] public bool BlzGetManipulatedItemWasAbsorbed() => false;

    [Native(Level.Recorded, Reason = "Auras are drawn and counted by abilities, which come with wave 4; nothing reads the switch back")]
    public void BlzUnitEnableAuras(unit? whichUnit, bool enable, bool affectsUI) { }

    [Native(Level.Close, Fact = TestRunProbe, Reason = "EVENT_GAME_VICTORY never fired in the game (probe events: RemovePlayer's victory, defeat and tie of a computer and of the user), so every read is outside one: nil")]
    public player? GetWinningPlayer() => null;

    // ---- Chat, keys, the mouse and trackables (wave 6)

    [Native(Level.Close)]
    public @event? TriggerRegisterTrackableHitEvent(trigger? whichTrigger, trackable? t) => whichTrigger is null || t is null ? null : Register(whichTrigger);

    [Native(Level.Close)]
    public @event? TriggerRegisterTrackableTrackEvent(trigger? whichTrigger, trackable? t) => whichTrigger is null || t is null ? null : Register(whichTrigger);

    [Native(Level.Close)] public trackable? GetTriggeringTrackable() => null;

    // ---- Sync: data sent by the local player reaches the registrations for that prefix a step later (EVENT_PLAYER_SYNC_DATA)

    const int SyncEventId = 2000;
    readonly List<(player Player, string Prefix, @event Event)> _syncEvents = [];
    (string Prefix, string Data)? _sync;

    [Native(Level.Close)]
    public @event? BlzTriggerRegisterPlayerSyncEvent(trigger? whichTrigger, player? whichPlayer, string? prefix, bool fromServer)
    {
        if (whichTrigger is null || whichPlayer is null || prefix is null) return null;
        var registered = Register(whichTrigger);
        _syncEvents.Add((whichPlayer, prefix, registered));
        return registered;
    }

    /// <summary>True for data within the game's limit of 255 characters (prefix and data together); it arrives a step later.</summary>
    [Native(Level.Close)]
    public bool BlzSendSyncData(string? prefix, string? data)
    {
        if (prefix is null || data is null || _local is null || prefix.Length + data.Length > 255) return false;
        var sender = _local;
        _game.At(_game.Step + 1, () =>
        {
            var outer = _sync;
            _sync = (prefix, data);
            try
            {
                foreach (var (player, registeredPrefix, registered) in _syncEvents.ToArray())
                    if (player == sender && registeredPrefix == prefix) Fire(registered, Convert<dialogevent>(SyncEventId), player: sender);
            }
            finally
            {
                _sync = outer;
            }
        });
        return true;
    }

    [Native(Level.Close)] public string? BlzGetTriggerSyncData() => _sync?.Data ?? "";
    [Native(Level.Close)] public string? BlzGetTriggerSyncPrefix() => _sync?.Prefix ?? "";

    [Native(Level.Recorded, Reason = "The sync of a trigger's data between machines; a run has one")] public void TriggerSyncReady() { }
    [Native(Level.Recorded, Reason = "The sync of a trigger's data between machines; a run has one")] public void TriggerSyncStart() { }
}
