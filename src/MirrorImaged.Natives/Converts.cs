namespace MirrorImaged.Natives;

// A Convert native gives the handle standing for a value of its type: made once per type and value, so the same value
// gives the same userdata. Its id is the value, also one common.j doesn't name; it takes none of the game's ids, and a key
// of its own (decisions D64; probe handles). Lua's == compares ids, so the same value of another type is equal, another
// userdata (decisions D112). ConvertPlayerColor and ConvertRace are Exact with their getters (decisions D90),
// ConvertGameEvent and ConvertLimitOp with GetTriggerEventId (probe triggers-2). ConvertAttackType,
// ConvertDamageType and ConvertWeaponType become Exact once their getters are written, which the facts compare them
// with (C# through CSharp.lua: every attack, damage and weapon type dealt from code reads back the same, through
// Convert*Type handles made once and compared with ==).
public sealed partial class GameNatives
{
    readonly Dictionary<(Type, int), handle> _converted = [];

    T Convert<T>(int value) where T : handle, new()
    {
        if (!_converted.TryGetValue((typeof(T), value), out var made))
        {
            _converted.Add((typeof(T), value), made = new T { Id = value });
            made.Key = _game.Add(made);
        }
        return (T)made;
    }

    /// <summary>The <c>aidifficulty</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public aidifficulty? ConvertAIDifficulty(int i) => Convert<aidifficulty>(i);
    /// <summary>The <c>abilitybooleanfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilitybooleanfield? ConvertAbilityBooleanField(int i) => Convert<abilitybooleanfield>(i);
    /// <summary>The <c>abilitybooleanlevelarrayfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilitybooleanlevelarrayfield? ConvertAbilityBooleanLevelArrayField(int i) => Convert<abilitybooleanlevelarrayfield>(i);
    /// <summary>The <c>abilitybooleanlevelfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilitybooleanlevelfield? ConvertAbilityBooleanLevelField(int i) => Convert<abilitybooleanlevelfield>(i);
    /// <summary>The <c>abilityintegerfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilityintegerfield? ConvertAbilityIntegerField(int i) => Convert<abilityintegerfield>(i);
    /// <summary>The <c>abilityintegerlevelarrayfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilityintegerlevelarrayfield? ConvertAbilityIntegerLevelArrayField(int i) => Convert<abilityintegerlevelarrayfield>(i);
    /// <summary>The <c>abilityintegerlevelfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilityintegerlevelfield? ConvertAbilityIntegerLevelField(int i) => Convert<abilityintegerlevelfield>(i);
    /// <summary>The <c>abilityrealfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilityrealfield? ConvertAbilityRealField(int i) => Convert<abilityrealfield>(i);
    /// <summary>The <c>abilityreallevelarrayfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilityreallevelarrayfield? ConvertAbilityRealLevelArrayField(int i) => Convert<abilityreallevelarrayfield>(i);
    /// <summary>The <c>abilityreallevelfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilityreallevelfield? ConvertAbilityRealLevelField(int i) => Convert<abilityreallevelfield>(i);
    /// <summary>The <c>abilitystringfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilitystringfield? ConvertAbilityStringField(int i) => Convert<abilitystringfield>(i);
    /// <summary>The <c>abilitystringlevelarrayfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilitystringlevelarrayfield? ConvertAbilityStringLevelArrayField(int i) => Convert<abilitystringlevelarrayfield>(i);
    /// <summary>The <c>abilitystringlevelfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public abilitystringlevelfield? ConvertAbilityStringLevelField(int i) => Convert<abilitystringlevelfield>(i);
    /// <summary>The <c>alliancetype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public alliancetype? ConvertAllianceType(int i) => Convert<alliancetype>(i);
    /// <summary>The <c>animtype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public animtype? ConvertAnimType(int i) => Convert<animtype>(i);
    /// <summary>The <c>armortype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public armortype? ConvertArmorType(int i) => Convert<armortype>(i);
    /// <summary>The <c>attacktype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public attacktype? ConvertAttackType(int i) => Convert<attacktype>(i);
    /// <summary>The <c>blendmode</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public blendmode? ConvertBlendMode(int i) => Convert<blendmode>(i);
    /// <summary>The <c>camerafield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public camerafield? ConvertCameraField(int i) => Convert<camerafield>(i);
    /// <summary>The <c>damagetype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public damagetype? ConvertDamageType(int i) => Convert<damagetype>(i);
    /// <summary>The <c>defensetype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public defensetype? ConvertDefenseType(int i) => Convert<defensetype>(i);
    /// <summary>The <c>dialogevent</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public dialogevent? ConvertDialogEvent(int i) => Convert<dialogevent>(i);
    /// <summary>The <c>effecttype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public effecttype? ConvertEffectType(int i) => Convert<effecttype>(i);
    /// <summary>The <c>equipmentType</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public equipmentType? ConvertEquipmentType(int i) => Convert<equipmentType>(i);
    /// <summary>The <c>fgamestate</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public fgamestate? ConvertFGameState(int i) => Convert<fgamestate>(i);
    /// <summary>The <c>fogstate</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public fogstate? ConvertFogState(int i) => Convert<fogstate>(i);
    /// <summary>The <c>fogstyle</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public fogstyle? ConvertFogStyle(int i) => Convert<fogstyle>(i);
    /// <summary>The <c>frameeventtype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public frameeventtype? ConvertFrameEventType(int i) => Convert<frameeventtype>(i);
    /// <summary>The <c>framepointtype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public framepointtype? ConvertFramePointType(int i) => Convert<framepointtype>(i);
    /// <summary>The <c>gamedifficulty</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public gamedifficulty? ConvertGameDifficulty(int i) => Convert<gamedifficulty>(i);
    /// <summary>The <c>gameevent</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Triggers and threads: probe triggers-2 (11-08); facts, Handles and errors: probes handles, handles-2, handles-3 and arguments (11-08)).</remarks>
    [Native(Level.Exact, Fact = Probe2 + "; " + Handles)] public gameevent? ConvertGameEvent(int i) => Convert<gameevent>(i);
    /// <summary>The <c>gamespeed</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public gamespeed? ConvertGameSpeed(int i) => Convert<gamespeed>(i);
    /// <summary>The <c>gametype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public gametype? ConvertGameType(int i) => Convert<gametype>(i);
    /// <summary>The <c>heroattribute</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public heroattribute? ConvertHeroAttribute(int i) => Convert<heroattribute>(i);
    /// <summary>The <c>igamestate</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public igamestate? ConvertIGameState(int i) => Convert<igamestate>(i);
    /// <summary>The <c>itembooleanfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public itembooleanfield? ConvertItemBooleanField(int i) => Convert<itembooleanfield>(i);
    /// <summary>The <c>itemintegerfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public itemintegerfield? ConvertItemIntegerField(int i) => Convert<itemintegerfield>(i);
    /// <summary>The <c>itemrealfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public itemrealfield? ConvertItemRealField(int i) => Convert<itemrealfield>(i);
    /// <summary>The <c>itemstringfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public itemstringfield? ConvertItemStringField(int i) => Convert<itemstringfield>(i);
    /// <summary>The <c>itemTag</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public itemTag? ConvertItemTag(int i) => Convert<itemTag>(i);
    /// <summary>The <c>itemtype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public itemtype? ConvertItemType(int i) => Convert<itemtype>(i);
    /// <summary>The <c>limitop</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Triggers and threads: probe triggers-2 (11-08)).</remarks>
    [Native(Level.Exact, Fact = Probe2)] public limitop? ConvertLimitOp(int i) => Convert<limitop>(i);
    /// <summary>The <c>loadoutslot</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public loadoutslot? ConvertLoadoutSlot(int i) => Convert<loadoutslot>(i);
    /// <summary>The <c>mapcontrol</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public mapcontrol? ConvertMapControl(int i) => Convert<mapcontrol>(i);
    /// <summary>The <c>mapdensity</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public mapdensity? ConvertMapDensity(int i) => Convert<mapdensity>(i);
    /// <summary>The <c>mapflag</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public mapflag? ConvertMapFlag(int i) => Convert<mapflag>(i);
    /// <summary>The <c>mapsetting</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public mapsetting? ConvertMapSetting(int i) => Convert<mapsetting>(i);
    /// <summary>The <c>mapvisibility</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public mapvisibility? ConvertMapVisibility(int i) => Convert<mapvisibility>(i);
    /// <summary>The <c>mousebuttontype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public mousebuttontype? ConvertMouseButtonType(int i) => Convert<mousebuttontype>(i);
    /// <summary>The <c>movetype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public movetype? ConvertMoveType(int i) => Convert<movetype>(i);
    /// <summary>The <c>originframetype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public originframetype? ConvertOriginFrameType(int i) => Convert<originframetype>(i);
    /// <summary>The <c>oskeytype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public oskeytype? ConvertOsKeyType(int i) => Convert<oskeytype>(i);
    /// <summary>The <c>pathingflag</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public pathingflag? ConvertPathingFlag(int i) => Convert<pathingflag>(i);
    /// <summary>The <c>pathingtype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public pathingtype? ConvertPathingType(int i) => Convert<pathingtype>(i);
    /// <summary>The <c>placement</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public placement? ConvertPlacement(int i) => Convert<placement>(i);
    /// <summary>The <c>playercolor</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: GetPlayerColor compares equal to ConvertPlayerColor(n) with Lua's == (10-20)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: GetPlayerColor compares equal to ConvertPlayerColor(n) with Lua's == (10-20)")] public playercolor? ConvertPlayerColor(int i) => Convert<playercolor>(i);
    /// <summary>The <c>playerevent</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public playerevent? ConvertPlayerEvent(int i) => Convert<playerevent>(i);
    /// <summary>The <c>playergameresult</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public playergameresult? ConvertPlayerGameResult(int i) => Convert<playergameresult>(i);
    /// <summary>The <c>playerscore</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public playerscore? ConvertPlayerScore(int i) => Convert<playerscore>(i);
    /// <summary>The <c>playerslotstate</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public playerslotstate? ConvertPlayerSlotState(int i) => Convert<playerslotstate>(i);
    /// <summary>The <c>playerstate</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public playerstate? ConvertPlayerState(int i) => Convert<playerstate>(i);
    /// <summary>The <c>playerunitevent</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public playerunitevent? ConvertPlayerUnitEvent(int i) => Convert<playerunitevent>(i);
    /// <summary>The <c>race</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: GetPlayerRace compares equal to RACE_* with Lua's == (10-20)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: GetPlayerRace compares equal to RACE_* with Lua's == (10-20)")] public race? ConvertRace(int i) => Convert<race>(i);
    /// <summary>The <c>racepreference</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public racepreference? ConvertRacePref(int i) => Convert<racepreference>(i);
    /// <summary>The <c>raritycontrol</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public raritycontrol? ConvertRarityControl(int i) => Convert<raritycontrol>(i);
    /// <summary>The <c>regentype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public regentype? ConvertRegenType(int i) => Convert<regentype>(i);
    /// <summary>The <c>soundtype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public soundtype? ConvertSoundType(int i) => Convert<soundtype>(i);
    /// <summary>The <c>startlocprio</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public startlocprio? ConvertStartLocPrio(int i) => Convert<startlocprio>(i);
    /// <summary>The <c>subanimtype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public subanimtype? ConvertSubAnimType(int i) => Convert<subanimtype>(i);
    /// <summary>The <c>targetflag</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public targetflag? ConvertTargetFlag(int i) => Convert<targetflag>(i);
    /// <summary>The <c>texmapflags</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public texmapflags? ConvertTexMapFlags(int i) => Convert<texmapflags>(i);
    /// <summary>The <c>textaligntype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public textaligntype? ConvertTextAlignType(int i) => Convert<textaligntype>(i);
    /// <summary>The <c>unitbooleanfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unitbooleanfield? ConvertUnitBooleanField(int i) => Convert<unitbooleanfield>(i);
    /// <summary>The <c>unitcategory</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unitcategory? ConvertUnitCategory(int i) => Convert<unitcategory>(i);
    /// <summary>The <c>unitevent</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unitevent? ConvertUnitEvent(int i) => Convert<unitevent>(i);
    /// <summary>The <c>unitintegerfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unitintegerfield? ConvertUnitIntegerField(int i) => Convert<unitintegerfield>(i);
    /// <summary>The <c>unitrealfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unitrealfield? ConvertUnitRealField(int i) => Convert<unitrealfield>(i);
    /// <summary>The <c>unitstate</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unitstate? ConvertUnitState(int i) => Convert<unitstate>(i);
    /// <summary>The <c>unitstringfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unitstringfield? ConvertUnitStringField(int i) => Convert<unitstringfield>(i);
    /// <summary>The <c>unittype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unittype? ConvertUnitType(int i) => Convert<unittype>(i);
    /// <summary>The <c>unitweaponbooleanfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unitweaponbooleanfield? ConvertUnitWeaponBooleanField(int i) => Convert<unitweaponbooleanfield>(i);
    /// <summary>The <c>unitweaponintegerfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unitweaponintegerfield? ConvertUnitWeaponIntegerField(int i) => Convert<unitweaponintegerfield>(i);
    /// <summary>The <c>unitweaponrealfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unitweaponrealfield? ConvertUnitWeaponRealField(int i) => Convert<unitweaponrealfield>(i);
    /// <summary>The <c>unitweaponstringfield</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public unitweaponstringfield? ConvertUnitWeaponStringField(int i) => Convert<unitweaponstringfield>(i);
    /// <summary>The <c>version</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public version? ConvertVersion(int i) => Convert<version>(i);
    /// <summary>The <c>volumegroup</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public volumegroup? ConvertVolumeGroup(int i) => Convert<volumegroup>(i);
    /// <summary>The <c>weapontype</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public weapontype? ConvertWeaponType(int i) => Convert<weapontype>(i);
    /// <summary>The <c>widgetevent</c> for <paramref name="i"/>, made once per value, so the same value gives the same handle.</summary>
    /// <remarks>Close: its id is the value, not one of the game's ids (D64).</remarks>
    [Native(Level.Close)] public widgetevent? ConvertWidgetEvent(int i) => Convert<widgetevent>(i);
}
