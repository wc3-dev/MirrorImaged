namespace MirrorImaged.Natives;

// The natives of the computer players AI (common.ai and the AI commands of common.j). The melee and campaign AI scripts run on the
// economy, the build orders and the fight of waves 3 and 4, which are not simulated yet (wave 7 brings them together), so these
// answer as an AI with nothing to count or command does: counts read 0, questions false, commands do nothing.
public sealed partial class GameNatives
{
    [Native(Level.Close)]
    public void CommandAI(player? num, int command, int data) { }

    [Native(Level.Close)]
    public void PauseCompAI(player? p, bool pause) { }

    [Native(Level.Close)]
    public void RecycleGuardPosition(unit? hUnit) { }

    [Native(Level.Close)]
    public void RemoveAllGuardPositions(player? num) { }

    [Native(Level.Close)]
    public void RemoveGuardPosition(unit? hUnit) { }

    [Native(Level.Close)]
    public void StartCampaignAI(player? num, string? script) { }

    [Native(Level.Close)]
    public void StartMeleeAI(player? num, string? script) { }

    [Native(Level.Close)]
    public bool AddAssault(int qty, int id) => false;

    [Native(Level.Close)]
    public bool AddDefenders(int qty, int id) => false;

    [Native(Level.Close)]
    public void AddGuardPost(int id, float x, float y) { }

    [Native(Level.Close)]
    public void AttackMoveKill(unit? target) { }

    [Native(Level.Close)]
    public void AttackMoveXY(int x, int y) { }

    [Native(Level.Close)]
    public bool CaptainAtGoal() => false;

    [Native(Level.Close)]
    public void CaptainAttack(float x, float y) { }

    [Native(Level.Close)]
    public void CaptainGoHome() { }

    [Native(Level.Close)]
    public int CaptainGroupSize() => 0;

    [Native(Level.Close)]
    public bool CaptainInCombat(bool attack_captain) => false;

    [Native(Level.Close)]
    public bool CaptainIsEmpty() => false;

    [Native(Level.Close)]
    public bool CaptainIsFull() => false;

    [Native(Level.Close)]
    public bool CaptainIsHome() => false;

    [Native(Level.Close)]
    public int CaptainReadiness() => 0;

    [Native(Level.Close)]
    public int CaptainReadinessHP() => 0;

    [Native(Level.Close)]
    public int CaptainReadinessMa() => 0;

    [Native(Level.Close)]
    public bool CaptainRetreating() => false;

    [Native(Level.Close)]
    public void CaptainVsPlayer(player? id) { }

    [Native(Level.Close)]
    public void CaptainVsUnits(player? id) { }

    [Native(Level.Close)]
    public void ClearCaptainTargets() { }

    [Native(Level.Close)]
    public void ClearHarvestAI() { }

    [Native(Level.Close)]
    public int CommandsWaiting() => 0;

    [Native(Level.Close)]
    public bool ConvertUnits(int qty, int id) => false;

    [Native(Level.Close)]
    public void CreateCaptains() { }

    [Native(Level.Close)]
    public bool CreepsOnMap() => false;

    [Native(Level.Close)]
    public void DebugFI(string? str, int val) { }

    [Native(Level.Close)]
    public void DebugS(string? str) { }

    [Native(Level.Close)]
    public void DebugUnitID(string? str, int val) { }

    [Native(Level.Close)]
    public void DisablePathing() { }

    [Native(Level.Close)]
    public void DisplayText(int p, string? str) { }

    [Native(Level.Close)]
    public void DisplayTextI(int p, string? str, int val) { }

    [Native(Level.Close)]
    public void DisplayTextII(int p, string? str, int v1, int v2) { }

    [Native(Level.Close)]
    public void DisplayTextIII(int p, string? str, int v1, int v2, int v3) { }

    [Native(Level.Close)]
    public bool DoAiScriptDebug() => false;

    [Native(Level.Close)]
    public void FillGuardPosts() { }

    [Native(Level.Close)]
    public int GetAiPlayer() => 0;

    [Native(Level.Close)]
    public unit? GetAllianceTarget() => null;

    [Native(Level.Close)]
    public unit? GetBuilding(player? p) => null;

    [Native(Level.Close)]
    public unit? GetCreepCamp(int min, int max, bool flyers_ok) => null;

    [Native(Level.Close)]
    public unit? GetEnemyBase() => null;

    [Native(Level.Close)]
    public unit? GetEnemyExpansion() => null;

    [Native(Level.Close)]
    public int GetEnemyPower() => 0;

    [Native(Level.Close)]
    public unit? GetExpansionFoe() => null;

    [Native(Level.Close)]
    public unit? GetExpansionPeon() => null;

    [Native(Level.Close)]
    public int GetExpansionX() => 0;

    [Native(Level.Close)]
    public int GetExpansionY() => 0;

    [Native(Level.Close)]
    public int GetGoldOwned() => 0;

    [Native(Level.Close)]
    public int GetHeroId() => 0;

    [Native(Level.Close)]
    public int GetHeroLevelAI() => 0;

    [Native(Level.Close)]
    public int GetLastCommand() => 0;

    [Native(Level.Close)]
    public int GetLastData() => 0;

    [Native(Level.Close)]
    public unit? GetMegaTarget() => null;

    [Native(Level.Close)]
    public int GetMinesOwned() => 0;

    [Native(Level.Close)]
    public int GetNextExpansion() => 0;

    [Native(Level.Close)]
    public int GetPlayerUnitTypeCount(player? p, int unitid) => 0;

    [Native(Level.Close)]
    public int GetTownUnitCount(int id, int tn, bool dn) => 0;

    [Native(Level.Close)]
    public int GetUnitBuildTime(int unitid) => 0;

    [Native(Level.Close)]
    public int GetUnitCount(int unitid) => 0;

    [Native(Level.Close)]
    public int GetUnitCountDone(int unitid) => 0;

    [Native(Level.Close)]
    public int GetUnitGoldCost(int unitid) => 0;

    [Native(Level.Close)]
    public int GetUnitWoodCost(int unitid) => 0;

    [Native(Level.Close)]
    public int GetUpgradeGoldCost(int id) => 0;

    [Native(Level.Close)]
    public int GetUpgradeLevel(int id) => 0;

    [Native(Level.Close)]
    public int GetUpgradeWoodCost(int id) => 0;

    [Native(Level.Close)]
    public void GroupTimedLife(bool allow) { }

    [Native(Level.Close)]
    public void HarvestGold(int town, int peons) { }

    [Native(Level.Close)]
    public void HarvestWood(int town, int peons) { }

    [Native(Level.Close)]
    public int IgnoredUnits(int unitid) => 0;

    [Native(Level.Close)]
    public void InitAssault() { }

    [Native(Level.Close)]
    public bool IsTowered(unit? target) => false;

    [Native(Level.Close)]
    public void LoadZepWave(int x, int y) { }

    [Native(Level.Close)]
    public int MeleeDifficulty() => 0;

    [Native(Level.Close)]
    public bool MergeUnits(int qty, int a, int b, int make) => false;

    [Native(Level.Close)]
    public void PopLastCommand() { }

    [Native(Level.Close)]
    public void PurchaseZeppelin() { }

    [Native(Level.Close)]
    public void RemoveInjuries() { }

    [Native(Level.Close)]
    public void RemoveSiege() { }

    [Native(Level.Close)]
    public void ResetCaptainLocs() { }

    [Native(Level.Close)]
    public void ReturnGuardPosts() { }

    [Native(Level.Close)]
    public void SetAllianceTarget(unit? id) { }

    [Native(Level.Close)]
    public void SetAmphibious() { }

    [Native(Level.Close)]
    public void SetCampaignAI() { }

    [Native(Level.Close)]
    public void SetCaptainChanges(bool allow) { }

    [Native(Level.Close)]
    public void SetCaptainHome(int which, float x, float y) { }

    [Native(Level.Close)]
    public void SetDefendPlayer(bool state) { }

    [Native(Level.Close)]
    public bool SetExpansion(unit? peon, int id) => false;

    [Native(Level.Close)]
    public void SetGroupsFlee(bool state) { }

    [Native(Level.Close)]
    public void SetHeroLevels(Func<int>? func) { }

    [Native(Level.Close)]
    public void SetHeroesBuyItems(bool state) { }

    [Native(Level.Close)]
    public void SetHeroesFlee(bool state) { }

    [Native(Level.Close)]
    public void SetHeroesTakeItems(bool state) { }

    [Native(Level.Close)]
    public void SetIgnoreInjured(bool state) { }

    [Native(Level.Close)]
    public void SetMeleeAI() { }

    [Native(Level.Close)]
    public void SetNewHeroes(bool state) { }

    [Native(Level.Close)]
    public void SetPeonsRepair(bool state) { }

    [Native(Level.Close)]
    public bool SetProduce(int qty, int id, int town) => false;

    [Native(Level.Close)]
    public void SetRandomPaths(bool state) { }

    [Native(Level.Close)]
    public void SetReplacementCount(int qty) { }

    [Native(Level.Close)]
    public void SetSlowChopping(bool state) { }

    [Native(Level.Close)]
    public void SetSmartArtillery(bool state) { }

    [Native(Level.Close)]
    public void SetStagePoint(float x, float y) { }

    [Native(Level.Close)]
    public void SetTargetHeroes(bool state) { }

    [Native(Level.Close)]
    public void SetUnitsFlee(bool state) { }

    [Native(Level.Close)]
    public bool SetUpgrade(int id) => false;

    [Native(Level.Close)]
    public void SetWatchMegaTargets(bool state) { }

    [Native(Level.Close)]
    public void ShiftTownSpot(float x, float y) { }

    [Native(Level.Close)]
    public void Sleep(float seconds) { }

    [Native(Level.Close)]
    public void StartGetEnemyBase() { }

    [Native(Level.Close)]
    public void StartThread(Action? func) { }

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
    public void TeleportCaptain(float x, float y) { }

    [Native(Level.Close)]
    public bool TownHasHall(int townid) => false;

    [Native(Level.Close)]
    public bool TownHasMine(int townid) => false;

    [Native(Level.Close)]
    public bool TownThreatened() => false;

    [Native(Level.Close)]
    public int TownWithMine() => 0;

    [Native(Level.Close)]
    public bool UnitInvis(unit? id) => false;

    [Native(Level.Close)]
    public void Unsummon(unit? unitid) { }

    [Native(Level.Close)]
    public bool WaitGetEnemyBase() => false;
}
