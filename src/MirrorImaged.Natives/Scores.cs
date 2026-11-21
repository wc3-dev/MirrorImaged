namespace MirrorImaged.Natives;

public partial class player
{
    internal int Trained, Killed, Built, Razed, HeroesKilled, UnitValue, HeroExperience, GoldMined, FoodMade, FoodUsed;
}

// GetPlayerScore (probe units-scores, facts, "Players and the game"; D794): a unit made counts as trained, a structure as built, and a kill of an enemy as a
// unit killed, a hero killed (too) or a structure razed, each adding the gold and lumber the type costs to the unit total; the hero total is 8 for each
// point of experience a kill gave the player's heroes; the resource total is the gold mined, delivered or taken from a haunted mine; the total adds the three;
// the most food made and used seen as units were made are kept. Close: the others (technology, items, mercenaries, the gold given, taxed and lost to upkeep, lumber) read 0, as
// the game read them for nothing done in the probe; a unit trained or a structure built by orders counts when it finishes, as in the game, and the units a spell or a shop
// makes aren't counted (not measured).
public sealed partial class GameNatives
{
    const string ScoreProbe = "probe units-scores (11-21, D794)";

    // A unit made by CreateUnit, or trained or built by the game's orders when they finish (the others, summoned, bought or raised, weren't measured): units
    // trained or structures built, with the cost in the unit total
    void ScoreMade(unit u)
    {
        if (IsBuildingNow(u)) u.Owner.Built++;
        else u.Owner.Trained++;
        u.Owner.UnitValue += u.Type.Integer(Cost) + u.Type.Integer(LumberCost);
        // The food made and used are looked at as units are made (a cap set at the start read 0: probe units-scores)
        u.Owner.FoodMade = Math.Max(u.Owner.FoodMade, u.Owner.States[FoodCap]);
        u.Owner.FoodUsed = Math.Max(u.Owner.FoodUsed, u.Owner.States[FoodUsedState]);
    }

    void ScoreKill(player killer, unit victim)
    {
        if (IsBuildingNow(victim)) killer.Razed++;
        else
        {
            killer.Killed++;
            if (IsHero(victim.Type.Id)) killer.HeroesKilled++;
        }
        killer.UnitValue += victim.Type.Integer(Cost) + victim.Type.Integer(LumberCost);
    }

    unit? Scored(unit? made)
    {
        if (made is not null) ScoreMade(made);
        return made;
    }

    int UnitScore(player p) => p.UnitValue;

    int HeroScore(player p) => p.HeroExperience * 8;

    /// <summary>One score of the player, kept for the kinds the probe measured; the other kinds and a nil argument read 0.</summary>
    /// <remarks>Close: Counts what the probe measured (units made, kills, razed structures, hero experience from kills, gold mined, food); technology, items, mercenaries, trade, upkeep and lumber read 0, and what a unit trained by an order or a structure built by a worker counts, and when, isn't measured.</remarks>
    [Native(Level.Close, Fact = ScoreProbe, Reason = "Counts what the probe measured (units made, kills, razed structures, hero experience from kills, gold mined, food); technology, items, mercenaries, trade, upkeep and lumber read 0, and what a unit trained by an order or a structure built by a worker counts, and when, isn't measured")]
    public int GetPlayerScore(player? whichPlayer, playerscore? whichPlayerScore) => whichPlayer is null || whichPlayerScore is null ? 0 : whichPlayerScore.Id switch
    {
        0 => whichPlayer.Trained,
        1 => whichPlayer.Killed,
        2 => whichPlayer.Built,
        3 => whichPlayer.Razed,
        5 => whichPlayer.FoodMade,
        6 => whichPlayer.FoodUsed,
        7 => whichPlayer.HeroesKilled,
        10 or 11 or 23 => whichPlayer.GoldMined,
        21 => UnitScore(whichPlayer),
        22 => HeroScore(whichPlayer),
        24 => UnitScore(whichPlayer) + HeroScore(whichPlayer) + whichPlayer.GoldMined,
        _ => 0,
    };
}
