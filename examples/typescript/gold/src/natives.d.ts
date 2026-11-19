// The natives the map calls. A larger map takes these from a types package for the game's natives.
declare interface timer { readonly __timer: never }
declare interface trigger { readonly __trigger: never }
declare interface player { readonly __player: never }
declare interface playerstate { readonly __playerstate: never }

declare const PLAYER_STATE_RESOURCE_GOLD: playerstate;

declare function Player(slot: number): player;
declare function GetPlayerState(whichPlayer: player, whichState: playerstate): number;
declare function SetPlayerState(whichPlayer: player, whichState: playerstate, value: number): void;
declare function CreateTimer(): timer;
declare function TimerStart(whichTimer: timer, timeout: number, periodic: boolean, handler: () => void): void;
declare function CreateTrigger(): trigger;
declare function TriggerRegisterPlayerChatEvent(whichTrigger: trigger, whichPlayer: player, message: string, exact: boolean): void;
declare function TriggerAddAction(whichTrigger: trigger, action: () => void): void;
declare function print(...values: unknown[]): void;
