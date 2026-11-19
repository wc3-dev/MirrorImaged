using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// The game's settings, menu and bookkeeping natives: what a getter reads back is kept; the screens, saves and campaign menus
// aren't simulated and are Recorded. Defaults not measured are noted (docs/decisions.md, D600).
public sealed partial class GameNatives
{
    const string SettingsNote = "A setting of the game's screens or menus; no native reads it back";

    readonly Dictionary<string, object> _settings = [];

    T Setting<T>(string key, T otherwise) => _settings.TryGetValue(key, out var value) && value is T set ? set : otherwise;

    [Native(Level.Exact, Fact = "game-facts.md: ConvertColor's packing (ARGB)")]
    public int BlzConvertColor(int a, int r, int g, int b) => ((a & 255) << 24) | ((r & 255) << 16) | ((g & 255) << 8) | (b & 255);

    /// <summary>The id of a four-character ability code, as FourCC reads it; 0 for another length.</summary>
    [Native(Level.Close)]
    public int AbilityId(string? abilityIdString) => abilityIdString is { Length: 4 } code ? Code(code) : 0;

    [Native(Level.Close)]
    public string? AbilityId2String(int abilityId) => Name(abilityId);

    /// <summary>The id of a named order (any case), 0 for a name the game doesn't give an order (probe orders-table: 373 names).</summary>
    [Native(Level.Exact, Fact = "docs/facts.md, Orders: probe orders-table (11-15); game-facts.md, Orders and events: OrderId2String (10-15, 11-03)")]
    public int OrderId(string? orderIdString) => orderIdString is null ? 0 : OrderIds.GetValueOrDefault(orderIdString);

    /// <summary>The name of an order id; "" for an id with none (852008, 851974), also an id of a unit.</summary>
    [Native(Level.Exact, Fact = "docs/facts.md, Orders: probe orders-table (11-15); game-facts.md, Orders and events: OrderId2String (10-15, 11-03)")]
    public string? OrderId2String(int orderId) => OrderNames.GetValueOrDefault(orderId, "");

    // The animation names of ANIM_TYPE_*
    static readonly string[] AnimNames = ["birth", "death", "decay", "dissipate", "stand", "walk", "attack", "morph", "sleep", "spell", "portrait"];

    [Native(Level.Close)]
    public string? BlzGetAnimName(animtype? whichAnim) => whichAnim is { Id: >= 0 } a && a.Id < AnimNames.Length ? AnimNames[a.Id] : "";

    // The model-art columns of an ability by effect type: effect, target, caster, special, area effect, missile, lightning
    static readonly string[] EffectColumns = ["Effectart", "Targetart", "Casterart", "Specialart", "Areaeffectart", "Missileart", "LightningEffect"];

    /// <summary>The model at the index of the ability's art column for the effect type, "" without one.</summary>
    [Native(Level.Close)]
    public string? GetAbilityEffectById(int abilityId, effecttype? t, int index)
    {
        if (t is not { Id: >= 0 } || t.Id >= EffectColumns.Length || Data.Object(ObjectKind.Ability, abilityId) is not { } type || Data.FieldAt(ObjectKind.Ability, EffectColumns[t.Id]) is not { } field) return "";
        var models = type.Text(field)?.Split(',', StringSplitOptions.TrimEntries) ?? [];
        return index >= 0 && index < models.Length ? models[index] : "";
    }

    [Native(Level.Close)]
    public string? GetAbilityEffect(string? abilityString, effecttype? t, int index) => GetAbilityEffectById(AbilityId(abilityString), t, index);

    // ---- The player's screen and input

    [Native(Level.Close)] public bool BlzIsSelectionEnabled() => Setting("selection", true);
    [Native(Level.Close)] public bool BlzIsSelectionCircleEnabled() => Setting("circle", true);
    [Native(Level.Close)] public bool BlzIsTargetIndicatorEnabled() => Setting("indicator", true);
    [Native(Level.Close)] public void BlzEnableSelections(bool enableSelection, bool enableSelectionCircle) { _settings["selection"] = enableSelection; _settings["circle"] = enableSelectionCircle; }
    [Native(Level.Close)] public void BlzEnableTargetIndicator(bool enable) => _settings["indicator"] = enable;
    [Native(Level.Close)] public float BlzGetHUDScale() => Setting("hud", 1f);
    [Native(Level.Close)] public void BlzSetHUDScale(int percentage) => _settings["hud"] = percentage / 100f;
    [Native(Level.Close)] public int BlzGetMinShadowCastingPointLightCount() => Setting("lights", 0);
    [Native(Level.Close)] public void BlzSetMinShadowCastingPointLightCount(int count) => _settings["lights"] = count;
    [Native(Level.Close)] public int BlzGetLocalClientWidth() => 1920;
    [Native(Level.Close)] public int BlzGetLocalClientHeight() => 1080;
    [Native(Level.Close)] public string? BlzGetLocale() => "enUS";
    [Native(Level.Close)] public bool BlzIsLocalClientActive() => true;

    /// <summary>The player's living town halls of any race and tier, one being built too; the melee script reveals a player that has none.</summary>
    [Native(Level.Close)] public int BlzGetPlayerTownHallCount(player? whichPlayer) => whichPlayer is null ? 0 : Mine(whichPlayer).Count(IsTownHall);

    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzEnableCursor(bool enable) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzEnableUIAutoPosition(bool enable) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzLockHUDScale() { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzUnlockHUDScale() { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzStartRecording(int fps) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzEndRecording() { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzDisplayChatMessage(player? whichPlayer, int recipient, string? message) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void BlzSetMousePos(int x, int y) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableDragSelect(bool state, bool ui) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnablePreSelect(bool state, bool ui) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableSelect(bool state, bool ui) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableMinimapFilterButtons(bool enableAlly, bool enableCreep) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableOcclusion(bool flag) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableUserControl(bool b) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void EnableUserUI(bool b) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void ForceUICancel() { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void ForceUIKey(string? key) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void ShowInterface(bool flag, float fadeDuration) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void SetReservedLocalHeroButtons(int reserved) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void SetPlayerOnScoreScreen(player? whichPlayer, bool flag) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void CachePlayerHeroData(player? whichPlayer) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void DoNotSaveReplay() { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void DisableRestartMission(bool flag) { }
    [Native(Level.Recorded, Reason = SettingsNote)] public void DisplayLoadDialog() { }
    [Native(Level.Recorded, Reason = "A cheat code typed into the game; nothing here acts on it")] public void Cheat(string? cheatStr) { }
    [Native(Level.Recorded, Reason = "The cinematic panels and model cinematics are drawn; no native reads them back")] public void BlzHideCinematicPanels(bool enable) { }
    [Native(Level.Recorded, Reason = "The cinematic panels and model cinematics are drawn; no native reads them back")] public void BlzSetCinematicEnabledDE(bool enable) { }
    [Native(Level.Recorded, Reason = "The cinematic panels and model cinematics are drawn; no native reads them back")] public void BlzPlayModelCinematicGameAtPosition(string? modelName, float posX, float posY, float posZ, float RotZ) { }
    [Native(Level.Close)] public bool BlzPreloadModelCinematicGame(string? modelName) => true;
    [Native(Level.Close)] public int BlzGetModelCinematicGameCurrentShot() => 0;
    [Native(Level.Close)] public float BlzGetModelCinematicGameRemainingTime() => 0;
    [Native(Level.Close)] public int BlzGetModelCinematicGameShotCount() => 0;

    [Native(Level.Recorded, Reason = "The automation harness of the game's own tests; nothing here runs it")] public void AutomationSetTestType(string? testType) { }
    [Native(Level.Recorded, Reason = "The automation harness of the game's own tests; nothing here runs it")] public void AutomationTestEnd() { }
    [Native(Level.Recorded, Reason = "The automation harness of the game's own tests; nothing here runs it")] public void AutomationTestStart(string? testName) { }
    [Native(Level.Recorded, Reason = "The automation harness of the game's own tests; nothing here runs it")] public void AutomationTestingFinished() { }

    // ---- The game's options, kept as set

    [Native(Level.Close)] public int GetAllyColorFilterState() => Setting("ally", 0);
    [Native(Level.Close)] public void SetAllyColorFilterState(int state) => _settings["ally"] = state;
    [Native(Level.Close)] public bool GetCreepCampFilterState() => Setting("creeps", false);
    [Native(Level.Close)] public void SetCreepCampFilterState(bool state) => _settings["creeps"] = state;
    [Native(Level.Close)] public bool GetCustomCampaignButtonVisible(int whichButton) => Setting($"campaign{whichButton}", false);
    [Native(Level.Close)] public void SetCustomCampaignButtonVisible(int whichButton, bool visible) => _settings[$"campaign{whichButton}"] = visible;
    [Native(Level.Close)] public gamedifficulty? GetDefaultDifficulty() => Convert<gamedifficulty>(Setting("difficulty", 1));
    [Native(Level.Close)] public void SetDefaultDifficulty(gamedifficulty? g) { if (g is not null) _settings["difficulty"] = g.Id; }
    [Native(Level.Close)] public mapdensity? GetCreatureDensity() => Convert<mapdensity>(Setting("creature", 0));
    [Native(Level.Close)] public void SetCreatureDensity(mapdensity? whichdensity) { if (whichdensity is not null) _settings["creature"] = whichdensity.Id; }
    [Native(Level.Close)] public mapdensity? GetResourceDensity() => Convert<mapdensity>(Setting("resource", 0));
    [Native(Level.Close)] public void SetResourceDensity(mapdensity? whichdensity) { if (whichdensity is not null) _settings["resource"] = whichdensity.Id; }
    [Native(Level.Close)] public bool IsGameTypeSupported(gametype? whichGameType) => whichGameType is not null && Setting($"type{whichGameType.Id}", false);
    [Native(Level.Close)] public void SetGameTypeSupported(gametype? whichGameType, bool value) { if (whichGameType is not null) _settings[$"type{whichGameType.Id}"] = value; }

    /// <summary>The percentage one player gives another of a resource each pay, as set; 0 until it is.</summary>
    [Native(Level.Close)]
    public int GetPlayerTaxRate(player? sourcePlayer, player? otherPlayer, playerstate? whichResource) =>
        sourcePlayer is null || otherPlayer is null || whichResource is null ? 0 : Setting($"tax{sourcePlayer.GetHashCode()},{otherPlayer.GetHashCode()},{whichResource.Id}", 0);

    [Native(Level.Close)]
    public void SetPlayerTaxRate(player? sourcePlayer, player? otherPlayer, playerstate? whichResource, int rate)
    {
        if (sourcePlayer is not null && otherPlayer is not null && whichResource is not null)
            _settings[$"tax{sourcePlayer.GetHashCode()},{otherPlayer.GetHashCode()},{whichResource.Id}"] = rate;
    }

    /// <summary>Close: the scores come with the game's counting (waves 3 and 6); 0 until then.</summary>
    [Native(Level.Close)] public int GetPlayerScore(player? whichPlayer, playerscore? whichPlayerScore) => 0;

    /// <summary>The difficulty the run seats its computers at, Normal unless it says otherwise (decisions D141, D430); Close.</summary>
    [Native(Level.Close)] public aidifficulty? GetAIDifficulty(player? num) => Convert<aidifficulty>(Difficulty);

    [Native(Level.Close)] public player? GetTournamentFinishNowPlayer() => null;
    [Native(Level.Close)] public int GetTournamentFinishNowRule() => 0;
    [Native(Level.Close)] public float GetTournamentFinishSoonTimeRemaining() => 0;
    [Native(Level.Close)] public int GetTournamentScore(player? whichPlayer) => 0;

    /// <summary>Both game versions are supported by the installed game and compatible with it.</summary>
    [Native(Level.Close)] public bool VersionCompatible(version? whichVersion) => whichVersion is not null;
    [Native(Level.Close)] public bool VersionSupported(version? whichVersion) => whichVersion is not null;

    // ---- Saves, levels and campaigns, which a test run doesn't have

    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetCampaignAvailable(int campaignNumber, bool available) { }
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetCampaignMenuRace(race? r) { }
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetCampaignMenuRaceEx(int campaignIndex) { }
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetEdCinematicAvailable(int campaignNumber, bool available) { }
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetMissionAvailable(int campaignNumber, int missionNumber, bool available) { }
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetOpCinematicAvailable(int campaignNumber, bool available) { }
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void SetTutorialCleared(bool cleared) { }
    [Native(Level.Recorded, Reason = "A campaign menu setting; no native reads it back")] public void ForceCampaignSelectScreen() { }
    [Native(Level.Recorded, Reason = "Saved games are the game's files; a test run keeps none")] public void SaveGame(string? saveFileName) { }
    [Native(Level.Recorded, Reason = "Saved games are the game's files; a test run keeps none")] public void SaveGameCheckpoint(string? saveFileName, bool showWindow) { }
    [Native(Level.Recorded, Reason = "Saved games are the game's files; a test run keeps none")] public void SetMaxCheckpointSaves(int maxCheckpointSaves) { }
    [Native(Level.Close)] public bool SaveGameExists(string? saveName) => false;
    [Native(Level.Close)] public bool CopySaveGame(string? sourceSaveName, string? destSaveName) => false;
    [Native(Level.Close)] public bool RemoveSaveDirectory(string? sourceDirName) => false;
    [Native(Level.Close)] public bool RenameSaveDirectory(string? sourceDirName, string? destDirName) => false;
    [Native(Level.Close)] public string? GetSaveBasicFilename() => "";
    [Native(Level.Close)] public string? SkinManagerGetLocalPath(string? key) => key;

    /// <summary>Ends the run as EndGame does: the next level, the restart and the load are other maps' runs, which this one doesn't make.</summary>
    [Native(Level.Close, Fact = "game-facts.md, Map and game: ChangeLevel ends this map's script")]
    public void ChangeLevel(string? newLevel, bool doScoreScreen) => EndGame(doScoreScreen);

    [Native(Level.Close)] public void RestartGame(bool doScoreScreen) => EndGame(doScoreScreen);
    [Native(Level.Close)] public void ReloadGame() => EndGame(false);
    [Native(Level.Close)] public void LoadGame(string? saveFileName, bool doScoreScreen) => EndGame(doScoreScreen);

    /// <summary>Close: the unit made from the name its data gives, nil for a name no unit has.</summary>
    [Native(Level.Close)]
    public unit? CreateUnitAtLocByName(player? id, string? unitname, location? whichLocation, float face) =>
        whichLocation is null || unitname is null || Data.Id(ObjectKind.Unit, unitname) is not { } unitId ? null : CreateUnit(id, unitId, whichLocation.X, whichLocation.Y, face);

    /// <summary>Close: movement comes with wave 3; kept as the unit's flag.</summary>
    [Native(Level.Recorded, Reason = "Whether a unit collides on its walk; movement comes with wave 3")] public void SetUnitPathing(unit? whichUnit, bool flag) { }

    [Native(Level.Recorded, Reason = "Construction and upgrade progress are drawn; they come with buildings (wave 3)")] public void UnitSetConstructionProgress(unit? whichUnit, int constructionPercentage) { }
    [Native(Level.Recorded, Reason = "Construction and upgrade progress are drawn; they come with buildings (wave 3)")] public void UnitSetUpgradeProgress(unit? whichUnit, int upgradePercentage) { }

    [Native(Level.Recorded, Reason = "Crippling hides an opponent's view of the player; visibility comes with wave 6")] public void CripplePlayer(player? whichPlayer, force? toWhichPlayers, bool flag) { }

    /// <summary>The cliff level of the corner nearest the point, as the ground's tiles give it; 0 off the map.</summary>
    [Native(Level.Close)]
    public int GetTerrainCliffLevel(float x, float y) => NearestCorner(x, y) is var (column, row) && Data.Terrain is { } terrain ? terrain.CliffLevel(column, row) : 0;

    /// <summary>Close: read as IsTerrainPathable's opposite (true when pathable); not measured.</summary>
    [Native(Level.Close)]
    public bool BlzIsTerrainPathableEx(float x, float y, pathingtype? t) => !IsTerrainPathable(x, y, t);
}
