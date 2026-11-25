-- A map that pays its player 10 gold every 2 seconds and says so in chat when asked.
function config() end

function main()
    TimerStart(CreateTimer(), 2, true, function()
        SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD) + 10)
    end)
    local asked = CreateTrigger()
    TriggerRegisterPlayerChatEvent(asked, Player(0), "-gold", true)
    TriggerAddAction(asked, function()
        print("gold", GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD))
    end)
end
