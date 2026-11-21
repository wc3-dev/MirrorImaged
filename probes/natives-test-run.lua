-- What a test run (one user, no tournament, no saved games, nothing happening) reads from the natives that answer a
-- fixed value or nothing: the player slots, the tournament, the locale and keys, the save file questions, the scores at
-- the start, the model cinematic, the mouse, and the event responses read outside any event.
local function show(v) return v == nil and "nil" or tostring(v) end
local function kind(h) return h == nil and "nil" or tostring(h):match("^[%a_]+") end

out("slots " .. GetBJMaxPlayers() .. " " .. GetPlayerNeutralAggressive() .. " " .. GetBJPlayerNeutralVictim() .. " " .. GetBJPlayerNeutralExtra()
  .. " " .. GetPlayerNeutralPassive())
out("tournament " .. show(GetTournamentFinishNowPlayer()) .. " " .. GetTournamentFinishNowRule() .. " " .. GetTournamentFinishSoonTimeRemaining()
  .. " " .. GetTournamentScore(Player(0)) .. " " .. GetTournamentScore(Player(1)) .. " " .. GetTournamentScore(nil))
out("client " .. BlzGetLocale() .. " " .. show(BlzIsLocalClientActive()))
local keys = {}
for meta = -1, 9 do keys[#keys + 1] = show(BlzIsMetaKeyPressed(meta)) end
out("meta " .. table.concat(keys, " "))
out("mouse " .. show(BlzGetMouseFocusUnit()))
out("save [" .. GetSaveBasicFilename() .. "] " .. show(SaveGameExists("mirror-no-such-save")) .. " " .. show(SaveGameExists("")) .. " copy "
  .. show(CopySaveGame("mirror-no-such-save", "mirror-no-such-copy")) .. " remove " .. show(RemoveSaveDirectory("mirror-no-such-save"))
  .. " rename " .. show(RenameSaveDirectory("mirror-no-such-save", "mirror-no-such-name")))
out("save after [" .. GetSaveBasicFilename() .. "] " .. show(SaveGameExists("mirror-no-such-copy")) .. " " .. show(SaveGameExists("mirror-no-such-name")))

local scores = {}
for i = 0, 25 do scores[#scores + 1] = GetPlayerScore(Player(0), ConvertPlayerScore(i)) end
out("scores player 0 " .. table.concat(scores, " "))
scores = {}
for i = 0, 25 do scores[#scores + 1] = GetPlayerScore(Player(1), ConvertPlayerScore(i)) end
out("scores player 1 " .. table.concat(scores, " "))
out("score nil " .. GetPlayerScore(nil, ConvertPlayerScore(0)) .. " " .. GetPlayerScore(Player(0), nil))

local model = "Objects\\CinematicCameras\\CameraZoomOut.mdx"
out("cinematic " .. BlzGetModelCinematicGameCurrentShot() .. " " .. BlzGetModelCinematicGameRemainingTime() .. " " .. BlzGetModelCinematicGameShotCount())
out("preload " .. show(BlzPreloadModelCinematicGame("mirror-no-such-model")) .. " " .. show(BlzPreloadModelCinematicGame("")) .. " "
  .. show(BlzPreloadModelCinematicGame(model)) .. " " .. show(BlzPreloadModelCinematicGame("Units\\Human\\Footman\\Footman.mdx")) .. " "
  .. show(BlzPreloadModelCinematicGame("units/human/footman/footman.mdx")) .. " " .. show(BlzPreloadModelCinematicGame(" ")) .. " "
  .. show(BlzPreloadModelCinematicGame("Footman")))
out("cinematic after " .. BlzGetModelCinematicGameCurrentShot() .. " " .. BlzGetModelCinematicGameRemainingTime() .. " " .. BlzGetModelCinematicGameShotCount())

out("outside " .. show(GetDetectedUnit()) .. " " .. show(GetEventDetectingPlayer()) .. " " .. show(GetWinningPlayer()) .. " " .. show(GetRescuer())
  .. " " .. show(BlzGetAbsorbingItem()) .. " " .. show(BlzGetManipulatedItemWasAbsorbed()) .. " " .. show(BlzGetStackingItemSource())
  .. " " .. show(BlzGetStackingItemTarget()) .. " " .. BlzGetStackingItemTargetPreviousCharges() .. " " .. show(GetTriggerUnit()))
out("doodads " .. BlzGetNumDoodads() .. " " .. show(BlzGetDoodadIsUsingModelAxes(0)) .. " " .. show(BlzGetDoodadIsUsingModelAxes(-1)))

done()
