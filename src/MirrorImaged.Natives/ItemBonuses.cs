using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class item
{
    internal readonly List<ability> Given = []; // the abilities it puts on its carrier
}

// What items give a hero (probe items-carried, facts "Items carried (W2-3)"): a carried item puts its abilities on the
// carrier and its bonuses count while it is in a slot; a powerup gives its permanent ones and is gone; a potion's heal
// is given when it is used. The values are the ability data's own fields (Istr, Iagi, Iint, Idef, Ilif, Iman, Ihpg,
// Impg, Ixpg), read where the ability keeps them.
public sealed partial class GameNatives
{
    const string CarriedProbe = "facts.md, Items carried (W2-3): probe items-carried (11-15)";

    static readonly int ItemAbilityList = Code("iabi"), BonusStrength = Code("Istr"), BonusAgility = Code("Iagi"), BonusIntelligence = Code("Iint"),
        BonusArmour = Code("Idef"), BonusLife = Code("Ilif"), BonusMana = Code("Iman"), HealLife = Code("Ihpg"), HealMana = Code("Impg"),
        GiveExperience = Code("Ixpg");

    IEnumerable<GameObject> AbilitiesOf(item i) =>
        (i.Type.Text(ItemAbilityList) ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(id => Data.Object(ObjectKind.Ability, Code(id))).OfType<GameObject>();

    // What an ability gives in a field, if it keeps that field where the field's column is (AIh1's DataA is Ihpg, not Iagi)
    int Gives(GameObject ability, int field) => Data.Own(ability, field) == field ? ability.Integer(field, 1) : 0;

    int Carried(unit u, int field) => u.Slots.OfType<item>().Sum(i => AbilitiesOf(i).Sum(a => Gives(a, field)));

    // The hero's attribute counting what it carries, as the whole part
    int WithBonus(unit u, int which) => Whole(which == 0 ? u.Str : which == 1 ? u.Agi : u.Int) + Carried(u, which == 0 ? BonusStrength : which == 1 ? BonusAgility : BonusIntelligence);

    // A change of what the unit carries: the most life and mana move by what the attributes and the items give, and the life and mana
    // keep their share of the most (probe items-carried: 300 of 650 with +75 read 335 of 725), as an attribute gained
    // by levels doesn't (it adds, Change)
    void Carry(unit u, Action change)
    {
        var (strength, intelligence, life, mana) = (WithBonus(u, 0), WithBonus(u, 2), Carried(u, BonusLife), Carried(u, BonusMana));
        var (oldLife, oldMana, lifeNow, manaNow) = (u.MaxLife, u.MaxMana, u.Life, u.Mana);
        change();
        u.MaxLife += (WithBonus(u, 0) - strength) * Data.Constant("StrHitPointBonus", 25) + Carried(u, BonusLife) - life;
        u.MaxMana += (WithBonus(u, 2) - intelligence) * Data.Constant("IntManaBonus", 15) + Carried(u, BonusMana) - mana;
        if (!u.Dead && oldLife > 0 && u.MaxLife != oldLife)
        {
            u.Life = Math.Min((float)Math.Round(lifeNow * u.MaxLife / oldLife, MidpointRounding.AwayFromZero), u.MaxLife);
            StateChanged(u, LifeState, lifeNow, u.Life);
        }
        if (oldMana > 0 && u.MaxMana != oldMana) SetMana(u, (float)Math.Round(manaNow * u.MaxMana / oldMana, MidpointRounding.AwayFromZero));
    }

    // The item in the slot: its abilities on the unit first in its list, its bonuses counting
    void Hold(unit u, item i, int slot) => Carry(u, () =>
    {
        u.Slots[slot] = i;
        i.Holder = u;
        foreach (var type in AbilitiesOf(i)) i.Given.Add(Give(u, type, 0));
    });

    void Release(unit u, item i, int slot) => Carry(u, () =>
    {
        u.Slots[slot] = null;
        foreach (var given in i.Given) Take(given);
        i.Given.Clear();
    });

    // A powerup's permanent gifts, before its pickup event: attributes as an item's bonus counts (life and mana keep their share:
    // 400 of 650 with a tome of strength read 415 of 675), a manual's life added, experience unless the hero's is suspended
    void Absorb(unit u, item i)
    {
        foreach (var a in AbilitiesOf(i))
        {
            Carry(u, () =>
            {
                u.Str += Gives(a, BonusStrength);
                u.Agi += Gives(a, BonusAgility);
                u.Int += Gives(a, BonusIntelligence);
            });
            if (Gives(a, BonusLife) is var life and not 0) Shift(u, life, 0);
            if (Gives(a, GiveExperience) is var xp and > 0 && !u.XpSuspended) GainXp(u, (long)u.Xp + xp);
        }
    }

    // Whether using the item can do anything: a heal needs life or mana to heal (probe items-carried: a potion at full life
    // is refused, its charge kept)
    bool Heals(unit u, item i)
    {
        var (life, mana) = (AbilitiesOf(i).Sum(a => Gives(a, HealLife)), AbilitiesOf(i).Sum(a => Gives(a, HealMana)));
        return life == 0 && mana == 0 || life > 0 && u.Life < u.MaxLife || mana > 0 && u.Mana < u.MaxMana;
    }

    void Heal(unit u, item i)
    {
        foreach (var a in AbilitiesOf(i))
        {
            if (Gives(a, HealLife) is var life and > 0) SetLife(u, Math.Min(u.Life + life, u.MaxLife));
            if (Gives(a, HealMana) is var mana and > 0) SetMana(u, u.Mana + mana);
        }
    }

    static readonly int ReincarnationDelay = Code("Ircd"), ReincarnationLife = Code("irc2");

    // A hero that dies carrying an ankh with a charge uses the last such one in its slots, as it dies (the use event with the
    // hero dead, the drop event if the charge was the last, the item gone inside the call), and is alive again where it
    // died the ability's delay later with its life at the ability's amount, and the mana it had (probe items-ankh).
    // No revive event fires, and no death event either.
    bool Reincarnate(unit u)
    {
        if (!IsHeroUnit(u) || u.Exploded) return false;
        for (var slot = u.Slots.Length - 1; slot >= 0; slot--)
        {
            if (u.Slots[slot] is not { Charges: > 0 } i || AbilitiesOf(i).FirstOrDefault(a => Gives(a, ReincarnationDelay) > 0) is not { } ankh) continue;
            Spend(u, i, now: true);
            _game.At(_game.Step + Game.Steps(Gives(ankh, ReincarnationDelay)), () =>
            {
                if (!u.Dead || _game.Handle(u.Key) is null) return;
                (u.Dead, u.Life) = (false, Math.Min(Gives(ankh, ReincarnationLife), u.MaxLife));
                Counting(u, 1);
            });
            return true;
        }
        return false;
    }
}
