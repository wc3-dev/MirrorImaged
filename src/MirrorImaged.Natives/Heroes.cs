using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// Heroes, as the heroes probe and the facts' "Units" and "Heroes" measure them (facts, "Heroes (W2-3)"): levels
// and experience by the constants' formula, the attributes and what they give, skill points, revival and the hero
// events. A hero keeps its attributes with the fractions its gains add up to; what a whole attribute is read as is the
// whole part. Life, mana and armour follow the attributes: a change adds its share to the life and mana now.
public sealed partial class GameNatives
{
    const string HeroProbe = "facts, Heroes (W2-3): probe heroes (11-15)";
    const int HeroLevelEvent = 78, HeroSkillEvent = 79, HeroReviveStart = 81, HeroReviveFinish = 83,
        PlayerHeroLevel = 41, PlayerHeroSkill = 42, PlayerHeroReviveStart = 44, PlayerHeroReviveFinish = 46;

    static readonly int HeroAbilities = Code("uhab"), InitialMana = Code("umpi"), AgilityBase = Code("uagi"),
        RequiredLevel = Code("arlv"), SkillLevelSkip = Code("alsk");

    // the gains are floats added and taken back, so 17 + 9 x 1.8 - 9 x 1.8 may be a hair under 17
    static int Whole(double attribute) => (int)Math.Floor(attribute + 1e-4);

    // A new hero: level 1, no experience, one skill point, its type's attributes and a proper name
    void Begin(unit u)
    {
        (u.Str, u.Agi, u.Int) = (u.Type.Integer(Strength), u.Type.Integer(AgilityBase), u.Type.Integer(Intelligence));
        u.SkillPoints = 1;
        u.ProperName = ProperName(u);
    }

    // The hero's attributes changed by change: the most life and mana move by the constants' bonus for each whole
    // attribute gained or lost, and the life and mana with them (a gain is added to what it is, a loss taken from it)
    void Change(unit u, Action change)
    {
        var (strength, intelligence) = (Whole(u.Str), Whole(u.Int));
        Regenerate(u);
        change();
        Shift(u, (Whole(u.Str) - strength) * Data.Constant("StrHitPointBonus", 25), (Whole(u.Int) - intelligence) * Data.Constant("IntManaBonus", 15));
    }

    void Shift(unit u, float life, float mana)
    {
        if (life != 0)
        {
            var old = u.Life;
            u.MaxLife += life;
            if (!u.Dead) u.Life = Math.Min(u.Life + life, u.MaxLife);
            if (u.Life != old) StateChanged(u, LifeState, old, u.Life);
        }
        if (mana != 0)
        {
            u.MaxMana += mana;
            SetMana(u, u.Mana + mana);
        }
    }

    // ---- Levels and experience

    int MaxLevel => (int)Data.Constant("MaxHeroLevel", 10);

    // The experience a level takes in all (facts: the first levels from NeedHeroXP's list, then each level the one
    // before's times A plus the level times B plus C, kept with its fraction and cut when read)
    int XpFor(int level)
    {
        var listed = (Data.Constant("NeedHeroXP") ?? "200").Split(',', StringSplitOptions.TrimEntries);
        var (a, b, c) = (Data.Constant("NeedHeroXPFormulaA", 1), Data.Constant("NeedHeroXPFormulaB", 100), Data.Constant("NeedHeroXPFormulaC", 0));
        double total = 0;
        for (var n = 2; n <= level; n++)
            total = n - 2 < listed.Length && float.TryParse(listed[n - 2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var given)
                ? given
                : total * a + n * b + c;
        return (int)total;
    }

    int LevelFor(int xp)
    {
        var level = 1;
        while (level < MaxLevel && XpFor(level + 1) <= xp) level++;
        return level;
    }

    float Gain(unit u, int field) => BlzGetUnitRealField(u, Convert<unitrealfield>(field));

    // The hero at the level: its gains per level added or taken, its skill points with them (from 0), then the level
    // event if asked. Its experience is the caller's.
    void LevelTo(unit u, int level, bool announce)
    {
        level = Math.Clamp(level, 1, MaxLevel);
        var gained = level - u.HeroLevel;
        if (gained == 0) return;
        Change(u, () =>
        {
            u.Str += gained * Gain(u, StrengthPerLevel);
            u.Agi += gained * Gain(u, AgilityPerLevel);
            u.Int += gained * Gain(u, IntelligencePerLevel);
        });
        u.HeroLevel = level;
        u.SkillPoints = Math.Max(0, u.SkillPoints + gained);
        if (announce) FireUnit(new(u), HeroLevelEvent, PlayerHeroLevel);
        LearnSkills(u);
    }

    static bool IsHeroUnit(unit? u) => u is not null && IsHero(u.Type.Id);

    /// <summary>The hero's level; 0 for nil and for a unit that isn't a hero (probes heroes-nonhero and heroes-nonhero-level).</summary>
    [Native(Level.Close)]
    public int GetHeroLevel(unit? whichHero) => IsHeroUnit(whichHero) ? whichHero!.HeroLevel : 0;

    [Native(Level.Close)]
    public int GetHeroXP(unit? whichHero) => IsHeroUnit(whichHero) ? whichHero!.Xp : 0;

    [Native(Level.Close)]
    public int GetHeroSkillPoints(unit? whichHero) => IsHeroUnit(whichHero) ? whichHero!.SkillPoints : 0;

    /// <summary>
    /// Raises the hero's experience to the value, past the last level's no further; a lower value changes nothing. A
    /// level reached fires one level event inside the call, reading the final level, experience and skill points. None
    /// while the hero's experience is suspended (probe heroes, game-facts "Heroes").
    /// </summary>
    [Native(Level.Close)]
    public void SetHeroXP(unit? whichHero, int newXpVal, bool showEyeCandy)
    {
        if (!IsHeroUnit(whichHero) || whichHero!.XpSuspended || newXpVal <= whichHero.Xp) return;
        GainXp(whichHero, newXpVal);
    }

    /// <summary>As SetHeroXP for the sum, but a negative amount lowers the experience, not the level; the experience stays from 0 to the last level's.</summary>
    [Native(Level.Close)]
    public void AddHeroXP(unit? whichHero, int xpToAdd, bool showEyeCandy)
    {
        if (!IsHeroUnit(whichHero) || whichHero!.XpSuspended) return;
        GainXp(whichHero, (int)Math.Max(0, (long)whichHero.Xp + xpToAdd));
    }

    void GainXp(unit u, long xp)
    {
        u.Xp = (int)Math.Min(xp, XpFor(MaxLevel));
        LevelTo(u, Math.Max(u.HeroLevel, LevelFor(u.Xp)), announce: true);
    }

    /// <summary>The level, only raised and up to the last one, with the experience of that level; none while the experience is suspended.</summary>
    [Native(Level.Close)]
    public void SetHeroLevel(unit? whichHero, int level, bool showEyeCandy)
    {
        if (!IsHeroUnit(whichHero) || whichHero!.XpSuspended || level <= whichHero.HeroLevel) return;
        whichHero.Xp = XpFor(Math.Min(level, MaxLevel));
        LevelTo(whichHero, level, announce: true);
    }

    /// <summary>
    /// Takes the levels off, down to level 1: no event, the experience that of the level now, the skill points
    /// the levels' (probe heroes). True for a hero.
    /// </summary>
    [Native(Level.Close)]
    public bool UnitStripHeroLevel(unit? whichHero, int howManyLevels)
    {
        if (!IsHeroUnit(whichHero)) return false;
        LevelTo(whichHero!, whichHero!.HeroLevel - howManyLevels, announce: false);
        whichHero.Xp = XpFor(whichHero.HeroLevel);
        return true;
    }

    [Native(Level.Close)]
    public void SuspendHeroXP(unit? whichHero, bool flag)
    {
        if (IsHeroUnit(whichHero)) whichHero!.XpSuspended = flag;
    }

    /// <summary>True for a unit that isn't a hero, before any suspend (probe heroes-nonhero).</summary>
    [Native(Level.Close)]
    public bool IsSuspendedXP(unit? whichHero) => whichHero is not null && (!IsHero(whichHero.Type.Id) || whichHero.XpSuspended);

    // BlzSetUnitIntegerField(UNIT_IF_LEVEL): a lower level is taken as UnitStripHeroLevel; a higher one is one level more
    // than the value (probe heroes: 3 from level 1 read 4, from level 5 read 3), with the experience of that level
    void SetLevelField(unit u, int value)
    {
        value = Math.Max(value, 1);
        var level = Math.Min(value > u.HeroLevel ? value + 1 : value, MaxLevel);
        LevelTo(u, level, announce: false);
        u.Xp = XpFor(u.HeroLevel);
    }

    // ---- Attributes

    // A hero's attribute set to a whole number: the fraction its levels' gains made is gone, and so is the life and mana
    // the difference gives
    void SetAttribute(unit u, int which, int value) => Change(u, () =>
    {
        if (which == 0) u.Str = value;
        else if (which == 1) u.Agi = value;
        else u.Int = value;
    });

    int AttributeOf(unit? u, int which, bool bonus = false)
    {
        if (u is null || !IsHero(u.Type.Id)) return 0;
        return bonus ? WithBonus(u, which) : Whole(which == 0 ? u.Str : which == 1 ? u.Agi : u.Int);
    }

    /// <summary>The whole part of the hero's strength, its gains per level included, or with the bonus of what it carries; 0 for a unit that isn't a hero (probe heroes-nonhero-str).</summary>
    [Native(Level.Close)]
    public int GetHeroStr(unit? whichHero, bool includeBonuses) => AttributeOf(whichHero, 0, includeBonuses);

    [Native(Level.Close)]
    public int GetHeroAgi(unit? whichHero, bool includeBonuses) => AttributeOf(whichHero, 1, includeBonuses);

    [Native(Level.Close)]
    public int GetHeroInt(unit? whichHero, bool includeBonuses) => AttributeOf(whichHero, 2, includeBonuses);

    /// <summary>
    /// Sets the strength, at once (0 and -5 too); the most life moves by the constants' bonus for the difference and the
    /// life with it. The permanent flag changes nothing (probe items-carried: a set with a bonus carried sets the base, whichever it is); the bonus of what the hero carries stays on top.
    /// </summary>
    [Native(Level.Close)]
    public void SetHeroStr(unit? whichHero, int newStr, bool permanent)
    {
        if (IsHeroUnit(whichHero)) SetAttribute(whichHero!, 0, newStr);
    }

    [Native(Level.Close)]
    public void SetHeroAgi(unit? whichHero, int newAgi, bool permanent)
    {
        if (IsHeroUnit(whichHero)) SetAttribute(whichHero!, 1, newAgi);
    }

    [Native(Level.Close)]
    public void SetHeroInt(unit? whichHero, int newInt, bool permanent)
    {
        if (IsHeroUnit(whichHero)) SetAttribute(whichHero!, 2, newInt);
    }

    // ---- Skills

    /// <summary>Raises the skill points by the amount, to 0 at the least; true for a hero (probe heroes).</summary>
    [Native(Level.Close)]
    public bool UnitModifySkillPoints(unit? whichHero, int skillPointDelta)
    {
        if (!IsHeroUnit(whichHero)) return false;
        whichHero!.SkillPoints = Math.Max(0, whichHero.SkillPoints + skillPointDelta);
        return true;
    }

    /// <summary>
    /// Learns the next level of a hero ability the hero's type has, if it has a skill point and its level is the
    /// ability's required level, then 2 more for each level after the first (or its level skip, which the data reads 0 for Holy Light); the skill event fires before
    /// the point is spent (probe heroes: the points read the same inside it). The hero gets the ability at that level (probe heroes-store: AHhb read 1, and the unit had one ability more).
    /// </summary>
    [Native(Level.Close)]
    public void SelectHeroSkill(unit? whichHero, int abilcode)
    {
        if (!IsHeroUnit(whichHero) || whichHero!.SkillPoints <= 0 || whichHero.Dead || Data.Object(ObjectKind.Ability, abilcode) is not { } ability) return;
        var abilities = (whichHero.Type.Text(HeroAbilities) ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!abilities.Contains(Name(abilcode))) return;
        var next = whichHero.Skills.GetValueOrDefault(abilcode) + 1;
        if (next > ability.Levels || whichHero.HeroLevel < ability.Integer(RequiredLevel) + (next - 1) * (ability.Integer(SkillLevelSkip) is var skip and > 0 ? skip : 2)) return;
        whichHero.Skills[abilcode] = next;
        (Find(whichHero, abilcode) ?? Give(whichHero, ability, 0)).Level = next;
        FireUnit(new(whichHero, Skill: abilcode, SkillLevel: next), HeroSkillEvent, PlayerHeroSkill);
        whichHero.SkillPoints = Math.Max(0, whichHero.SkillPoints - 1);
    }

    [Native(Level.Recorded, Reason = "No unit of the game's data has talents: ATal and ATua can't be given (UnitAddAbility false), and the game changed nothing on a level 10 Paladin with 10 skill points, with or without the call (probe heroes-talents, D793)")]
    public void BlzResetUnitTalents(unit? whichUnit)
    {
    }

    [Native(Level.Close)]
    public unit? GetLearningUnit() => Response(HeroSkillEvent, PlayerHeroSkill)?.Unit;

    [Native(Level.Close)]
    public unit? GetLevelingUnit() => Response(HeroLevelEvent, PlayerHeroLevel)?.Unit;

    /// <summary>The skill's id in a skill event; 0 outside one.</summary>
    [Native(Level.Close)]
    public int GetLearnedSkill() => Response(HeroSkillEvent, PlayerHeroSkill)?.Skill ?? 0;

    [Native(Level.Close)]
    public int GetLearnedSkillLevel() => Response(HeroSkillEvent, PlayerHeroSkill)?.SkillLevel ?? 0;

    // ---- Revival

    /// <summary>
    /// Brings a dead hero back at the place: false for a hero that lives or a unit that isn't one. The start event
    /// fires first with the hero dead where it died, then it lives with its most life and its type's starting mana
    /// (100 of 255, not its full mana), and the finish event fires (probe heroes), all inside the call.
    /// </summary>
    [Native(Level.Close)]
    public bool ReviveHero(unit? whichHero, float x, float y, bool doEyecandy)
    {
        if (!IsHeroUnit(whichHero) || !whichHero!.Dead) return false;
        FireUnit(new(whichHero), HeroReviveStart, PlayerHeroReviveStart);
        whichHero.Dead = false;
        Move(whichHero, x, y);
        whichHero.Life = whichHero.MaxLife;
        whichHero.Mana = Math.Min(whichHero.Type.Integer(InitialMana), whichHero.MaxMana);
        Counting(whichHero, 1);
        FireUnit(new(whichHero), HeroReviveFinish, PlayerHeroReviveFinish);
        return true;
    }

    [Native(Level.Close)]
    public bool ReviveHeroLoc(unit? whichHero, location? loc, bool doEyecandy) => loc is not null && ReviveHero(whichHero, loc.X, loc.Y, doEyecandy);

    [Native(Level.Close)]
    public unit? GetRevivingUnit() => Response(HeroReviveStart, HeroReviveFinish, PlayerHeroReviveStart, PlayerHeroReviveFinish)?.Unit;

    /// <summary>
    /// The unit of a revivable event, which never fires here: the game fires it long after a hero died and was revived,
    /// for a unit that isn't the hero (probe heroes' last line, recorded in probes/differences/heroes.txt).
    /// </summary>
    [Native(Level.Close)]
    public unit? GetRevivableUnit() => Response(80, 43)?.Unit;

    // ---- Proper names

    readonly Dictionary<string, int> _nameUses = [];
    readonly Random _names = new(0x6e616d65); // the game draws from a generator of its own, not the map's (D523)

    // A name of the type's list drawn at random, with its roman numeral once it is taken (facts: "Granis Darkhammer XII")
    string ProperName(unit u)
    {
        var names = (u.Type.Text(ProperNames) ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0) return "";
        var name = names[_names.Next(names.Length)];
        var uses = _nameUses[name] = _nameUses.GetValueOrDefault(name) + 1;
        return uses == 1 ? name : $"{name} {Roman(uses)}";
    }

    static string Roman(int n)
    {
        var (text, numerals) = ("", new (int, string)[] { (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I") });
        foreach (var (value, symbol) in numerals)
            for (; n >= value; n -= value) text += symbol;
        return text;
    }

    /// <summary>The hero's proper name, drawn at creation; "" for nil and for a unit that isn't a hero.</summary>
    [Native(Level.Close)]
    public string? GetHeroProperName(unit? whichHero) => IsHeroUnit(whichHero) ? whichHero!.ProperName : "";

    [Native(Level.Close)]
    public void BlzSetHeroProperName(unit? whichUnit, string? heroProperName)
    {
        if (IsHeroUnit(whichUnit) && heroProperName is not null) whichUnit!.ProperName = heroProperName;
    }
}
