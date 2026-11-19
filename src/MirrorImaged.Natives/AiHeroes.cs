namespace MirrorImaged.Natives;

// The skills a computer player's heroes learn: the AI script gives a function (`SetHeroLevels`) that answers, for the hero (`GetHeroId`) and the level it has
// reached (`GetHeroLevelAI`), the ability to learn; each time a hero of the player has a skill point, the function is asked and the hero learns what it names
// (decisions D383).
public sealed partial class GameNatives
{
    readonly Dictionary<player, Func<int>> _skillChoosers = [];
    unit? _choosing;

    /// <summary>Gives the function that names the skill a hero learns, for the AI player; heroes it has with a skill point learn at once.</summary>
    [Native(Level.Close)]
    public void SetHeroLevels(Func<int>? func)
    {
        if (func is null) _skillChoosers.Remove(Ai);
        else _skillChoosers[Ai] = func;
        foreach (var hero in Mine(Ai).Where(IsHeroUnit).ToArray()) LearnSkills(hero);
    }

    /// <summary>The type of the hero whose skill is being chosen, in the function that chooses it.</summary>
    [Native(Level.Close)] public int GetHeroId() => _choosing?.Type.Id ?? 0;

    /// <summary>The level the hero whose skill is being chosen has reached.</summary>
    [Native(Level.Close)] public int GetHeroLevelAI() => _choosing?.HeroLevel ?? 0;

    // A hero of a player that has a chooser learns what it names for each skill point, until a point is not spent
    void LearnSkills(unit hero)
    {
        if (!_skillChoosers.TryGetValue(hero.Owner, out var chooser) || hero.Dead) return;
        var outer = _choosing;
        _choosing = hero;
        try
        {
            for (var guard = 0; guard < 12 && hero.SkillPoints > 0; guard++)
            {
                var points = hero.SkillPoints;
                SelectHeroSkill(hero, chooser());
                if (hero.SkillPoints == points) break;
            }
        }
        finally { _choosing = outer; }
    }
}
