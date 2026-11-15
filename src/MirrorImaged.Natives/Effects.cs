namespace MirrorImaged.Natives;

public partial class effect
{
    internal float X, Y, Z, Scale = 1;
    internal float? Height;   // BlzSetSpecialEffectHeight's, read as the z until another place is set
    internal widget? Target;  // the widget an effect made on one stays on (its place reads 0)
    internal bool Destroyed;
}

public partial class lightning
{
    internal float Red = 1, Green = 1, Blue = 1, Alpha = 1;
    internal bool Destroyed;
}

// Special effects and lightning: where they are and how they are scaled, which the getters read (game-facts, "Presentation":
// probes of 10-19 to 11-12), the rest of what they draw Recorded.
public sealed partial class GameNatives
{
    const string EffectFacts = "game-facts.md, Presentation: special effects (11-03)";
    const string LightningFacts = "game-facts.md, Presentation: SetLightningColor reads back (11-06)";

    bool _cineFilter;
    readonly HashSet<unit> _noGlow = [];

    effect? MakeEffect(float x, float y, widget? target = null) =>
        Make(new effect { X = target is null ? x : 0, Y = target is null ? y : 0, Z = target is null ? Ground(x, y) : 0, Target = target });

    /// <summary>
    /// An effect at the point on the ground there (z is the ground's height, as the game made it on raised ground), a
    /// handle for any model, also one that draws nothing (facts, Presentation).
    /// </summary>
    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpecialEffect(string? modelName, float x, float y) => MakeEffect(x, y);

    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpecialEffectLoc(string? modelName, location? where) => where is null ? null : MakeEffect(where.X, where.Y);

    /// <summary>An effect on a widget reads x, y and z 0 wherever the widget is, and its scale 1 (facts, Presentation).</summary>
    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpecialEffectTarget(string? modelName, widget? targetWidget, string? attachPointName) => MakeEffect(0, 0, targetWidget);

    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffect(string? abilityString, effecttype? t, float x, float y) => MakeEffect(x, y);

    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffectById(int abilityId, effecttype? t, float x, float y) => MakeEffect(x, y);

    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffectByIdLoc(int abilityId, effecttype? t, location? where) => where is null ? null : MakeEffect(where.X, where.Y);

    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffectLoc(string? abilityString, effecttype? t, location? where) => where is null ? null : MakeEffect(where.X, where.Y);

    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffectTarget(string? modelName, effecttype? t, widget? targetWidget, string? attachPoint) => MakeEffect(0, 0, targetWidget);

    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffectTargetById(int abilityId, effecttype? t, widget? targetWidget, string? attachPoint) => MakeEffect(0, 0, targetWidget);

    /// <summary>Ends the effect, which plays its death where the model has one; no native reads that, and the place stays readable.</summary>
    [Native(Level.Recorded, Reason = "The effect's death is drawn; the place and scale it read stay")]
    public void DestroyEffect(effect? whichEffect)
    {
        if (whichEffect is not null) whichEffect.Destroyed = true;
    }

    /// <summary>Removes the effect at once, with no death animation.</summary>
    [Native(Level.Recorded, Reason = "The removal is drawn; the place and scale it read stay")]
    public void BlzRemoveEffect(effect? whichEffect) => DestroyEffect(whichEffect);

    /// <summary>The effect's x as it is set; 0 for one on a widget (facts, Presentation).</summary>
    [Native(Level.Exact, Fact = EffectFacts)]
    public float BlzGetLocalSpecialEffectX(effect? whichEffect) => whichEffect?.X ?? 0;

    [Native(Level.Exact, Fact = EffectFacts)]
    public float BlzGetLocalSpecialEffectY(effect? whichEffect) => whichEffect?.Y ?? 0;

    /// <summary>
    /// The height set, else the z of the place: the ground there when it was made, then whatever BlzSetSpecialEffectZ or
    /// BlzSetSpecialEffectPosition set; a move by x or y alone keeps the z, and a height reads until a place is set after it.
    /// </summary>
    [Native(Level.Exact, Fact = EffectFacts)]
    public float BlzGetLocalSpecialEffectZ(effect? whichEffect) => whichEffect is null ? 0 : whichEffect.Height ?? whichEffect.Z;

    [Native(Level.Exact, Fact = EffectFacts)]
    public float BlzGetSpecialEffectScale(effect? whichEffect) => whichEffect?.Scale ?? 0;

    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectScale(effect? whichEffect, float scale)
    {
        if (whichEffect is not null) whichEffect.Scale = scale;
    }

    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectX(effect? whichEffect, float x)
    {
        if (whichEffect is null) return;
        whichEffect.X = x;
        whichEffect.Height = null;
    }

    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectY(effect? whichEffect, float y)
    {
        if (whichEffect is null) return;
        whichEffect.Y = y;
        whichEffect.Height = null;
    }

    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectZ(effect? whichEffect, float z)
    {
        if (whichEffect is null) return;
        whichEffect.Z = z;
        whichEffect.Height = null;
    }

    /// <summary>The z read is the height, on flat ground and a hill alike, until x, y, z or the position is set again (facts, Presentation).</summary>
    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectHeight(effect? whichEffect, float height)
    {
        if (whichEffect is not null) whichEffect.Height = height;
    }

    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectPosition(effect? whichEffect, float x, float y, float z)
    {
        if (whichEffect is null) return;
        (whichEffect.X, whichEffect.Y, whichEffect.Z, whichEffect.Height) = (x, y, z, null);
    }

    /// <summary>Close: the z set isn't measured; it stays, as for x and y alone.</summary>
    [Native(Level.Close, Fact = EffectFacts)]
    public void BlzSetSpecialEffectPositionLoc(effect? whichEffect, location? loc)
    {
        if (whichEffect is null || loc is null) return;
        (whichEffect.X, whichEffect.Y, whichEffect.Height) = (loc.X, loc.Y, null);
    }

    const string EffectDrawn = "How the effect is drawn (colour, turn, animation, time); no native reads it back";

    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectAlpha(effect? whichEffect, int alpha) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectAnimation(effect? whichEffect, string? whichAnimation) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectAnimationBlendTime(effect? whichEffect, float blendTime) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectColor(effect? whichEffect, int r, int g, int b) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectColorByPlayer(effect? whichEffect, player? whichPlayer) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectMatrixScale(effect? whichEffect, float x, float y, float z) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectOrientation(effect? whichEffect, float yaw, float pitch, float roll) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectPitch(effect? whichEffect, float pitch) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectRoll(effect? whichEffect, float roll) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectYaw(effect? whichEffect, float yaw) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectTime(effect? whichEffect, float time) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectTimeScale(effect? whichEffect, float timeScale) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzPlaySpecialEffect(effect? whichEffect, animtype? whichAnim) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzPlaySpecialEffectWithTimeScale(effect? whichEffect, animtype? whichAnim, float timeScale) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzQueueSpecialEffectAnimation(effect? whichEffect, string? whichAnimation) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzResetSpecialEffectMatrix(effect? whichEffect) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSpecialEffectAddSubAnimation(effect? whichEffect, subanimtype? whichSubAnim) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSpecialEffectClearSubAnimations(effect? whichEffect) { }
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSpecialEffectRemoveSubAnimation(effect? whichEffect, subanimtype? whichSubAnim) { }

    // A bolt is made for any code (the game draws only the ones it knows); it reads its colour back as set, though it draws plain
    lightning? MakeBolt(string? codeName) => string.IsNullOrEmpty(codeName) ? null : Make(new lightning());

    [Native(Level.Close, Fact = LightningFacts)]
    public lightning? AddLightning(string? codeName, bool checkVisibility, float x1, float y1, float x2, float y2) => MakeBolt(codeName);

    [Native(Level.Close, Fact = LightningFacts)]
    public lightning? AddLightningEx(string? codeName, bool checkVisibility, float x1, float y1, float z1, float x2, float y2, float z2) => MakeBolt(codeName);

    /// <summary>True when the bolt was alive, false for one already destroyed.</summary>
    [Native(Level.Close, Fact = LightningFacts)]
    public bool DestroyLightning(lightning? whichBolt)
    {
        if (whichBolt is null || whichBolt.Destroyed) return false;
        whichBolt.Destroyed = true;
        return true;
    }

    [Native(Level.Close, Fact = LightningFacts)]
    public bool MoveLightning(lightning? whichBolt, bool checkVisibility, float x1, float y1, float x2, float y2) => whichBolt is { Destroyed: false };

    [Native(Level.Close, Fact = LightningFacts)]
    public bool MoveLightningEx(lightning? whichBolt, bool checkVisibility, float x1, float y1, float z1, float x2, float y2, float z2) => whichBolt is { Destroyed: false };

    /// <summary>Answers true and reads the colour back, also after a move; the game draws the bolt plain all the same (facts, Presentation).</summary>
    [Native(Level.Exact, Fact = LightningFacts)]
    public bool SetLightningColor(lightning? whichBolt, float r, float g, float b, float a)
    {
        if (whichBolt is not { Destroyed: false }) return false;
        (whichBolt.Red, whichBolt.Green, whichBolt.Blue, whichBolt.Alpha) = (r, g, b, a);
        return true;
    }

    [Native(Level.Exact, Fact = LightningFacts)]
    public float GetLightningColorR(lightning? whichBolt) => whichBolt?.Red ?? 0;

    [Native(Level.Exact, Fact = LightningFacts)]
    public float GetLightningColorG(lightning? whichBolt) => whichBolt?.Green ?? 0;

    [Native(Level.Exact, Fact = LightningFacts)]
    public float GetLightningColorB(lightning? whichBolt) => whichBolt?.Blue ?? 0;

    [Native(Level.Exact, Fact = LightningFacts)]
    public float GetLightningColorA(lightning? whichBolt) => whichBolt?.Alpha ?? 0;

    [Native(Level.Close, Fact = "game-facts.md, Presentation: nothing else reads the filter")]
    public void DisplayCineFilter(bool flag) => _cineFilter = flag;

    [Native(Level.Close, Fact = "game-facts.md, Presentation: nothing else reads the filter")]
    public bool IsCineFilterDisplayed() => _cineFilter;

    [Native(Level.Close)]
    public void AllowHeroGlowOnUnit(unit? whichUnit)
    {
        if (whichUnit is not null) _noGlow.Remove(whichUnit);
    }

    [Native(Level.Close)]
    public void DisallowHeroGlowOnUnit(unit? whichUnit)
    {
        if (whichUnit is not null) _noGlow.Add(whichUnit);
    }

    /// <summary>True until DisallowHeroGlowOnUnit; Close: the game's default for a unit that isn't a hero isn't measured.</summary>
    [Native(Level.Close)]
    public bool HeroGlowIsAllowedOnUnit(unit? whichUnit) => whichUnit is not null && !_noGlow.Contains(whichUnit);

    [Native(Level.Recorded, Reason = "The minimap's terrain picture is drawn; no native reads it back")]
    public bool BlzChangeMinimapTerrainTex(string? texFile) => true;
}
