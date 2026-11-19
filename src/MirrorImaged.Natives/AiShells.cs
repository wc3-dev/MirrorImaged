namespace MirrorImaged.Natives;

// The natives of the computer players' AI (common.ai and the AI commands of common.j) that are not modelled. The melee scripts set how the AI
// fights (fleeing, repairing, targeting heroes) through settings nothing reads back, which are Recorded; the rest are the campaign scripts'
// (guard posts, defenders, suicide waves, commands from the map, zeppelins) and answer as an AI with nothing to count or command does: counts
// read 0, questions false, commands do nothing (docs/decisions.md, D431). What the melee scripts' choices rest on is in AiTown.cs.
public sealed partial class GameNatives
{
    const string AiSetting = "A setting of how the AI fights and works that the simulation does not model; no native reads it back", DebugText = "Shows a text of the AI script to its developer";

    [Native(Level.Close)]
    public void CommandAI(player? num, int command, int data) { }

    [Native(Level.Close)]
    public void PauseCompAI(player? p, bool pause) { }

    [Native(Level.Close)]
    public bool AddDefenders(int qty, int id) => false;

    [Native(Level.Close)]
    public void AddGuardPost(int id, float x, float y) { }

    [Native(Level.Close)]
    public int CommandsWaiting() => 0;

    [Native(Level.Recorded, Reason = DebugText)]
    public void DebugFI(string? str, int val) { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DebugS(string? str) { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DebugUnitID(string? str, int val) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void DisablePathing() { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DisplayText(int p, string? str) { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DisplayTextI(int p, string? str, int val) { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DisplayTextII(int p, string? str, int v1, int v2) { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DisplayTextIII(int p, string? str, int v1, int v2, int v3) { }

    [Native(Level.Close)]
    public bool DoAiScriptDebug() => false;

    [Native(Level.Close)]
    public void FillGuardPosts() { }

    [Native(Level.Close)]
    public int GetLastCommand() => 0;

    [Native(Level.Close)]
    public int GetLastData() => 0;

    [Native(Level.Close)]
    public unit? GetMegaTarget() => null;

    [Native(Level.Recorded, Reason = AiSetting)]
    public void GroupTimedLife(bool allow) { }

    [Native(Level.Close)]
    public int IgnoredUnits(int unitid) => 0;

    [Native(Level.Close)]
    public void LoadZepWave(int x, int y) { }

    [Native(Level.Close)]
    public void PopLastCommand() { }

    [Native(Level.Close)]
    public void PurchaseZeppelin() { }

    [Native(Level.Close)]
    public void RemoveInjuries() { }

    [Native(Level.Close)]
    public void RemoveSiege() { }

    [Native(Level.Close)]
    public void ReturnGuardPosts() { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetAmphibious() { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetCampaignAI() { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetDefendPlayer(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetGroupsFlee(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetHeroesBuyItems(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetHeroesFlee(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetHeroesTakeItems(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetIgnoreInjured(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetMeleeAI() { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetNewHeroes(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetPeonsRepair(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetRandomPaths(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetReplacementCount(int qty) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetSlowChopping(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetSmartArtillery(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetStagePoint(float x, float y) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetTargetHeroes(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetUnitsFlee(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void SetWatchMegaTargets(bool state) { }

    [Native(Level.Recorded, Reason = AiSetting)]
    public void ShiftTownSpot(float x, float y) { }

    [Native(Level.Close)]
    public void StopGathering() { }

    [Native(Level.Close)]
    public bool SuicidePlayer(player? id, bool check_full) => false;

    [Native(Level.Close)]
    public bool SuicidePlayerUnits(player? id, bool check_full) => false;

    [Native(Level.Close)]
    public void SuicideUnit(int count, int unitid) { }

    [Native(Level.Close)]
    public void SuicideUnitEx(int ct, int uid, int pid) { }

    [Native(Level.Close)]
    public void Unsummon(unit? unitid) { }
}
