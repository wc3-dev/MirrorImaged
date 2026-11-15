using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// A unit kept in a game cache: what StoreUnit takes and RestoreUnit makes a new unit from (probe heroes-store, facts
// "Stored units (W2-3)"). Its type, and for a hero its level, experience, skill points, learned skills, attributes and the
// items in its slots with their charges; nothing else (life and mana come back full, the name, user data, flags and
// the abilities added are gone).
sealed record StoredUnit(int Type, int Level, int Xp, int Points, double Str, double Agi, double Int, Dictionary<int, int> Skills, (int Slot, int Item, int Charges)[] Items);

public sealed partial class GameNatives
{
    const string StoredProbe = "facts.md, Stored units (W2-3): probe heroes-store (11-15)";

    /// <summary>
    /// Keeps the unit in the cache and answers false, the game's answer for every unit, one dead or a building too; a unit
    /// removed this step still is kept, one already gone (nil) is not (probe heroes-store). Close: the cache is one run's
    /// memory, as SaveGameCache keeps it.
    /// </summary>
    [Native(Level.Close, Fact = StoredProbe)]
    public bool StoreUnit(gamecache? cache, string? missionKey, string? key, unit? whichUnit)
    {
        if (whichUnit is not { } u) return false;
        var hero = IsHeroUnit(u);
        CacheStore(cache, 'u', missionKey, key, new StoredUnit(u.Type.Id, hero ? u.HeroLevel : 0, u.Xp, u.SkillPoints, u.Str, u.Agi, u.Int, new(u.Skills),
            u.Slots.Select((i, slot) => (Slot: slot, Item: i?.Type.Id ?? 0, Charges: i?.Charges ?? 0)).Where(s => s.Item != 0).ToArray()));
        return false;
    }

    [Native(Level.Close, Fact = StoredProbe)]
    public bool HaveStoredUnit(gamecache? cache, string? missionKey, string? key) => CacheGet(cache, 'u', missionKey, key) is not null;

    [Native(Level.Close, Fact = StoredProbe)]
    public void FlushStoredUnit(gamecache? cache, string? missionKey, string? key) => CacheFlush(cache, 'u', missionKey, key);

    // Syncing sends the local player's value to the others: a run has one machine (as SyncStoredInteger)
    [Native(Level.Close)]
    public void SyncStoredUnit(gamecache? cache, string? missionKey, string? key) { }

    /// <summary>
    /// A new unit of the stored type for the player at the place, alive at its most life and mana; a hero with the stored
    /// level, experience, points, skills and attributes, its items back in their slots (each with its pickup events) and
    /// one level event when it is above level 1 (probe heroes-store). Nil where nothing is stored and for no player.
    /// Close: the level event's count was measured for one hero.
    /// </summary>
    [Native(Level.Close, Fact = StoredProbe)]
    public unit? RestoreUnit(gamecache? cache, string? missionKey, string? key, player? forWhichPlayer, float x, float y, float facing)
    {
        if (CacheGet(cache, 'u', missionKey, key) is not StoredUnit s || Create(forWhichPlayer, s.Type, x, y, facing, s.Type) is not { } u) return null;
        if (IsHeroUnit(u))
        {
            Change(u, () => (u.Str, u.Agi, u.Int) = (s.Str, s.Agi, s.Int));
            (u.HeroLevel, u.Xp, u.SkillPoints) = (s.Level, s.Xp, s.Points);
            foreach (var (skill, level) in s.Skills)
                if (Data.Object(ObjectKind.Ability, skill) is { } type)
                {
                    u.Skills[skill] = level;
                    Give(u, type, 0).Level = level;
                }
            foreach (var (slot, item, charges) in s.Items)
                if (NewItem(item, x, y, item) is { } i)
                {
                    i.Charges = charges;
                    Put(u, i, slot);
                }
            if (s.Level > 1) FireUnit(new(u), HeroLevelEvent, PlayerHeroLevel);
        }
        (u.Life, u.Mana) = (u.MaxLife, u.MaxMana);
        return u;
    }
}
