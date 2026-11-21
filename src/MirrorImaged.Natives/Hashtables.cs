namespace MirrorImaged.Natives;

public partial class hashtable
{
    // Under each parent key, each child key holds an integer, a real, a boolean, a string and a handle apart.
    internal readonly Dictionary<int, Dictionary<(int Child, Slot Slot), object>> Parents = [];
}

enum Slot { Integer, Real, Boolean, String, Handle }

// The hashtable natives as the probes hashtables and strings-2 measured them (facts, "Strings, numbers and
// hashtables"): what a key holds of one kind leaves the others alone; a missing value loads as 0, 0.0, false, "" or
// null; at most 256 hashtables at once, and FlushParentHashtable destroys one, which gives its place back.
public sealed partial class GameNatives
{
    const string TablesFact = "facts, Strings, numbers and hashtables (probes hashtables, strings-2)";
    int _hashtables;

    /// <summary>A new hashtable; null when 256 are in use.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public hashtable? InitHashtable()
    {
        if (_hashtables == 256) return null;
        _hashtables++;
        return Make(new hashtable());
    }

    bool Alive(handle h) => _game.Handle(h.Key) == h;

    void Save(hashtable? table, int parentKey, int childKey, Slot slot, object value)
    {
        if (table is null || !Alive(table)) return;
        if (!table.Parents.TryGetValue(parentKey, out var children)) table.Parents.Add(parentKey, children = []);
        children[(childKey, slot)] = value;
    }

    object? Load(hashtable? table, int parentKey, int childKey, Slot slot) =>
        table is not null && table.Parents.TryGetValue(parentKey, out var children) ? children.GetValueOrDefault((childKey, slot)) : null;

    bool Have(hashtable? table, int parentKey, int childKey, Slot slot) =>
        table is not null && table.Parents.TryGetValue(parentKey, out var children) && children.ContainsKey((childKey, slot));

    void Remove(hashtable? table, int parentKey, int childKey, Slot slot)
    {
        if (table is not null && table.Parents.TryGetValue(parentKey, out var children)) children.Remove((childKey, slot));
    }

    // A null or destroyed table or handle saves nothing and gives false; a Convert native's value is saved and gives
    // false too.
    bool SaveHandle(hashtable? table, int parentKey, int childKey, handle? value)
    {
        if (table is null || !Alive(table) || value is null || !Alive(value)) return false;
        Save(table, parentKey, childKey, Slot.Handle, value);
        return !(_converted.TryGetValue((value.GetType(), value.Id), out var converted) && converted == value);
    }

    // A handle of another type, or one destroyed since, loads as null.
    T? LoadHandle<T>(hashtable? table, int parentKey, int childKey) where T : handle =>
        Load(table, parentKey, childKey, Slot.Handle) is T h && Alive(h) ? h : null;

    /// <summary>Keeps the integer under the two keys; nothing is kept with a null or destroyed hashtable.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public void SaveInteger(hashtable? table, int parentKey, int childKey, int value) => Save(table, parentKey, childKey, Slot.Integer, value);

    /// <summary>Keeps the real under the two keys; nothing is kept with a null or destroyed hashtable.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public void SaveReal(hashtable? table, int parentKey, int childKey, float value) => Save(table, parentKey, childKey, Slot.Real, value);

    /// <summary>Keeps the boolean under the two keys; nothing is kept with a null or destroyed hashtable.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public void SaveBoolean(hashtable? table, int parentKey, int childKey, bool value) => Save(table, parentKey, childKey, Slot.Boolean, value);

    // A null string saved, which JASS can give and which loads as null (probe jass-threads); Lua refuses a nil one
    static readonly object NullString = new();

    /// <summary>Saves the string, null too, and gives true; false, saving nothing, with no table or a destroyed one.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public bool SaveStr(hashtable? table, int parentKey, int childKey, string? value)
    {
        if (table is null || !Alive(table)) return false;
        Save(table, parentKey, childKey, Slot.String, value ?? NullString);
        return true;
    }

    /// <summary>The integer kept under the two keys; 0 when none is.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public int LoadInteger(hashtable? table, int parentKey, int childKey) => Load(table, parentKey, childKey, Slot.Integer) as int? ?? 0;

    /// <summary>The real kept under the two keys; 0.0 when none is.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public float LoadReal(hashtable? table, int parentKey, int childKey) => Load(table, parentKey, childKey, Slot.Real) as float? ?? 0;

    /// <summary>The boolean kept under the two keys; false when none is.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public bool LoadBoolean(hashtable? table, int parentKey, int childKey) => Load(table, parentKey, childKey, Slot.Boolean) as bool? ?? false;

    /// <summary>The string saved; a key never saved gives "" in Lua and null in JASS (probe jass-threads), as does a saved null.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public string? LoadStr(hashtable? table, int parentKey, int childKey) => Load(table, parentKey, childKey, Slot.String) switch
    {
        string s => s,
        null when !Jass => "",
        _ => null,
    };

    /// <summary>Whether a integer is kept under the two keys; the other types under them don't count.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public bool HaveSavedInteger(hashtable? table, int parentKey, int childKey) => Have(table, parentKey, childKey, Slot.Integer);

    /// <summary>Whether a real is kept under the two keys; the other types under them don't count.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public bool HaveSavedReal(hashtable? table, int parentKey, int childKey) => Have(table, parentKey, childKey, Slot.Real);

    /// <summary>Whether a boolean is kept under the two keys; the other types under them don't count.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public bool HaveSavedBoolean(hashtable? table, int parentKey, int childKey) => Have(table, parentKey, childKey, Slot.Boolean);

    /// <summary>Whether a string is kept under the two keys; the other types under them don't count.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public bool HaveSavedString(hashtable? table, int parentKey, int childKey) => Have(table, parentKey, childKey, Slot.String);

    /// <summary>Whether a handle is kept under the two keys; the other types under them don't count.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public bool HaveSavedHandle(hashtable? table, int parentKey, int childKey) => Have(table, parentKey, childKey, Slot.Handle);

    /// <summary>Forgets the integer under the two keys and leaves the other types under them; nothing happens with a null hashtable.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public void RemoveSavedInteger(hashtable? table, int parentKey, int childKey) => Remove(table, parentKey, childKey, Slot.Integer);

    /// <summary>Forgets the real under the two keys and leaves the other types under them; nothing happens with a null hashtable.</summary>
    /// <remarks>Close: Modelled on RemoveSavedInteger; removing a real isn't measured.</remarks>
    [Native(Level.Close)]
    public void RemoveSavedReal(hashtable? table, int parentKey, int childKey) => Remove(table, parentKey, childKey, Slot.Real);

    /// <summary>Forgets the boolean under the two keys and leaves the other types under them; nothing happens with a null hashtable.</summary>
    /// <remarks>Close: Modelled on RemoveSavedInteger; removing a boolean isn't measured.</remarks>
    [Native(Level.Close)]
    public void RemoveSavedBoolean(hashtable? table, int parentKey, int childKey) => Remove(table, parentKey, childKey, Slot.Boolean);

    /// <summary>Forgets the string under the two keys and leaves the other types under them; nothing happens with a null hashtable.</summary>
    /// <remarks>Close: Modelled on RemoveSavedInteger; removing a string isn't measured.</remarks>
    [Native(Level.Close)]
    public void RemoveSavedString(hashtable? table, int parentKey, int childKey) => Remove(table, parentKey, childKey, Slot.String);

    /// <summary>Forgets the handle under the two keys and leaves the other types under them; nothing happens with a null hashtable.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public void RemoveSavedHandle(hashtable? table, int parentKey, int childKey) => Remove(table, parentKey, childKey, Slot.Handle);

    /// <summary>Forgets everything kept under the parent key; nothing happens with a null hashtable.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public void FlushChildHashtable(hashtable? table, int parentKey) => table?.Parents.Remove(parentKey);

    /// <summary>Empties the hashtable and destroys it, so it holds nothing more and another can be made in its place.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)]
    public void FlushParentHashtable(hashtable? table)
    {
        if (table is null || !Alive(table)) return;
        table.Parents.Clear();
        _game.Remove(table.Key);
        _hashtables--;
    }

    // One handle a child key, whatever its type. Exact for the types the probes saved and loaded that a test can make here;
    // the probes' location and text tag saves gave true, but Location and CreateTextTag are Missing still.
    /// <summary>Keeps the ability under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or ability.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveAbilityHandle(hashtable? table, int parentKey, int childKey, ability? whichAbility) => SaveHandle(table, parentKey, childKey, whichAbility);
    /// <summary>Keeps the agent under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or agent.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)] public bool SaveAgentHandle(hashtable? table, int parentKey, int childKey, agent? whichAgent) => SaveHandle(table, parentKey, childKey, whichAgent);
    /// <summary>Keeps the boolexpr under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or boolexpr.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveBooleanExprHandle(hashtable? table, int parentKey, int childKey, boolexpr? whichBoolexpr) => SaveHandle(table, parentKey, childKey, whichBoolexpr);
    /// <summary>Keeps the button under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or button.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveButtonHandle(hashtable? table, int parentKey, int childKey, button? whichButton) => SaveHandle(table, parentKey, childKey, whichButton);
    /// <summary>Keeps the defeat condition under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or defeat condition.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveDefeatConditionHandle(hashtable? table, int parentKey, int childKey, defeatcondition? whichDefeatcondition) => SaveHandle(table, parentKey, childKey, whichDefeatcondition);
    /// <summary>Keeps the destructable under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or destructable.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveDestructableHandle(hashtable? table, int parentKey, int childKey, destructable? whichDestructable) => SaveHandle(table, parentKey, childKey, whichDestructable);
    /// <summary>Keeps the dialog under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or dialog.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveDialogHandle(hashtable? table, int parentKey, int childKey, dialog? whichDialog) => SaveHandle(table, parentKey, childKey, whichDialog);
    /// <summary>Keeps the effect under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or effect.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveEffectHandle(hashtable? table, int parentKey, int childKey, effect? whichEffect) => SaveHandle(table, parentKey, childKey, whichEffect);
    /// <summary>Keeps the fog modifier under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or fog modifier.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveFogModifierHandle(hashtable? table, int parentKey, int childKey, fogmodifier? whichFogModifier) => SaveHandle(table, parentKey, childKey, whichFogModifier);
    // The game's answer for a Convert native's value varied: false in one run of the hashtables probe, true in the next (D125).
    /// <summary>Keeps the fog state under the two keys, giving false for one a Convert native made; false, keeping nothing, with a null or destroyed hashtable or fog state.</summary>
    /// <remarks>Close: The game's answer for a Convert native's value varied, false in one run of the hashtables probe and true in the next (D125); false is modelled.</remarks>
    [Native(Level.Close)] public bool SaveFogStateHandle(hashtable? table, int parentKey, int childKey, fogstate? whichFogState) => SaveHandle(table, parentKey, childKey, whichFogState);
    /// <summary>Keeps the force under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or force.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveForceHandle(hashtable? table, int parentKey, int childKey, force? whichForce) => SaveHandle(table, parentKey, childKey, whichForce);
    /// <summary>Keeps the frame under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or frame.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveFrameHandle(hashtable? table, int parentKey, int childKey, framehandle? whichFrameHandle) => SaveHandle(table, parentKey, childKey, whichFrameHandle);
    /// <summary>Keeps the group under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or group.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)] public bool SaveGroupHandle(hashtable? table, int parentKey, int childKey, group? whichGroup) => SaveHandle(table, parentKey, childKey, whichGroup);
    /// <summary>Keeps the hashtable under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or hashtable.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveHashtableHandle(hashtable? table, int parentKey, int childKey, hashtable? whichHashtable) => SaveHandle(table, parentKey, childKey, whichHashtable);
    /// <summary>Keeps the image under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or image.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveImageHandle(hashtable? table, int parentKey, int childKey, image? whichImage) => SaveHandle(table, parentKey, childKey, whichImage);
    /// <summary>Keeps the item under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or item.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveItemHandle(hashtable? table, int parentKey, int childKey, item? whichItem) => SaveHandle(table, parentKey, childKey, whichItem);
    /// <summary>Keeps the item pool under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or item pool.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveItemPoolHandle(hashtable? table, int parentKey, int childKey, itempool? whichItempool) => SaveHandle(table, parentKey, childKey, whichItempool);
    /// <summary>Keeps the leaderboard under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or leaderboard.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveLeaderboardHandle(hashtable? table, int parentKey, int childKey, leaderboard? whichLeaderboard) => SaveHandle(table, parentKey, childKey, whichLeaderboard);
    /// <summary>Keeps the lightning under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or lightning.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveLightningHandle(hashtable? table, int parentKey, int childKey, lightning? whichLightning) => SaveHandle(table, parentKey, childKey, whichLightning);
    /// <summary>Keeps the location under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or location.</summary>
    /// <remarks>Close: The probes' save gave true, but Location is Missing, so a test can't make one here and it isn't checked.</remarks>
    [Native(Level.Close)] public bool SaveLocationHandle(hashtable? table, int parentKey, int childKey, location? whichLocation) => SaveHandle(table, parentKey, childKey, whichLocation);
    /// <summary>Keeps the multiboard under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or multiboard.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveMultiboardHandle(hashtable? table, int parentKey, int childKey, multiboard? whichMultiboard) => SaveHandle(table, parentKey, childKey, whichMultiboard);
    /// <summary>Keeps the multiboard item under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or multiboard item.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveMultiboardItemHandle(hashtable? table, int parentKey, int childKey, multiboarditem? whichMultiboarditem) => SaveHandle(table, parentKey, childKey, whichMultiboarditem);
    /// <summary>Keeps the player under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or player.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)] public bool SavePlayerHandle(hashtable? table, int parentKey, int childKey, player? whichPlayer) => SaveHandle(table, parentKey, childKey, whichPlayer);
    /// <summary>Keeps the quest under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or quest.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveQuestHandle(hashtable? table, int parentKey, int childKey, quest? whichQuest) => SaveHandle(table, parentKey, childKey, whichQuest);
    /// <summary>Keeps the quest item under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or quest item.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveQuestItemHandle(hashtable? table, int parentKey, int childKey, questitem? whichQuestitem) => SaveHandle(table, parentKey, childKey, whichQuestitem);
    /// <summary>Keeps the rect under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or rect.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveRectHandle(hashtable? table, int parentKey, int childKey, rect? whichRect) => SaveHandle(table, parentKey, childKey, whichRect);
    /// <summary>Keeps the region under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or region.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveRegionHandle(hashtable? table, int parentKey, int childKey, region? whichRegion) => SaveHandle(table, parentKey, childKey, whichRegion);
    /// <summary>Keeps the sound under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or sound.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveSoundHandle(hashtable? table, int parentKey, int childKey, sound? whichSound) => SaveHandle(table, parentKey, childKey, whichSound);
    /// <summary>Keeps the text tag under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or text tag.</summary>
    /// <remarks>Close: The probes' save gave true, but CreateTextTag is Missing, so a test can't make one here and it isn't checked.</remarks>
    [Native(Level.Close)] public bool SaveTextTagHandle(hashtable? table, int parentKey, int childKey, texttag? whichTexttag) => SaveHandle(table, parentKey, childKey, whichTexttag);
    /// <summary>Keeps the timer dialog under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or timer dialog.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveTimerDialogHandle(hashtable? table, int parentKey, int childKey, timerdialog? whichTimerdialog) => SaveHandle(table, parentKey, childKey, whichTimerdialog);
    /// <summary>Keeps the timer under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or timer.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)] public bool SaveTimerHandle(hashtable? table, int parentKey, int childKey, timer? whichTimer) => SaveHandle(table, parentKey, childKey, whichTimer);
    /// <summary>Keeps the trackable under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or trackable.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveTrackableHandle(hashtable? table, int parentKey, int childKey, trackable? whichTrackable) => SaveHandle(table, parentKey, childKey, whichTrackable);
    /// <summary>Keeps the trigger action under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or trigger action.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveTriggerActionHandle(hashtable? table, int parentKey, int childKey, triggeraction? whichTriggeraction) => SaveHandle(table, parentKey, childKey, whichTriggeraction);
    /// <summary>Keeps the trigger condition under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or trigger condition.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveTriggerConditionHandle(hashtable? table, int parentKey, int childKey, triggercondition? whichTriggercondition) => SaveHandle(table, parentKey, childKey, whichTriggercondition);
    /// <summary>Keeps the trigger event under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or trigger event.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveTriggerEventHandle(hashtable? table, int parentKey, int childKey, @event? whichEvent) => SaveHandle(table, parentKey, childKey, whichEvent);
    /// <summary>Keeps the trigger under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or trigger.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveTriggerHandle(hashtable? table, int parentKey, int childKey, trigger? whichTrigger) => SaveHandle(table, parentKey, childKey, whichTrigger);
    /// <summary>Keeps the ubersplat under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or ubersplat.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveUbersplatHandle(hashtable? table, int parentKey, int childKey, ubersplat? whichUbersplat) => SaveHandle(table, parentKey, childKey, whichUbersplat);
    /// <summary>Keeps the unit under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or unit.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveUnitHandle(hashtable? table, int parentKey, int childKey, unit? whichUnit) => SaveHandle(table, parentKey, childKey, whichUnit);
    /// <summary>Keeps the unit pool under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or unit pool.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveUnitPoolHandle(hashtable? table, int parentKey, int childKey, unitpool? whichUnitpool) => SaveHandle(table, parentKey, childKey, whichUnitpool);
    /// <summary>Keeps the widget under the two keys and gives true; false, keeping nothing, with a null or destroyed hashtable or widget.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public bool SaveWidgetHandle(hashtable? table, int parentKey, int childKey, widget? whichWidget) => SaveHandle(table, parentKey, childKey, whichWidget);

    /// <summary>The ability kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public ability? LoadAbilityHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<ability>(table, parentKey, childKey);
    /// <summary>The boolexpr kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public boolexpr? LoadBooleanExprHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<boolexpr>(table, parentKey, childKey);
    /// <summary>The button kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public button? LoadButtonHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<button>(table, parentKey, childKey);
    /// <summary>The defeat condition kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public defeatcondition? LoadDefeatConditionHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<defeatcondition>(table, parentKey, childKey);
    /// <summary>The destructable kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public destructable? LoadDestructableHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<destructable>(table, parentKey, childKey);
    /// <summary>The dialog kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public dialog? LoadDialogHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<dialog>(table, parentKey, childKey);
    /// <summary>The effect kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public effect? LoadEffectHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<effect>(table, parentKey, childKey);
    /// <summary>The fog modifier kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public fogmodifier? LoadFogModifierHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<fogmodifier>(table, parentKey, childKey);
    /// <summary>The fog state kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)] public fogstate? LoadFogStateHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<fogstate>(table, parentKey, childKey);
    /// <summary>The force kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public force? LoadForceHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<force>(table, parentKey, childKey);
    /// <summary>The frame kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public framehandle? LoadFrameHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<framehandle>(table, parentKey, childKey);
    /// <summary>The group kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)] public group? LoadGroupHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<group>(table, parentKey, childKey);
    /// <summary>The hashtable kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public hashtable? LoadHashtableHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<hashtable>(table, parentKey, childKey);
    /// <summary>The image kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public image? LoadImageHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<image>(table, parentKey, childKey);
    /// <summary>The item kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public item? LoadItemHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<item>(table, parentKey, childKey);
    /// <summary>The item pool kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public itempool? LoadItemPoolHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<itempool>(table, parentKey, childKey);
    /// <summary>The leaderboard kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public leaderboard? LoadLeaderboardHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<leaderboard>(table, parentKey, childKey);
    /// <summary>The lightning kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public lightning? LoadLightningHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<lightning>(table, parentKey, childKey);
    /// <summary>The location kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public location? LoadLocationHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<location>(table, parentKey, childKey);
    /// <summary>The multiboard kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public multiboard? LoadMultiboardHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<multiboard>(table, parentKey, childKey);
    /// <summary>The multiboard item kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public multiboarditem? LoadMultiboardItemHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<multiboarditem>(table, parentKey, childKey);
    /// <summary>The player kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)] public player? LoadPlayerHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<player>(table, parentKey, childKey);
    /// <summary>The quest kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public quest? LoadQuestHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<quest>(table, parentKey, childKey);
    /// <summary>The quest item kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public questitem? LoadQuestItemHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<questitem>(table, parentKey, childKey);
    /// <summary>The rect kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public rect? LoadRectHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<rect>(table, parentKey, childKey);
    /// <summary>The region kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public region? LoadRegionHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<region>(table, parentKey, childKey);
    /// <summary>The sound kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public sound? LoadSoundHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<sound>(table, parentKey, childKey);
    /// <summary>The text tag kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public texttag? LoadTextTagHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<texttag>(table, parentKey, childKey);
    /// <summary>The timer dialog kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public timerdialog? LoadTimerDialogHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<timerdialog>(table, parentKey, childKey);
    /// <summary>The timer kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)] public timer? LoadTimerHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<timer>(table, parentKey, childKey);
    /// <summary>The trackable kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public trackable? LoadTrackableHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<trackable>(table, parentKey, childKey);
    /// <summary>The trigger action kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public triggeraction? LoadTriggerActionHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<triggeraction>(table, parentKey, childKey);
    /// <summary>The trigger condition kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public triggercondition? LoadTriggerConditionHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<triggercondition>(table, parentKey, childKey);
    /// <summary>The trigger event kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public @event? LoadTriggerEventHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<@event>(table, parentKey, childKey);
    /// <summary>The trigger kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public trigger? LoadTriggerHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<trigger>(table, parentKey, childKey);
    /// <summary>The ubersplat kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public ubersplat? LoadUbersplatHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<ubersplat>(table, parentKey, childKey);
    /// <summary>The unit kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public unit? LoadUnitHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<unit>(table, parentKey, childKey);
    /// <summary>The unit pool kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Close: Not checked against the game for this type: Exact covers only the types the probes saved and loaded that a test can make here.</remarks>
    [Native(Level.Close)] public unitpool? LoadUnitPoolHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<unitpool>(table, parentKey, childKey);
    /// <summary>The widget kept under the two keys; null when none is, when it is of another type, or when it was destroyed since.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes hashtables, strings-2)).</remarks>
    [Native(Level.Exact, Fact = TablesFact)] public widget? LoadWidgetHandle(hashtable? table, int parentKey, int childKey) => LoadHandle<widget>(table, parentKey, childKey);
}
