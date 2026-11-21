namespace MirrorImaged.Natives;

// The natives of the computer players' AI (common.ai and the AI commands of common.j) that are not modelled. The melee scripts set how the AI
// fights (fleeing, repairing, targeting heroes) through settings nothing reads back, which are Recorded; the rest are the campaign scripts'
// (guard posts, defenders, suicide waves, commands from the map, zeppelins) and answer as an AI with nothing to count or command does: counts
// read 0, questions false, commands do nothing (decisions D431). What the melee scripts' choices rest on is in AiTown.cs.
public sealed partial class GameNatives
{
    const string DebugText = "Shows a text of the AI script to its developer";
    const string EngineSetting = "; read from its name and how common.ai uses it, not measured (D797)";

    // The commands a map sent a player, a plain queue the player's script reads from the front (D798)
    readonly Dictionary<player, Queue<(int Command, int Data)>> _commands = [];
    const string CommandOrder = "Which of several commands is the last is not measured (a map's JASS cannot call these natives, probe ai-commands); the oldest is taken (D798)";

    [Native(Level.Close, Reason = CommandOrder)]
    public void CommandAI(player? num, int command, int data)
    {
        if (num is null) return;
        if (!_commands.TryGetValue(num, out var queue)) _commands[num] = queue = [];
        queue.Enqueue((command, data));
    }

    // Close: a paused AI stopped training and building for its 15 s while its workers went on harvesting, and began again when unpaused
    // (game-facts, probe-ai-game-ai); the simulation holds the script's threads, whose assignments of workers stand meanwhile
    [Native(Level.Close, Fact = "game-facts.md, Orders and events: PauseCompAI (11-14, probe-ai-game-ai)")]
    public void PauseCompAI(player? p, bool pause)
    {
        if (p is null) return;
        if (pause) { _pausedAi.Add(p); return; }
        _pausedAi.Remove(p);
        if (_heldAi.Remove(p, out var held)) foreach (var thread in held) ResumeAi(p, thread, 0);
    }

    [Native(Level.Recorded, Reason = "Campaign: the engine keeps the number of units of the type ready as defenders at the guard posts, and answers whether it could; the simulation keeps no defenders and answers false, so a map sees no unit made (D798)")]
    public bool AddDefenders(int qty, int id) => false;

    [Native(Level.Recorded, Reason = "Campaign: marks a place where the engine keeps a unit of the type on guard for FillGuardPosts; the simulation keeps no guard posts (D798)")]
    public void AddGuardPost(int id, float x, float y) { }

    [Native(Level.Close, Reason = CommandOrder)]
    public int CommandsWaiting() => _commands.GetValueOrDefault(Ai)?.Count ?? 0;

    [Native(Level.Recorded, Reason = DebugText)]
    public void DebugFI(string? str, int val) { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DebugS(string? str) { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DebugUnitID(string? str, int val) { }

    [Native(Level.Recorded, Reason = "Switches off the game AI engine's own pathing for the AI player's groups; no shipped script calls it, and the simulation's groups walk by the unit pathing alone" + EngineSetting)]
    public void DisablePathing() { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DisplayText(int p, string? str) { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DisplayTextI(int p, string? str, int val) { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DisplayTextII(int p, string? str, int v1, int v2) { }

    [Native(Level.Recorded, Reason = DebugText)]
    public void DisplayTextIII(int p, string? str, int v1, int v2, int v3) { }

    [Native(Level.Close, Reason = "False: a developer's switch for the script's debug texts (DisplayText, Debug*), off in the retail game; not measured, as no map can call it (D798)")]
    public bool DoAiScriptDebug() => false;

    [Native(Level.Recorded, Reason = "Campaign: the engine trains units of the types AddGuardPost named and sends them to their posts; the simulation trains none (D798)")]
    public void FillGuardPosts() { }

    [Native(Level.Close, Reason = CommandOrder)]
    public int GetLastCommand() => _commands.GetValueOrDefault(Ai)?.TryPeek(out var next) == true ? next.Command : 0;

    [Native(Level.Close, Reason = CommandOrder)]
    public int GetLastData() => _commands.GetValueOrDefault(Ai)?.TryPeek(out var next) == true ? next.Data : 0;

    [Native(Level.Close, Reason = "Nil: no enemy is ever weak enough for the all-out attack the script makes on a mega target; the game's rule for one is not known (D431, D798)")]
    public unit? GetMegaTarget() => null;

    [Native(Level.Recorded, Reason = "Whether the engine lets units with a timed life (summons) join the AI's groups; common.ai's campaign setup turns it off. The simulation's wave takes units by type and does not look at their timed life" + EngineSetting)]
    public void GroupTimedLife(bool allow) { }

    [Native(Level.Close, Reason = "0: the units of the type the engine keeps out of the AI's counts (campaign); the simulation sets none aside (D798)")]
    public int IgnoredUnits(int unitid) => 0;

    [Native(Level.Recorded, Reason = "Campaign: the engine loads the next attack wave into a zeppelin bound for the place; the simulation has no zeppelin loading, the wave walks (D798)")]
    public void LoadZepWave(int x, int y) { }

    [Native(Level.Close, Reason = CommandOrder)]
    public void PopLastCommand() => _commands.GetValueOrDefault(Ai)?.TryDequeue(out _);

    [Native(Level.Recorded, Reason = "The engine buys a zeppelin for the AI; the melee script asks only on a map it finds no way across, which a simulated run does not meet (D431, D798)")]
    public void PurchaseZeppelin() { }

    [Native(Level.Recorded, Reason = "The engine takes injured units out of the attack group; the simulation's wave is the units of the types the script asked for, injured or not (D431, D798)")]
    public void RemoveInjuries() { }

    [Native(Level.Recorded, Reason = "The engine takes siege units out of the attack group; the simulation's wave is the units of the types the script asked for (D431, D798)")]
    public void RemoveSiege() { }

    [Native(Level.Recorded, Reason = "Campaign: the engine sends the units on guard back to their posts; the simulation keeps no guard posts (D798)")]
    public void ReturnGuardPosts() { }

    [Native(Level.Recorded, Reason = "Tells the engine the AI's groups may cross water; no shipped script calls it, and the simulation sends a group by the unit pathing alone" + EngineSetting)]
    public void SetAmphibious() { }

    [Native(Level.Recorded, Reason = "Tells the engine to play as the campaign AI, which common.ai pairs with fleeing off and slow chopping; the simulation has one AI, the script, so there is no engine mode to change" + EngineSetting)]
    public void SetCampaignAI() { }

    [Native(Level.Recorded, Reason = "Whether the engine sends the AI's idle units to defend its buildings and units when they are attacked; common.ai's melee setup turns it on. The simulation's units fight what comes in their own range only" + EngineSetting)]
    public void SetDefendPlayer(bool state) { }

    [Native(Level.Recorded, Reason = "Whether the engine pulls a whole group of the AI back from a fight it is losing; on in melee (off for the newbie AI), off in campaign. A wave in the simulation keeps the orders the script gave it" + EngineSetting)]
    public void SetGroupsFlee(bool state) { }

    [Native(Level.Recorded, Reason = "Whether the engine sends the AI's heroes to a shop to buy items with its gold; on in melee (off for the newbie AI). The simulation's AI heroes buy only by an order of the script, and no shipped script gives one" + EngineSetting)]
    public void SetHeroesBuyItems(bool state) { }

    [Native(Level.Recorded, Reason = "Whether the engine pulls the AI's hurt heroes out of a fight; on in melee, off in campaign. The simulation's heroes keep the orders the script gave them" + EngineSetting)]
    public void SetHeroesFlee(bool state) { }

    [Native(Level.Recorded, Reason = "Whether the engine sends the AI's heroes to pick up items on the ground; common.ai's melee setup turns it on. The simulation's AI heroes take an item only by an order of the script" + EngineSetting)]
    public void SetHeroesTakeItems(bool state) { }

    [Native(Level.Recorded, Reason = "Whether the engine leaves injured units out when it forms an attack group; common.ai's melee setup turns it on (RemoveInjuries does the same to the wave). The simulation's wave takes units by type, injured or not" + EngineSetting)]
    public void SetIgnoreInjured(bool state) { }

    [Native(Level.Recorded, Reason = "Tells the engine to play as the melee AI, with the settings common.ai then makes; the simulation has one AI, the script, so there is no engine mode to change" + EngineSetting)]
    public void SetMeleeAI() { }

    [Native(Level.Recorded, Reason = "Whether the engine trains a new hero by itself when the AI has none; common.ai's campaign setup turns it off. The simulation trains a hero only when the script asks for one" + EngineSetting)]
    public void SetNewHeroes(bool state) { }

    [Native(Level.Recorded, Reason = "Whether the AI's workers repair damaged buildings by themselves; on in melee and in the campaign's hard difficulty. The simulation's workers repair only by an order of the script, and no shipped script gives one" + EngineSetting)]
    public void SetPeonsRepair(bool state) { }

    [Native(Level.Recorded, Reason = "Whether the engine walks a group by a random path to its goal; no shipped script calls it, and the simulation walks by the unit pathing alone" + EngineSetting)]
    public void SetRandomPaths(bool state) { }

    [Native(Level.Recorded, Reason = "How many units the engine keeps ready to refill a group from its losses (common.ai's SetReplacements gives one number by difficulty). The simulation makes only what the script asks for" + EngineSetting)]
    public void SetReplacementCount(int qty) { }

    [Native(Level.Recorded, Reason = "Whether the engine paces the AI's lumber workers slowly; common.ai's campaign setup turns it on. The simulation sends cutters to trees only when the script calls HarvestWood" + EngineSetting)]
    public void SetSlowChopping(bool state) { }

    [Native(Level.Recorded, Reason = "Whether the engine picks the targets of the AI's siege units itself; on in melee (off for the newbie AI). The simulation's siege units attack what they are ordered to or what comes in range" + EngineSetting)]
    public void SetSmartArtillery(bool state) { }

    [Native(Level.Recorded, Reason = "The place where the engine gathers a group before an attack; no shipped script calls it, and the simulation gathers the wave where the script sends it" + EngineSetting)]
    public void SetStagePoint(float x, float y) { }

    [Native(Level.Recorded, Reason = "Whether the engine has the AI's attackers prefer enemy heroes as targets; on in melee (off for the newbie AI and campaign easy and normal). The simulation's units choose targets by their own acquisition" + EngineSetting)]
    public void SetTargetHeroes(bool state) { }

    [Native(Level.Recorded, Reason = "Whether the engine pulls the AI's single units out of a fight they are losing; on in melee (off for the newbie AI and campaign easy and normal). The simulation's units keep their orders" + EngineSetting)]
    public void SetUnitsFlee(bool state) { }

    [Native(Level.Recorded, Reason = "Whether the engine looks for an enemy weak enough to send an all-out attack at, which GetMegaTarget then answers; common.ai's melee setup turns it on. The simulation answers no such target (D431)" + EngineSetting)]
    public void SetWatchMegaTargets(bool state) { }

    [Native(Level.Recorded, Reason = "Moves the place the engine takes as the town's centre for its building and defence; no shipped script calls it, and the simulation finds the town from its halls and mines" + EngineSetting)]
    public void ShiftTownSpot(float x, float y) { }

    [Native(Level.Recorded, Reason = "InitAI calls it to stop the AI's workers gathering before the script assigns them; HarvestGold and HarvestWood in the simulation keep a worker that already does the work asked for and send the others, so a script that assigns every worker ends the same (D798)")]
    public void StopGathering() { }

    [Native(Level.Recorded, Reason = "Campaign: sends the whole army at the player's buildings until none stands, answering true when done; the simulation sends no suicide wave and answers false, so the script keeps waiting (D798)")]
    public bool SuicidePlayer(player? id, bool check_full) => false;

    [Native(Level.Recorded, Reason = "Campaign: sends the whole army at the player's units until none stands, answering true when done; the simulation sends no suicide wave and answers false, so the script keeps waiting (D798)")]
    public bool SuicidePlayerUnits(player? id, bool check_full) => false;

    [Native(Level.Recorded, Reason = "Campaign: the engine sends the number of units of the type at the enemy to die; the simulation sends none (D798)")]
    public void SuicideUnit(int count, int unitid) { }

    [Native(Level.Recorded, Reason = "Campaign: the engine sends the number of units of the type at the player's units to die; the simulation sends none (D798)")]
    public void SuicideUnitEx(int ct, int uid, int pid) { }

    [Native(Level.Recorded, Reason = "Campaign: the engine unsummons the building (UnsummonAll does each of the player's in turn); the simulation leaves it standing (D798)")]
    public void Unsummon(unit? unitid) { }
}
