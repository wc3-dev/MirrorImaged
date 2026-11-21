-- The SyncStored natives of each kind on one machine: a value stored, read, synced and read again at once, a step
-- later and after a second, and a key that was never stored.
local function show(v) return v == nil and "nil" or tostring(v) end
local cache = InitGameCache("mirror-sync.w3v")
StoreInteger(cache, "m", "i", 42)
StoreReal(cache, "m", "r", 1.5)
StoreBoolean(cache, "m", "b", true)
StoreString(cache, "m", "s", "text")
local unit = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
StoreUnit(cache, "m", "u", unit)
local function read(label)
  out(label .. " " .. GetStoredInteger(cache, "m", "i") .. " " .. GetStoredReal(cache, "m", "r") .. " " .. show(GetStoredBoolean(cache, "m", "b")) .. " "
    .. GetStoredString(cache, "m", "s") .. " " .. show(HaveStoredUnit(cache, "m", "u")) .. " missing " .. GetStoredInteger(cache, "m", "none") .. " "
    .. show(HaveStoredInteger(cache, "m", "i")) .. " " .. show(HaveStoredInteger(cache, "m", "none")))
end
read("stored")
SyncStoredInteger(cache, "m", "i")
SyncStoredReal(cache, "m", "r")
SyncStoredBoolean(cache, "m", "b")
SyncStoredString(cache, "m", "s")
SyncStoredUnit(cache, "m", "u")
SyncStoredInteger(cache, "m", "none")
SyncStoredInteger(nil, "m", "i")
read("synced")
TimerStart(CreateTimer(), 0, false, guard(function()
  read("a step later")
  TimerStart(CreateTimer(), 1, false, guard(function()
    read("a second later")
    done()
  end))
end))
