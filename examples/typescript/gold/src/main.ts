// The same map in TypeScript, for TypeScriptToLua (tstl): `npx tstl` writes war3map.lua, and the tests are the Lua tests of the Lua example.
export function main(): void {
  TimerStart(CreateTimer(), 2, true, () => {
    SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD) + 10);
  });
  const asked = CreateTrigger();
  TriggerRegisterPlayerChatEvent(asked, Player(0), "-gold", true);
  TriggerAddAction(asked, () => {
    print("gold", GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD));
  });
}
