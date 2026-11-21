namespace MirrorImaged.Natives;

public partial class gamecache
{
    internal string Name = "";
    internal Dictionary<(char Type, string Mission, string Key), object> Values = [];
}

// Game caches, as the files probe measured them (decisions D103, D104): one cache a name, which every InitGameCache of
// it gives a new handle to; each type's values apart, missions and keys in any case; a game starts with the caches
// saved in its documents, SaveGameCache writes them all, and ReloadGameCachesFromDisk puts the saved ones back in
// place. A handle whose cache was flushed, or dropped by a reload, reads and stores nothing. The unit natives are in
// UnitCache.cs.
public sealed partial class GameNatives
{
    const string CacheFact = "facts, Files and game cache (the files probe, 11-08)";

    // The game's caches, taken from the documents' saved ones as the game starts (Documents).
    Dictionary<string, Dictionary<(char, string, string), object>> _caches = new(StringComparer.OrdinalIgnoreCase);

    static Dictionary<string, Dictionary<(char, string, string), object>> CopyCaches(Dictionary<string, Dictionary<(char Type, string Mission, string Key), object>> caches) =>
        new(caches.Select(cache => KeyValuePair.Create(cache.Key, new Dictionary<(char, string, string), object>(cache.Value))), StringComparer.OrdinalIgnoreCase);

    /// <summary>The cache of the file's name, one cache a name in any case, a new handle each call; nil reads as an empty name.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Handles and identity: InitGameCache makes a new handle every call (10-01); facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Handles and identity: InitGameCache makes a new handle every call (10-01); " + CacheFact)]
    public gamecache? InitGameCache(string? campaignFile)
    {
        var name = campaignFile ?? "";
        if (!_caches.TryGetValue(name, out var values)) _caches[name] = values = [];
        return Make(new gamecache { Name = name, Values = values });
    }

    /// <summary>
    /// Writes every cache, not only <paramref name="whichCache"/>, to the documents, for the next game, and gives true;
    /// false for null (not measured). Close: measured in one run, which a probe can't repeat (it rewrites the account's
    /// saved caches).
    /// </summary>
    /// <remarks>Close: measured in one run, which a probe can't repeat, as it rewrites the account's saved caches.</remarks>
    [Native(Level.Close)]
    public bool SaveGameCache(gamecache? whichCache)
    {
        if (whichCache is null) return false;
        Documents.GameCaches.Clear();
        foreach (var (name, values) in _caches) Documents.GameCaches[name] = new(values);
        return true;
    }

    /// <summary>
    /// The caches as last saved, each in place, so its handles read it; a cache never saved is gone. True, as in a live
    /// game. Close, as <see cref="SaveGameCache"/>.
    /// </summary>
    /// <remarks>Close: measured in one run, as SaveGameCache.</remarks>
    [Native(Level.Close)]
    public bool ReloadGameCachesFromDisk()
    {
        foreach (var name in _caches.Keys.Where(name => !Documents.GameCaches.ContainsKey(name)).ToList()) _caches.Remove(name);
        foreach (var (name, saved) in Documents.GameCaches)
        {
            if (_caches.TryGetValue(name, out var values)) values.Clear();
            else _caches[name] = values = [];
            foreach (var (key, value) in saved) values[key] = value;
        }
        return true;
    }

    /// <summary>Removes the cache; every handle of it then reads and stores nothing.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public void FlushGameCache(gamecache? cache)
    {
        if (CacheLive(cache)) _caches.Remove(cache!.Name);
    }

    /// <summary>Removes every value of the mission, of all types; nothing for a flushed cache.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public void FlushStoredMission(gamecache? cache, string? missionKey)
    {
        if (!CacheLive(cache)) return;
        var mission = CacheKey(missionKey);
        foreach (var key in cache!.Values.Keys.Where(key => key.Mission == mission).ToList()) cache.Values.Remove(key);
    }

    /// <summary>Stores the integer under the mission and key; nothing for a flushed cache.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public void StoreInteger(gamecache? cache, string? missionKey, string? key, int value) => CacheStore(cache, 'i', missionKey, key, value);

    /// <summary>Stores the real under the mission and key; nothing for a flushed cache.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public void StoreReal(gamecache? cache, string? missionKey, string? key, float value) => CacheStore(cache, 'r', missionKey, key, value);

    /// <summary>Stores the boolean under the mission and key; nothing for a flushed cache.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public void StoreBoolean(gamecache? cache, string? missionKey, string? key, bool value) => CacheStore(cache, 'b', missionKey, key, value);

    /// <summary>True once stored, an empty string too; false where nothing is stored (not measured).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public bool StoreString(gamecache? cache, string? missionKey, string? key, string? value) => CacheStore(cache, 's', missionKey, key, value ?? "");

    /// <summary>The integer stored under the mission and key; 0 where none is.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public int GetStoredInteger(gamecache? cache, string? missionKey, string? key) => (int)(CacheGet(cache, 'i', missionKey, key) ?? 0);

    /// <summary>The real stored under the mission and key; 0 where none is.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public float GetStoredReal(gamecache? cache, string? missionKey, string? key) => (float)(CacheGet(cache, 'r', missionKey, key) ?? 0f);

    /// <summary>The boolean stored under the mission and key; false where none is.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public bool GetStoredBoolean(gamecache? cache, string? missionKey, string? key) => (bool)(CacheGet(cache, 'b', missionKey, key) ?? false);

    /// <summary>The string stored under the mission and key; empty where none is.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public string? GetStoredString(gamecache? cache, string? missionKey, string? key) => (string)(CacheGet(cache, 's', missionKey, key) ?? "");

    /// <summary>Whether an integer is stored under the mission and key; false for a flushed cache.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public bool HaveStoredInteger(gamecache? cache, string? missionKey, string? key) => CacheGet(cache, 'i', missionKey, key) is not null;

    /// <summary>Whether a real is stored under the mission and key; false for a flushed cache.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public bool HaveStoredReal(gamecache? cache, string? missionKey, string? key) => CacheGet(cache, 'r', missionKey, key) is not null;

    /// <summary>Whether a boolean is stored under the mission and key; false for a flushed cache.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public bool HaveStoredBoolean(gamecache? cache, string? missionKey, string? key) => CacheGet(cache, 'b', missionKey, key) is not null;

    /// <summary>Whether a string is stored under the mission and key; false for a flushed cache.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public bool HaveStoredString(gamecache? cache, string? missionKey, string? key) => CacheGet(cache, 's', missionKey, key) is not null;

    /// <summary>Removes the integer stored under the mission and key.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Files and game cache (the files probe, 11-08)).</remarks>
    [Native(Level.Exact, Fact = CacheFact)]
    public void FlushStoredInteger(gamecache? cache, string? missionKey, string? key) => CacheFlush(cache, 'i', missionKey, key);

    /// <summary>As <see cref="FlushStoredInteger"/>, the one measured.</summary>
    /// <remarks>Close: only the integer's flush was measured; this follows it for reals, not measured.</remarks>
    [Native(Level.Close)]
    public void FlushStoredReal(gamecache? cache, string? missionKey, string? key) => CacheFlush(cache, 'r', missionKey, key);

    /// <summary>As <see cref="FlushStoredInteger"/>, the one measured.</summary>
    /// <remarks>Close: only the integer's flush was measured; this follows it for booleans, not measured.</remarks>
    [Native(Level.Close)]
    public void FlushStoredBoolean(gamecache? cache, string? missionKey, string? key) => CacheFlush(cache, 'b', missionKey, key);

    /// <summary>As <see cref="FlushStoredInteger"/>, the one measured.</summary>
    /// <remarks>Close: only the integer's flush was measured; this follows it for strings, not measured.</remarks>
    [Native(Level.Close)]
    public void FlushStoredString(gamecache? cache, string? missionKey, string? key) => CacheFlush(cache, 's', missionKey, key);

    const string SyncReason = "Sends the local player's value to the other machines, and a run has one, whose caches are every player's; on a single machine the game read the value the same after it (probes files and heroes-store, D793)";

    /// <summary>Changes nothing; a run has one machine, so there is nobody to send the value to.</summary>
    /// <remarks>Recorded: Sends the local player's value to the other machines, and a run has one, whose caches are every player's; on a single machine the game read the value the same after it (probes files and heroes-store, D793).</remarks>
    [Native(Level.Recorded, Reason = SyncReason)]
    public void SyncStoredInteger(gamecache? cache, string? missionKey, string? key) { }

    /// <summary>Changes nothing; a run has one machine, so there is nobody to send the value to.</summary>
    /// <remarks>Recorded: Sends the local player's value to the other machines, and a run has one, whose caches are every player's; on a single machine the game read the value the same after it (probes files and heroes-store, D793).</remarks>
    [Native(Level.Recorded, Reason = SyncReason)]
    public void SyncStoredReal(gamecache? cache, string? missionKey, string? key) { }

    /// <summary>Changes nothing; a run has one machine, so there is nobody to send the value to.</summary>
    /// <remarks>Recorded: Sends the local player's value to the other machines, and a run has one, whose caches are every player's; on a single machine the game read the value the same after it (probes files and heroes-store, D793).</remarks>
    [Native(Level.Recorded, Reason = SyncReason)]
    public void SyncStoredBoolean(gamecache? cache, string? missionKey, string? key) { }

    /// <summary>Changes nothing; a run has one machine, so there is nobody to send the value to.</summary>
    /// <remarks>Recorded: Sends the local player's value to the other machines, and a run has one, whose caches are every player's; on a single machine the game read the value the same after it (probes files and heroes-store, D793).</remarks>
    [Native(Level.Recorded, Reason = SyncReason)]
    public void SyncStoredString(gamecache? cache, string? missionKey, string? key) { }

    bool CacheLive(gamecache? cache) => cache is not null && _caches.TryGetValue(cache.Name, out var values) && values == cache.Values;

    // The game finds missions and keys in any case (measured with ASCII letters).
    static string CacheKey(string? key) => (key ?? "").ToUpperInvariant();

    bool CacheStore(gamecache? cache, char type, string? missionKey, string? key, object value)
    {
        if (!CacheLive(cache)) return false;
        cache!.Values[(type, CacheKey(missionKey), CacheKey(key))] = value;
        return true;
    }

    object? CacheGet(gamecache? cache, char type, string? missionKey, string? key) =>
        CacheLive(cache) ? cache!.Values.GetValueOrDefault((type, CacheKey(missionKey), CacheKey(key))) : null;

    void CacheFlush(gamecache? cache, char type, string? missionKey, string? key)
    {
        if (CacheLive(cache)) cache!.Values.Remove((type, CacheKey(missionKey), CacheKey(key)));
    }
}
