// The same map in JASS: MirrorImaged turns it into Lua before it runs, so the tests are the same.
function PayPlayer takes nothing returns nothing
    call SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD) + 10)
endfunction

function SayGold takes nothing returns nothing
    call BJDebugMsg("gold " + I2S(GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD)))
endfunction

function main takes nothing returns nothing
    local trigger asked = CreateTrigger()
    call TimerStart(CreateTimer(), 2, true, function PayPlayer)
    call TriggerRegisterPlayerChatEvent(asked, Player(0), "-gold", true)
    call TriggerAddAction(asked, function SayGold)
endfunction
