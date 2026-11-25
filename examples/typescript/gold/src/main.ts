// The gold map in TypeScript. `npm run build` (TypeScriptToLua) writes the folder map gold.w3x/war3map.lua from it.
function config(): void {}

function main(): void {
  TimerStart(CreateTimer(), 2, true, () => {
    SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD) + 10);
  });
  const asked = CreateTrigger();
  TriggerRegisterPlayerChatEvent(asked, Player(0), "-gold", true);
  TriggerAddAction(asked, () => {
    print("gold", GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD));
  });
}
