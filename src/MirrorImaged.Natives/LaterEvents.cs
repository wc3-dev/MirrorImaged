namespace MirrorImaged.Natives;

// The event responses and registrations of events whose sources come with later waves (spells, training, building, selling,
// summoning, attacks: waves 3 and 4; the keyboard, mouse and chat: wave 6): the registrations answer an event that never fires
// and the responses read nil or 0 outside one, as they do outside any event. Sync data is sent to the local player's
// registrations a step later.
public sealed partial class GameNatives
{
    // ---- Spells and the orders that come with them (wave 4)


    /// <summary>Using an item on a place or target needs its ability's cast (wave 4): refused.</summary>
    [Native(Level.Close)] public bool UnitUseItemPoint(unit? whichUnit, item? whichItem, float x, float y) => false;
    [Native(Level.Close)] public bool UnitUseItemTarget(unit? whichUnit, item? whichItem, widget? target) => false;

    [Native(Level.Close)]
    public @event? TriggerRegisterCommandEvent(trigger? whichTrigger, int whichAbility, string? order) => whichTrigger is null ? null : Register(whichTrigger);

    [Native(Level.Close)]
    public @event? TriggerRegisterUpgradeCommandEvent(trigger? whichTrigger, int whichUpgrade) => whichTrigger is null ? null : Register(whichTrigger);

    // ---- Combat, buildings, training, shops and summons (wave 3)

    [Native(Level.Close)] public unit? GetBuyingUnit() => null;
    [Native(Level.Close)] public unit? GetSellingUnit() => null;
    [Native(Level.Close)] public item? GetSoldItem() => null;
    [Native(Level.Close)] public unit? GetSoldUnit() => null;
    [Native(Level.Close)] public unit? GetSummonedUnit() => null;
    [Native(Level.Close)] public unit? GetSummoningUnit() => null;
    [Native(Level.Close)] public item? BlzGetAbsorbingItem() => null;
    [Native(Level.Close)] public bool BlzGetManipulatedItemWasAbsorbed() => false;

    [Native(Level.Recorded, Reason = "Auras are drawn and counted by abilities, which come with wave 4; nothing reads the switch back")]
    public void BlzUnitEnableAuras(unit? whichUnit, bool enable, bool affectsUI) { }

    /// <summary>No run fires the victory event with a winner (docs/tasks/later.md, wave 6): nil.</summary>
    [Native(Level.Close)] public player? GetWinningPlayer() => null;

    // ---- Chat, keys, the mouse and trackables (wave 6)

    [Native(Level.Close)]
    public @event? TriggerRegisterTrackableHitEvent(trigger? whichTrigger, trackable? t) => whichTrigger is null || t is null ? null : Register(whichTrigger);

    [Native(Level.Close)]
    public @event? TriggerRegisterTrackableTrackEvent(trigger? whichTrigger, trackable? t) => whichTrigger is null || t is null ? null : Register(whichTrigger);

    [Native(Level.Close)] public trackable? GetTriggeringTrackable() => null;
    [Native(Level.Close)] public bool BlzGetTriggerPlayerIsKeyDown() => false;
    [Native(Level.Close)] public oskeytype? BlzGetTriggerPlayerKey() => null;
    [Native(Level.Close)] public int BlzGetTriggerPlayerMetaKey() => 0;
    [Native(Level.Close)] public mousebuttontype? BlzGetTriggerPlayerMouseButton() => null;
    [Native(Level.Close)] public location? BlzGetTriggerPlayerMousePosition() => null;
    [Native(Level.Close)] public float BlzGetTriggerPlayerMouseX() => 0;
    [Native(Level.Close)] public float BlzGetTriggerPlayerMouseY() => 0;
    [Native(Level.Close)] public bool BlzIsKeyPressed(oskeytype? key) => false;
    [Native(Level.Close)] public bool BlzIsMetaKeyPressed(int metakey) => false;
    [Native(Level.Close)] public bool BlzIsMouseButtonPressed(mousebuttontype? mouseButtonType) => false;
    [Native(Level.Close)] public int BlzGetMouseScreenPosX() => 0;
    [Native(Level.Close)] public int BlzGetMouseScreenPosY() => 0;

    [Native(Level.Close)]
    public @event? BlzTriggerRegisterPlayerKeyEvent(trigger? whichTrigger, player? whichPlayer, oskeytype? key, int metaKey, bool keyDown) =>
        whichTrigger is null || whichPlayer is null ? null : Register(whichTrigger);

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
