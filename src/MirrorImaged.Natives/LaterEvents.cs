namespace MirrorImaged.Natives;

// The event responses and registrations of events whose sources come with later waves (spells, training, building, selling,
// summoning, attacks: waves 3 and 4; the keyboard, mouse and chat: wave 6): the registrations answer an event that never fires
// and the responses read nil or 0 outside one, as they do outside any event. Sync data is sent to the local player's
// registrations a step later.
public sealed partial class GameNatives
{
    // ---- Spells and the orders that come with them (wave 4)


    [Native(Level.Close)]
    public @event? TriggerRegisterCommandEvent(trigger? whichTrigger, int whichAbility, string? order) => whichTrigger is null ? null : Register(whichTrigger);

    [Native(Level.Close)]
    public @event? TriggerRegisterUpgradeCommandEvent(trigger? whichTrigger, int whichUpgrade) => whichTrigger is null ? null : Register(whichTrigger);

    // ---- Combat, buildings, training, shops and summons (wave 3)

    const string NoAbsorbing = "No item is absorbed here, as none was in the game (facts, Items: stacking was never seen, and what makes items stack isn't known), so every read is outside one: nil and false (probe natives-test-run)";

    [Native(Level.Close, Fact = TestRunProbe, Reason = NoAbsorbing)] public item? BlzGetAbsorbingItem() => null;
    [Native(Level.Close, Fact = TestRunProbe, Reason = NoAbsorbing)] public bool BlzGetManipulatedItemWasAbsorbed() => false;

    // Close: a unit whose auras are off gives none; the buffs it gave end with their own time (probe units-resets). The flag that sets what the
    // aura's buff shows to the player is drawn only.
    [Native(Level.Close, Fact = "probes/expected/units-resets.txt: aura switch")]
    public void BlzUnitEnableAuras(unit? whichUnit, bool enable, bool affectsUI)
    {
        if (whichUnit is not null) whichUnit.AurasOff = !enable;
    }

    [Native(Level.Close, Fact = TestRunProbe, Reason = "EVENT_GAME_VICTORY never fired in the game (probe events: RemovePlayer's victory, defeat and tie of a computer and of the user), so every read is outside one: nil")]
    public player? GetWinningPlayer() => null;

    // ---- Chat, keys, the mouse and trackables (wave 6)

    [Native(Level.Close)]
    public @event? TriggerRegisterTrackableHitEvent(trigger? whichTrigger, trackable? t) => whichTrigger is null || t is null ? null : Register(whichTrigger);

    [Native(Level.Close)]
    public @event? TriggerRegisterTrackableTrackEvent(trigger? whichTrigger, trackable? t) => whichTrigger is null || t is null ? null : Register(whichTrigger);

    [Native(Level.Close, Reason = "Nil, as outside an event: no trackable event fires (see CreateTrackable, D795)")]
    public trackable? GetTriggeringTrackable() => null;

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
