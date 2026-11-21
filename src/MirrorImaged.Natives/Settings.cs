using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

// The game's settings, menu and bookkeeping natives: what a getter reads back is kept; the screens, saves and campaign menus
// aren't simulated and are Recorded. Defaults not measured are noted (decisions D600).
public partial class unit
{
    internal bool PathingOff;
}

public sealed partial class GameNatives
{
    const string SettingsNote = "A setting of the game's screens or menus; no native reads it back";
    const string TestRunProbe = "probe natives-test-run (11-21, D791)";

    readonly Dictionary<string, object> _settings = [];

    T Setting<T>(string key, T otherwise) => _settings.TryGetValue(key, out var value) && value is T set ? set : otherwise;

    /// <summary>The ARGB colour as one int, each of the four parts cut to its low 8 bits.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md: ConvertColor's packing (ARGB)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md: ConvertColor's packing (ARGB)")]
    public int BlzConvertColor(int a, int r, int g, int b) => ((a & 255) << 24) | ((r & 255) << 16) | ((g & 255) << 8) | (b & 255);

    /// <summary>The id of a four-character ability code, as FourCC reads it; 0 for another length.</summary>
    /// <remarks>Close: Reads the string as FourCC does; the game's answer for a string of another length is not measured.</remarks>
    [Native(Level.Close)]
    public int AbilityId(string? abilityIdString) => abilityIdString is { Length: 4 } code ? Code(code) : 0;

    /// <summary>The id written as its four characters, high byte first.</summary>
    /// <remarks>Close: Writes any id as four characters, whether an ability has it or not; the game's answer for an id with no ability is not measured.</remarks>
    [Native(Level.Close)]
    public string? AbilityId2String(int abilityId) => Name(abilityId);

    /// <summary>The id of a named order (any case), 0 for a name the game doesn't give an order (probe orders-table: 373 names).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Orders: probe orders-table (11-15); game-facts.md, Orders and events: OrderId2String (10-15, 11-03)).</remarks>
    [Native(Level.Exact, Fact = "facts, Orders: probe orders-table (11-15); game-facts.md, Orders and events: OrderId2String (10-15, 11-03)")]
    public int OrderId(string? orderIdString) => orderIdString is null ? 0 : OrderIds.GetValueOrDefault(orderIdString);

    /// <summary>The name of an order id; "" for an id with none (852008, 851974), also an id of a unit.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Orders: probe orders-table (11-15); game-facts.md, Orders and events: OrderId2String (10-15, 11-03)).</remarks>
    [Native(Level.Exact, Fact = "facts, Orders: probe orders-table (11-15); game-facts.md, Orders and events: OrderId2String (10-15, 11-03)")]
    public string? OrderId2String(int orderId) => OrderNames.GetValueOrDefault(orderId, "");

    // The animation names of ANIM_TYPE_*
    static readonly string[] AnimNames = ["birth", "death", "decay", "dissipate", "stand", "walk", "attack", "morph", "sleep", "spell", "portrait"];

    /// <summary>The name of an animation type (birth, death, stand, walk, attack and the rest); "" for nil or a type past the list.</summary>
    /// <remarks>Close: The names are the ANIM_TYPE_* words as the list in this file has them, not measured in the game.</remarks>
    [Native(Level.Close)]
    public string? BlzGetAnimName(animtype? whichAnim) => whichAnim is { Id: >= 0 } a && a.Id < AnimNames.Length ? AnimNames[a.Id] : "";

    // The model-art columns of an ability by effect type: effect, target, caster, special, area effect, missile, lightning
    static readonly string[] EffectColumns = ["Effectart", "Targetart", "Casterart", "Specialart", "Areaeffectart", "Missileart", "LightningEffect"];

    /// <summary>The model at the index of the ability's art column for the effect type, "" without one.</summary>
    /// <remarks>Close: Reads the ability's art column from the object data; the game's answer for an index past the list is not measured.</remarks>
    [Native(Level.Close)]
    public string? GetAbilityEffectById(int abilityId, effecttype? t, int index)
    {
        if (t is not { Id: >= 0 } || t.Id >= EffectColumns.Length || Data.Object(ObjectKind.Ability, abilityId) is not { } type || Data.FieldAt(ObjectKind.Ability, EffectColumns[t.Id]) is not { } field) return "";
        var models = type.Text(field)?.Split(',', StringSplitOptions.TrimEntries) ?? [];
        return index >= 0 && index < models.Length ? models[index] : "";
    }

    /// <summary>As <c>GetAbilityEffectById</c>, for the ability named by its four-character code.</summary>
    /// <remarks>Close: Reads the code as <c>AbilityId</c> does, then as <c>GetAbilityEffectById</c> does; not measured in the game.</remarks>
    [Native(Level.Close)]
    public string? GetAbilityEffect(string? abilityString, effecttype? t, int index) => GetAbilityEffectById(AbilityId(abilityString), t, index);

    // ---- The player's screen and input

    /// <summary>True until <c>BlzEnableSelections</c> turns selection off.</summary>
    /// <remarks>Close: Kept as set; the default of true is not measured (D600).</remarks>
    [Native(Level.Close)] public bool BlzIsSelectionEnabled() => Setting("selection", true);
    /// <summary>True until <c>BlzEnableSelections</c> turns the selection circles off.</summary>
    /// <remarks>Close: Kept as set; the default of true is not measured (D600).</remarks>
    [Native(Level.Close)] public bool BlzIsSelectionCircleEnabled() => Setting("circle", true);
    /// <summary>True until <c>BlzEnableTargetIndicator</c> turns the target indicator off.</summary>
    /// <remarks>Close: Kept as set; the default of true is not measured (D600).</remarks>
    [Native(Level.Close)] public bool BlzIsTargetIndicatorEnabled() => Setting("indicator", true);
    /// <summary>Keeps whether selection and the selection circles are on, for their getters.</summary>
    /// <remarks>Close: Kept for its getters; it doesn't stop <c>SelectUnit</c> here, and what it does in the game is not measured.</remarks>
    [Native(Level.Close)] public void BlzEnableSelections(bool enableSelection, bool enableSelectionCircle) { _settings["selection"] = enableSelection; _settings["circle"] = enableSelectionCircle; }
    /// <summary>Keeps whether the target indicator is on, for <c>BlzIsTargetIndicatorEnabled</c>.</summary>
    /// <remarks>Close: Kept for its getter; what it does in the game is not measured.</remarks>
    [Native(Level.Close)] public void BlzEnableTargetIndicator(bool enable) => _settings["indicator"] = enable;
    /// <summary>The HUD scale as a fraction of 1, 1 until <c>BlzSetHUDScale</c> sets it.</summary>
    /// <remarks>Close: Kept as set; the default of 1 is not measured (D600).</remarks>
    [Native(Level.Close)] public float BlzGetHUDScale() => Setting("hud", 1f);
    /// <summary>Keeps the percentage, as a fraction of 1, for <c>BlzGetHUDScale</c>.</summary>
    /// <remarks>Close: Kept as set with no limit; the game's range for the percentage is not measured.</remarks>
    [Native(Level.Close)] public void BlzSetHUDScale(int percentage) => _settings["hud"] = percentage / 100f;
    /// <summary>The count <c>BlzSetMinShadowCastingPointLightCount</c> kept; 0 until then.</summary>
    /// <remarks>Close: Kept as set; the default of 0 is not measured (D600).</remarks>
    [Native(Level.Close)] public int BlzGetMinShadowCastingPointLightCount() => Setting("lights", 0);
    /// <summary>Keeps the count, for <c>BlzGetMinShadowCastingPointLightCount</c>.</summary>
    /// <remarks>Close: Kept for its getter; what it does in the game is not measured.</remarks>
    [Native(Level.Close)] public void BlzSetMinShadowCastingPointLightCount(int count) => _settings["lights"] = count;
    /// <summary>1920 by 1080, a little larger on each other machine of a desync check (<see cref="Machine"/>).</summary>
    /// <remarks>Close: A fixed width, not read from a window; the game answers the width of the real client, not measured.</remarks>
    [Native(Level.Close)] public int BlzGetLocalClientWidth() => 1920 + Machine * 16;
    /// <summary>1080, a little taller on each other machine of a desync check (<see cref="Machine"/>).</summary>
    /// <remarks>Close: A fixed height, not read from a window; the game answers the height of the real client, not measured.</remarks>
    [Native(Level.Close)] public int BlzGetLocalClientHeight() => 1080 + Machine * 9;
    /// <summary>The locale of the client, always enUS.</summary>
    /// <remarks>Close: The game's own language, enUS on the PC measured; MirrorImaged answers enUS wherever it runs.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = "The game's own language, enUS on the PC measured; MirrorImaged answers enUS wherever it runs")]
    public string? BlzGetLocale() => "enUS";

    /// <summary>True: the game's window counts as the active one.</summary>
    /// <remarks>Close: True while the game's window is the active one, as it was in the runs measured; a window the player has switched away from isn't measured, and a run has none.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = "True while the game's window is the active one, as it was in the runs measured; a window the player has switched away from isn't measured, and a run has none")]
    public bool BlzIsLocalClientActive() => true;

    /// <summary>The player's living town halls of any race and tier, one being built too; the melee script reveals a player that has none.</summary>
    /// <remarks>Close: A town hall of a kind not in the list that <c>IsTownHall</c> reads is not counted; the game's own count is not measured.</remarks>
    [Native(Level.Close)] public int BlzGetPlayerTownHallCount(player? whichPlayer) => whichPlayer is null ? 0 : Mine(whichPlayer).Count(IsTownHall);

    /// <summary>Changes nothing; the cursor isn't shown or hidden.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzEnableCursor(bool enable) { }
    /// <summary>Changes nothing; the interface isn't laid out.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzEnableUIAutoPosition(bool enable) { }
    /// <summary>Changes nothing; <c>BlzSetHUDScale</c> is not stopped.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzLockHUDScale() { }
    /// <summary>Changes nothing; it undoes nothing here.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzUnlockHUDScale() { }
    /// <summary>Changes nothing; no video is recorded.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzStartRecording(int fps) { }
    /// <summary>Changes nothing; there is no recording to end.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzEndRecording() { }
    /// <summary>Changes nothing; the message isn't shown.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzDisplayChatMessage(player? whichPlayer, int recipient, string? message) { }
    /// <summary>Changes nothing; the mouse isn't moved.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzSetMousePos(int x, int y) { }
    /// <summary>Changes nothing; dragging a selection box isn't simulated.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableDragSelect(bool state, bool ui) { }
    /// <summary>Changes nothing; the highlight under the mouse isn't simulated.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnablePreSelect(bool state, bool ui) { }
    /// <summary>Changes nothing; it doesn't stop <c>SelectUnit</c>.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableSelect(bool state, bool ui) { }
    /// <summary>Changes nothing; the minimap isn't drawn.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableMinimapFilterButtons(bool enableAlly, bool enableCreep) { }
    /// <summary>Changes nothing; nothing is drawn.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableOcclusion(bool flag) { }
    /// <summary>Changes nothing; the player's own commands aren't simulated.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableUserControl(bool b) { }
    /// <summary>Changes nothing; the interface isn't drawn.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableUserUI(bool b) { }
    /// <summary>Changes nothing; no interface is open to cancel.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void ForceUICancel() { }
    /// <summary>Changes nothing; no key is pressed.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void ForceUIKey(string? key) { }
    /// <summary>Changes nothing; the interface isn't drawn.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void ShowInterface(bool flag, float fadeDuration) { }
    /// <summary>Changes nothing; no hero buttons are drawn.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void SetReservedLocalHeroButtons(int reserved) { }
    /// <summary>Changes nothing; no score screen is shown.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void SetPlayerOnScoreScreen(player? whichPlayer, bool flag) { }
    /// <summary>Changes nothing; no hero data is carried to another map.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void CachePlayerHeroData(player? whichPlayer) { }
    /// <summary>Changes nothing; no replay is saved.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void DoNotSaveReplay() { }
    /// <summary>Changes nothing; no menu is shown.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void DisableRestartMission(bool flag) { }
    /// <summary>Changes nothing; no dialog is shown.</summary>
    /// <remarks>Recorded: A setting of the game's screens or menus; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = SettingsNote)] public void DisplayLoadDialog() { }
    /// <summary>Changes nothing; the code isn't acted on.</summary>
    /// <remarks>Recorded: A cheat code typed into the game; nothing here acts on it.</remarks>
    [Native(Level.Recorded, Reason = "A cheat code typed into the game; nothing here acts on it")] public void Cheat(string? cheatStr) { }
    /// <summary>Changes nothing; no panels are drawn.</summary>
    /// <remarks>Recorded: The cinematic panels and model cinematics are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic panels and model cinematics are drawn; no native reads them back")] public void BlzHideCinematicPanels(bool enable) { }
    /// <summary>Changes nothing; nothing is drawn.</summary>
    /// <remarks>Recorded: The cinematic panels and model cinematics are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic panels and model cinematics are drawn; no native reads them back")] public void BlzSetCinematicEnabledDE(bool enable) { }
    /// <summary>Changes nothing; the cinematic isn't played.</summary>
    /// <remarks>Recorded: The cinematic panels and model cinematics are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic panels and model cinematics are drawn; no native reads them back")] public void BlzPlayModelCinematicGameAtPosition(string? modelName, float posX, float posY, float posZ, float RotZ) { }
    /// <summary>True for the empty name, false for any other.</summary>
    /// <remarks>Close: True only for the empty name, as the game answered; a model it would load as a cinematic wasn't found (a camera model, a unit's and a name with no folder gave false), so none answers true here.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = "True only for the empty name, as the game answered; a model it would load as a cinematic wasn't found (a camera model, a unit's and a name with no folder gave false), so none answers true here")]
    public bool BlzPreloadModelCinematicGame(string? modelName) => modelName == "";
    /// <summary>0: no model cinematic plays here.</summary>
    /// <remarks>Close: A model cinematic plays only from BlzPlayModelCinematicGameAtPosition, which is Recorded here; the game answered 0 with none playing.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = "A model cinematic plays only from BlzPlayModelCinematicGameAtPosition, which is Recorded here; the game answered 0 with none playing")]
    public int BlzGetModelCinematicGameCurrentShot() => 0;

    /// <summary>0: no model cinematic plays here.</summary>
    /// <remarks>Close: A model cinematic plays only from BlzPlayModelCinematicGameAtPosition, which is Recorded here; the game answered 0.0 with none playing.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = "A model cinematic plays only from BlzPlayModelCinematicGameAtPosition, which is Recorded here; the game answered 0.0 with none playing")]
    public float BlzGetModelCinematicGameRemainingTime() => 0;

    /// <summary>0: no model cinematic plays here.</summary>
    /// <remarks>Close: A model cinematic plays only from BlzPlayModelCinematicGameAtPosition, which is Recorded here; the game answered 0 with none playing.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = "A model cinematic plays only from BlzPlayModelCinematicGameAtPosition, which is Recorded here; the game answered 0 with none playing")]
    public int BlzGetModelCinematicGameShotCount() => 0;

    /// <summary>Changes nothing; no test harness runs.</summary>
    /// <remarks>Recorded: The automation harness of the game's own tests; nothing here runs it.</remarks>
    [Native(Level.Recorded, Reason = "The automation harness of the game's own tests; nothing here runs it")] public void AutomationSetTestType(string? testType) { }
    /// <summary>Changes nothing; no test ends.</summary>
    /// <remarks>Recorded: The automation harness of the game's own tests; nothing here runs it.</remarks>
    [Native(Level.Recorded, Reason = "The automation harness of the game's own tests; nothing here runs it")] public void AutomationTestEnd() { }
    /// <summary>Changes nothing; no test starts.</summary>
    /// <remarks>Recorded: The automation harness of the game's own tests; nothing here runs it.</remarks>
    [Native(Level.Recorded, Reason = "The automation harness of the game's own tests; nothing here runs it")] public void AutomationTestStart(string? testName) { }
    /// <summary>Changes nothing; no test run finishes.</summary>
    /// <remarks>Recorded: The automation harness of the game's own tests; nothing here runs it.</remarks>
    [Native(Level.Recorded, Reason = "The automation harness of the game's own tests; nothing here runs it")] public void AutomationTestingFinished() { }

    // ---- The game's options, kept as set

    /// <summary>The state <c>SetAllyColorFilterState</c> kept; 0 until then.</summary>
    /// <remarks>Close: Kept as set; the default of 0 is not measured (D600).</remarks>
    [Native(Level.Close)] public int GetAllyColorFilterState() => Setting("ally", 0);
    /// <summary>Keeps the state, for <c>GetAllyColorFilterState</c>.</summary>
    /// <remarks>Close: Kept as set, whatever the value; the game's range of states is not measured.</remarks>
    [Native(Level.Close)] public void SetAllyColorFilterState(int state) => _settings["ally"] = state;
    /// <summary>The state <c>SetCreepCampFilterState</c> kept; false until then.</summary>
    /// <remarks>Close: Kept as set; the default of false is not measured (D600).</remarks>
    [Native(Level.Close)] public bool GetCreepCampFilterState() => Setting("creeps", false);
    /// <summary>Keeps the state, for <c>GetCreepCampFilterState</c>.</summary>
    /// <remarks>Close: Kept for its getter; what the game's minimap does with it is not simulated.</remarks>
    [Native(Level.Close)] public void SetCreepCampFilterState(bool state) => _settings["creeps"] = state;
    /// <summary>Whether the campaign button was made visible; false for a button never set.</summary>
    /// <remarks>Close: Kept as set for each button number; the default of false is not measured (D600).</remarks>
    [Native(Level.Close)] public bool GetCustomCampaignButtonVisible(int whichButton) => Setting($"campaign{whichButton}", false);
    /// <summary>Keeps whether the campaign button is visible, for each button number.</summary>
    /// <remarks>Close: Kept for its getter; the campaign screen is not simulated.</remarks>
    [Native(Level.Close)] public void SetCustomCampaignButtonVisible(int whichButton, bool visible) => _settings[$"campaign{whichButton}"] = visible;
    /// <summary>The difficulty <c>SetDefaultDifficulty</c> kept; 1 (normal) until then.</summary>
    /// <remarks>Close: Kept as set; the default of 1 is not measured (D600).</remarks>
    [Native(Level.Close)] public gamedifficulty? GetDefaultDifficulty() => Convert<gamedifficulty>(Setting("difficulty", 1));
    /// <summary>Keeps the difficulty, for <c>GetDefaultDifficulty</c>; nil changes nothing.</summary>
    /// <remarks>Close: Kept for its getter; what the game's menus do with it is not simulated.</remarks>
    [Native(Level.Close)] public void SetDefaultDifficulty(gamedifficulty? g) { if (g is not null) _settings["difficulty"] = g.Id; }
    /// <summary>The density <c>SetCreatureDensity</c> kept; 0 until then.</summary>
    /// <remarks>Close: Kept as set; the default of 0 is not measured (D600).</remarks>
    [Native(Level.Close)] public mapdensity? GetCreatureDensity() => Convert<mapdensity>(Setting("creature", 0));
    /// <summary>Keeps the density, for <c>GetCreatureDensity</c>; nil changes nothing.</summary>
    /// <remarks>Close: Kept for its getter; the game's map generation is not simulated.</remarks>
    [Native(Level.Close)] public void SetCreatureDensity(mapdensity? whichdensity) { if (whichdensity is not null) _settings["creature"] = whichdensity.Id; }
    /// <summary>The density <c>SetResourceDensity</c> kept; 0 until then.</summary>
    /// <remarks>Close: Kept as set; the default of 0 is not measured (D600).</remarks>
    [Native(Level.Close)] public mapdensity? GetResourceDensity() => Convert<mapdensity>(Setting("resource", 0));
    /// <summary>Keeps the density, for <c>GetResourceDensity</c>; nil changes nothing.</summary>
    /// <remarks>Close: Kept for its getter; the game's map generation is not simulated.</remarks>
    [Native(Level.Close)] public void SetResourceDensity(mapdensity? whichdensity) { if (whichdensity is not null) _settings["resource"] = whichdensity.Id; }
    /// <summary>Whether the game type was set supported; false for nil and for one never set.</summary>
    /// <remarks>Close: Kept as set; the default of false is not measured (D600).</remarks>
    [Native(Level.Close)] public bool IsGameTypeSupported(gametype? whichGameType) => whichGameType is not null && Setting($"type{whichGameType.Id}", false);
    /// <summary>Keeps whether the game type is supported, for <c>IsGameTypeSupported</c>; nil changes nothing.</summary>
    /// <remarks>Close: Kept for its getter; the game's lobby is not simulated.</remarks>
    [Native(Level.Close)] public void SetGameTypeSupported(gametype? whichGameType, bool value) { if (whichGameType is not null) _settings[$"type{whichGameType.Id}"] = value; }

    /// <summary>The percentage one player gives another of a resource each pay, as set; 0 until it is.</summary>
    /// <remarks>Close: Kept for each pair of players and resource, as set; the game's tax on income is not simulated and its default is not measured.</remarks>
    [Native(Level.Close)]
    public int GetPlayerTaxRate(player? sourcePlayer, player? otherPlayer, playerstate? whichResource) =>
        sourcePlayer is null || otherPlayer is null || whichResource is null ? 0 : Setting($"tax{sourcePlayer.GetHashCode()},{otherPlayer.GetHashCode()},{whichResource.Id}", 0);

    /// <summary>Keeps the rate for the two players and the resource, for <c>GetPlayerTaxRate</c>; nothing when any of the three is nil.</summary>
    /// <remarks>Close: Kept as set with no limit; the game's range of rates is not measured, and no tax is taken here.</remarks>
    [Native(Level.Close)]
    public void SetPlayerTaxRate(player? sourcePlayer, player? otherPlayer, playerstate? whichResource, int rate)
    {
        if (sourcePlayer is not null && otherPlayer is not null && whichResource is not null)
            _settings[$"tax{sourcePlayer.GetHashCode()},{otherPlayer.GetHashCode()},{whichResource.Id}"] = rate;
    }

    /// <summary>The difficulty the run seats its computers at, Normal unless it says otherwise (decisions D141, D430); Close.</summary>
    /// <remarks>Close: Any player gives the run's difficulty; the game's answer for a human player is not measured.</remarks>
    [Native(Level.Close)] public aidifficulty? GetAIDifficulty(player? num) => Convert<aidifficulty>(Difficulty);

    const string NoTournament = "A test run is not a tournament game: the game answered nil, 0, 0.0 and 0 for the four, whichever player was asked";

    /// <summary>Nil: this is not a tournament game.</summary>
    /// <remarks>Close: A test run is not a tournament game: the game answered nil, 0, 0.0 and 0 for the four, whichever player was asked.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = NoTournament)] public player? GetTournamentFinishNowPlayer() => null;
    /// <summary>0: this is not a tournament game.</summary>
    /// <remarks>Close: A test run is not a tournament game: the game answered nil, 0, 0.0 and 0 for the four, whichever player was asked.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = NoTournament)] public int GetTournamentFinishNowRule() => 0;
    /// <summary>0: this is not a tournament game.</summary>
    /// <remarks>Close: A test run is not a tournament game: the game answered nil, 0, 0.0 and 0 for the four, whichever player was asked.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = NoTournament)] public float GetTournamentFinishSoonTimeRemaining() => 0;
    /// <summary>0 for any player: this is not a tournament game.</summary>
    /// <remarks>Close: A test run is not a tournament game: the game answered nil, 0, 0.0 and 0 for the four, whichever player was asked.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = NoTournament)] public int GetTournamentScore(player? whichPlayer) => 0;

    /// <summary>Both game versions are supported by the installed game and compatible with it.</summary>
    /// <remarks>Close: True for any version but nil; which versions the game accepts is not measured.</remarks>
    [Native(Level.Close)] public bool VersionCompatible(version? whichVersion) => whichVersion is not null;
    /// <summary>True for any version but nil.</summary>
    /// <remarks>Close: True for any version but nil; which versions the game supports is not measured.</remarks>
    [Native(Level.Close)] public bool VersionSupported(version? whichVersion) => whichVersion is not null;

    // ---- Saves, levels and campaigns, which a test run doesn't have

    /// <summary>Changes nothing; no campaign menu is drawn.</summary>
    /// <remarks>Recorded: A campaign menu setting; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetCampaignAvailable(int campaignNumber, bool available) { }
    /// <summary>Changes nothing; no campaign menu is drawn.</summary>
    /// <remarks>Recorded: A campaign menu setting; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetCampaignMenuRace(race? r) { }
    /// <summary>Changes nothing; no campaign menu is drawn.</summary>
    /// <remarks>Recorded: A campaign menu setting; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetCampaignMenuRaceEx(int campaignIndex) { }
    /// <summary>Changes nothing; no cinematic menu is drawn.</summary>
    /// <remarks>Recorded: A campaign menu setting; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetEdCinematicAvailable(int campaignNumber, bool available) { }
    /// <summary>Changes nothing; no mission list is drawn.</summary>
    /// <remarks>Recorded: A campaign menu setting; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetMissionAvailable(int campaignNumber, int missionNumber, bool available) { }
    /// <summary>Changes nothing; no cinematic menu is drawn.</summary>
    /// <remarks>Recorded: A campaign menu setting; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetOpCinematicAvailable(int campaignNumber, bool available) { }
    /// <summary>Changes nothing; no tutorial record is kept.</summary>
    /// <remarks>Recorded: A campaign menu setting; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetTutorialCleared(bool cleared) { }
    /// <summary>Changes nothing; no campaign screen is shown.</summary>
    /// <remarks>Recorded: A campaign menu setting; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void ForceCampaignSelectScreen() { }
    /// <summary>Changes nothing; no file is saved.</summary>
    /// <remarks>Recorded: Saved games are the game's files; a test run keeps none.</remarks>
    [Native(Level.Recorded, Reason = "Saved games are the game's files; a test run keeps none")] public void SaveGame(string? saveFileName) { }
    /// <summary>Changes nothing; no file is saved.</summary>
    /// <remarks>Recorded: Saved games are the game's files; a test run keeps none.</remarks>
    [Native(Level.Recorded, Reason = "Saved games are the game's files; a test run keeps none")] public void SaveGameCheckpoint(string? saveFileName, bool showWindow) { }
    /// <summary>Changes nothing; no checkpoint saves are kept.</summary>
    /// <remarks>Recorded: Saved games are the game's files; a test run keeps none.</remarks>
    [Native(Level.Recorded, Reason = "Saved games are the game's files; a test run keeps none")] public void SetMaxCheckpointSaves(int maxCheckpointSaves) { }
    const string NoSaves = "A test run keeps no saved games (SaveGame is Recorded): the game answered false for a name that doesn't exist, and a save of its own isn't measured";

    /// <summary>False: a test run keeps no saved games.</summary>
    /// <remarks>Close: A test run keeps no saved games (SaveGame is Recorded): the game answered false for a name that doesn't exist, and a save of its own isn't measured.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = NoSaves)] public bool SaveGameExists(string? saveName) => false;
    /// <summary>False: no save is copied, a test run keeping none.</summary>
    /// <remarks>Close: A test run keeps no saved games (SaveGame is Recorded): the game answered false for a name that doesn't exist, and a save of its own isn't measured.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = NoSaves)] public bool CopySaveGame(string? sourceSaveName, string? destSaveName) => false;
    /// <summary>False: no directory is removed, a test run keeping none.</summary>
    /// <remarks>Close: A test run keeps no saved games (SaveGame is Recorded): the game answered false for a name that doesn't exist, and a save of its own isn't measured.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = NoSaves)] public bool RemoveSaveDirectory(string? sourceDirName) => false;
    /// <summary>False: no directory is renamed, a test run keeping none.</summary>
    /// <remarks>Close: A test run keeps no saved games (SaveGame is Recorded): the game answered false for a name that doesn't exist, and a save of its own isn't measured.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = NoSaves)] public bool RenameSaveDirectory(string? sourceDirName, string? destDirName) => false;

    /// <summary>The empty string: a test run isn't a game loaded from a save.</summary>
    /// <remarks>Close: A test run isn't a game loaded from a save: the game answered the empty string, before and after copies, removals and renames that found nothing.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = "A test run isn't a game loaded from a save: the game answered the empty string, before and after copies, removals and renames that found nothing")]
    public string? GetSaveBasicFilename() => "";
    /// <summary>The key itself, as the path.</summary>
    /// <remarks>Close: Returns the key unchanged; the game's path for a skin is not measured.</remarks>
    [Native(Level.Close)] public string? SkinManagerGetLocalPath(string? key) => key;

    /// <summary>Ends the run as EndGame does: the next level, the restart and the load are other maps' runs, which this one doesn't make.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Map and game: ChangeLevel ends this map's script); the score screen and the level that loads next are not simulated.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Map and game: ChangeLevel ends this map's script")]
    public void ChangeLevel(string? newLevel, bool doScoreScreen) => EndGame(doScoreScreen);

    /// <summary>Ends the run as <c>EndGame</c> does; the restart is another run, which this one doesn't make.</summary>
    /// <remarks>Close: Modelled as the end of the run; what the game restarts, and with what, is not measured.</remarks>
    [Native(Level.Close)] public void RestartGame(bool doScoreScreen) => EndGame(doScoreScreen);
    /// <summary>Ends the run, with no score screen.</summary>
    /// <remarks>Close: Modelled as the end of the run; what the game reloads is not measured.</remarks>
    [Native(Level.Close)] public void ReloadGame() => EndGame(false);
    /// <summary>Ends the run as <c>EndGame</c> does; the save isn't loaded.</summary>
    /// <remarks>Close: Modelled as the end of the run; what the game loads, and with what, is not measured.</remarks>
    [Native(Level.Close)] public void LoadGame(string? saveFileName, bool doScoreScreen) => EndGame(doScoreScreen);

    /// <summary>Close: the unit made from the name its data gives, nil for a name no unit has.</summary>
    /// <remarks>Close: Reads the name as the data gives it; how the game matches a name, and one that two types share, is not measured.</remarks>
    [Native(Level.Close)]
    public unit? CreateUnitAtLocByName(player? id, string? unitname, location? whichLocation, float face) =>
        whichLocation is null || unitname is null || Data.Id(ObjectKind.Unit, unitname) is not { } unitId ? null : CreateUnit(id, unitId, whichLocation.X, whichLocation.Y, face);

    /// <summary>Off, the unit walks through other units and they through it. Close: the game's unit also walks through buildings and ground.</summary>
    /// <remarks>Close: Only other units are walked through; the buildings and ground the game's unit also passes are not modelled.</remarks>
    [Native(Level.Close)]
    public void SetUnitPathing(unit? whichUnit, bool flag)
    {
        if (whichUnit is not null) whichUnit.PathingOff = !flag;
    }

    // The rest of the build time is what the percentage leaves of it, counted from now, and the life jumps to the building's tenth and that share of it more
    // (probe units-construction, D796). Close: a negative percentage finishes it as 100 does, as measured, and 0 was not seen to finish; life while it grows is flat here
    /// <summary>Sets how far a building under construction is, as a percentage: the rest of its build time is counted from now and its life jumps to match; any other unit is left alone.</summary>
    /// <remarks>Close: measured in part (probes/expected/units-construction.txt); a percentage of 0 was not seen to finish the building, and the life stays flat while it grows.</remarks>
    [Native(Level.Close, Fact = "probes/expected/units-construction.txt")]
    public void UnitSetConstructionProgress(unit? whichUnit, int constructionPercentage)
    {
        if (whichUnit is not { Constructing: true } building) return;
        var percent = constructionPercentage < 0 ? 100 : Math.Min(constructionPercentage, 100);
        var left = Math.Max(building.Type.Real(BuildTime), 0.1) * (1 - percent / 100.0);
        building.Life = Math.Min(building.MaxLife, building.MaxLife * (StartLifeShare + percent / 100f));
        building.ConstructDone = _game.Step + (percent == 100 ? 0 : Math.Max(1, Game.Steps(left)));
        if (percent == 100) FinishBuilding(building);
        else _game.At(building.ConstructDone, () => FinishBuilding(building));
    }

    // The upgrade of a building to another type finishes the rest of its time after now; training and research are not changed (probe units-construction)
    /// <summary>Sets how far the first upgrade in the building's queue is, as a percentage of its time, which then runs out from now; training and research are not changed.</summary>
    /// <remarks>Close: measured in part (probes/expected/units-construction.txt); a percentage outside 0 to 100 is held to it, which the game's answer for is not measured.</remarks>
    [Native(Level.Close, Fact = "probes/expected/units-construction.txt")]
    public void UnitSetUpgradeProgress(unit? whichUnit, int upgradePercentage)
    {
        if (whichUnit is not { Train: [{ Started: true, Upgrade: true } item, ..] } building) return;
        item.Done = _game.Step + Math.Max(1, Game.Steps(item.Seconds * (1 - Math.Clamp(upgradePercentage, 0, 100) / 100.0)));
        _game.At(item.Done, () => FinishTraining(building, item));
    }

    /// <summary>Changes nothing; nothing a native reads back changes.</summary>
    /// <remarks>Recorded: Crippling a player to a force (melee's exposing of a defeated player's units to its enemies) changed none of the visibility reads in the game: the player's units and the ground under them read the same for the force, crippled or not, with fog and mask on or off (probe units-cripple, D796).</remarks>
    [Native(Level.Recorded, Reason = "Crippling a player to a force (melee's exposing of a defeated player's units to its enemies) changed none of the visibility reads in the game: the player's units and the ground under them read the same for the force, crippled or not, with fog and mask on or off (probe units-cripple, D796)")] public void CripplePlayer(player? whichPlayer, force? toWhichPlayers, bool flag) { }

    /// <summary>The cliff level of the corner nearest the point, as the ground's tiles give it; 0 off the map.</summary>
    /// <remarks>Close: Reads the cliff level from the map's tiles at the nearest corner; the game's choice of corner and its answer off the map are not measured.</remarks>
    [Native(Level.Close)]
    public int GetTerrainCliffLevel(float x, float y) => NearestCorner(x, y) is var (column, row) && Data.Terrain is { } terrain ? terrain.CliffLevel(column, row) : 0;

    /// <summary>Close: read as IsTerrainPathable's opposite (true when pathable); not measured.</summary>
    /// <remarks>Close: Modelled as the opposite of <c>IsTerrainPathable</c>; the game's answer is not measured.</remarks>
    [Native(Level.Close)]
    public bool BlzIsTerrainPathableEx(float x, float y, pathingtype? t) => !IsTerrainPathable(x, y, t);
}
