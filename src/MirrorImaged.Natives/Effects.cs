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
    /// <remarks>Close: measured in part (game-facts.md, Presentation: special effects (11-03)); the model name is ignored, since nothing is drawn; a null model is not measured.</remarks>
    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpecialEffect(string? modelName, float x, float y) => MakeEffect(x, y);

    /// <summary>As <c>AddSpecialEffect</c>, at a location's place; null for a null location.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: special effects (11-03)); the model name is ignored, since nothing is drawn; a null location is not measured.</remarks>
    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpecialEffectLoc(string? modelName, location? where) => where is null ? null : MakeEffect(where.X, where.Y);

    /// <summary>An effect on a widget reads x, y and z 0 wherever the widget is, and its scale 1 (facts, Presentation).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: special effects (11-03)); the model and attach point are ignored, since nothing is drawn; a null target is not measured.</remarks>
    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpecialEffectTarget(string? modelName, widget? targetWidget, string? attachPointName) => MakeEffect(0, 0, targetWidget);

    /// <summary>An effect at a point, made as <c>AddSpecialEffect</c> does; the ability string and effect type are ignored.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: special effects (11-03)); the ability's effect model is not looked up, and the handle and place are as for a special effect.</remarks>
    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffect(string? abilityString, effecttype? t, float x, float y) => MakeEffect(x, y);

    /// <summary>As <c>AddSpellEffect</c>, with the ability's id.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: special effects (11-03)); the ability's effect model is not looked up, and the handle and place are as for a special effect.</remarks>
    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffectById(int abilityId, effecttype? t, float x, float y) => MakeEffect(x, y);

    /// <summary>As <c>AddSpellEffectById</c>, at a location's place; null for a null location.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: special effects (11-03)); the ability's effect model is not looked up, and the handle and place are as for a special effect.</remarks>
    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffectByIdLoc(int abilityId, effecttype? t, location? where) => where is null ? null : MakeEffect(where.X, where.Y);

    /// <summary>As <c>AddSpellEffect</c>, at a location's place; null for a null location.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: special effects (11-03)); the ability's effect model is not looked up, and the handle and place are as for a special effect.</remarks>
    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffectLoc(string? abilityString, effecttype? t, location? where) => where is null ? null : MakeEffect(where.X, where.Y);

    /// <summary>An effect on a widget, made as <c>AddSpecialEffectTarget</c> does; the model and effect type are ignored.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: special effects (11-03)); the model is not looked up, and the handle and place are as for a special effect on a widget.</remarks>
    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffectTarget(string? modelName, effecttype? t, widget? targetWidget, string? attachPoint) => MakeEffect(0, 0, targetWidget);

    /// <summary>As <c>AddSpellEffectTarget</c>, with the ability's id.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: special effects (11-03)); the ability's effect model is not looked up, and the handle and place are as for a special effect.</remarks>
    [Native(Level.Close, Fact = EffectFacts)]
    public effect? AddSpellEffectTargetById(int abilityId, effecttype? t, widget? targetWidget, string? attachPoint) => MakeEffect(0, 0, targetWidget);

    /// <summary>Ends the effect, which plays its death where the model has one; no native reads that, and the place stays readable.</summary>
    /// <remarks>Recorded: The effect's death is drawn; the place and scale it read stay.</remarks>
    [Native(Level.Recorded, Reason = "The effect's death is drawn; the place and scale it read stay")]
    public void DestroyEffect(effect? whichEffect)
    {
        if (whichEffect is not null) whichEffect.Destroyed = true;
    }

    /// <summary>Removes the effect at once, with no death animation.</summary>
    /// <remarks>Recorded: The removal is drawn; the place and scale it read stay.</remarks>
    [Native(Level.Recorded, Reason = "The removal is drawn; the place and scale it read stay")]
    public void BlzRemoveEffect(effect? whichEffect) => DestroyEffect(whichEffect);

    /// <summary>The effect's x as it is set; 0 for one on a widget (facts, Presentation).</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: special effects (11-03)).</remarks>
    [Native(Level.Exact, Fact = EffectFacts)]
    public float BlzGetLocalSpecialEffectX(effect? whichEffect) => whichEffect?.X ?? 0;

    /// <summary>The effect's y as it is set; 0 for one on a widget and for a null effect.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: special effects (11-03)).</remarks>
    [Native(Level.Exact, Fact = EffectFacts)]
    public float BlzGetLocalSpecialEffectY(effect? whichEffect) => whichEffect?.Y ?? 0;

    /// <summary>
    /// The height set, else the z of the place: the ground there when it was made, then whatever BlzSetSpecialEffectZ or
    /// BlzSetSpecialEffectPosition set; a move by x or y alone keeps the z, and a height reads until a place is set after it.
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: special effects (11-03)).</remarks>
    [Native(Level.Exact, Fact = EffectFacts)]
    public float BlzGetLocalSpecialEffectZ(effect? whichEffect) => whichEffect is null ? 0 : whichEffect.Height ?? whichEffect.Z;

    /// <summary>The effect's scale, 1 until set; 0 for a null effect.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: special effects (11-03)).</remarks>
    [Native(Level.Exact, Fact = EffectFacts)]
    public float BlzGetSpecialEffectScale(effect? whichEffect) => whichEffect?.Scale ?? 0;

    /// <summary>Sets the scale <c>BlzGetSpecialEffectScale</c> reads; nothing for a null effect.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: special effects (11-03)).</remarks>
    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectScale(effect? whichEffect, float scale)
    {
        if (whichEffect is not null) whichEffect.Scale = scale;
    }

    /// <summary>Moves the effect's x, and ends a height set before.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: special effects (11-03)).</remarks>
    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectX(effect? whichEffect, float x)
    {
        if (whichEffect is null) return;
        whichEffect.X = x;
        whichEffect.Height = null;
    }

    /// <summary>Moves the effect's y, and ends a height set before.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: special effects (11-03)).</remarks>
    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectY(effect? whichEffect, float y)
    {
        if (whichEffect is null) return;
        whichEffect.Y = y;
        whichEffect.Height = null;
    }

    /// <summary>Sets the effect's z, and ends a height set before.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: special effects (11-03)).</remarks>
    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectZ(effect? whichEffect, float z)
    {
        if (whichEffect is null) return;
        whichEffect.Z = z;
        whichEffect.Height = null;
    }

    /// <summary>The z read is the height, on flat ground and a hill alike, until x, y, z or the position is set again (facts, Presentation).</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: special effects (11-03)).</remarks>
    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectHeight(effect? whichEffect, float height)
    {
        if (whichEffect is not null) whichEffect.Height = height;
    }

    /// <summary>Sets the effect's x, y and z together, and ends a height set before.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: special effects (11-03)).</remarks>
    [Native(Level.Exact, Fact = EffectFacts)]
    public void BlzSetSpecialEffectPosition(effect? whichEffect, float x, float y, float z)
    {
        if (whichEffect is null) return;
        (whichEffect.X, whichEffect.Y, whichEffect.Z, whichEffect.Height) = (x, y, z, null);
    }

    /// <summary>Close: the z set isn't measured; it stays, as for x and y alone.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: special effects (11-03)); a location has no z, so the effect's z stays; the game's z for it is not measured.</remarks>
    [Native(Level.Close, Fact = EffectFacts)]
    public void BlzSetSpecialEffectPositionLoc(effect? whichEffect, location? loc)
    {
        if (whichEffect is null || loc is null) return;
        (whichEffect.X, whichEffect.Y, whichEffect.Height) = (loc.X, loc.Y, null);
    }

    const string EffectDrawn = "How the effect is drawn (colour, turn, animation, time); no native reads it back";

    /// <summary>Changes nothing; the alpha is not drawn or kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectAlpha(effect? whichEffect, int alpha) { }
    /// <summary>Changes nothing; the animation is not played or kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectAnimation(effect? whichEffect, string? whichAnimation) { }
    /// <summary>Changes nothing; the blend time is not kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectAnimationBlendTime(effect? whichEffect, float blendTime) { }
    /// <summary>Changes nothing; the colour is not drawn or kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectColor(effect? whichEffect, int r, int g, int b) { }
    /// <summary>Changes nothing; the player's colour is not drawn or kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectColorByPlayer(effect? whichEffect, player? whichPlayer) { }
    /// <summary>Changes nothing; the scale on each axis is not drawn or kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectMatrixScale(effect? whichEffect, float x, float y, float z) { }
    /// <summary>Changes nothing; the turn is not drawn or kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectOrientation(effect? whichEffect, float yaw, float pitch, float roll) { }
    /// <summary>Changes nothing; the pitch is not drawn or kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectPitch(effect? whichEffect, float pitch) { }
    /// <summary>Changes nothing; the roll is not drawn or kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectRoll(effect? whichEffect, float roll) { }
    /// <summary>Changes nothing; the yaw is not drawn or kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectYaw(effect? whichEffect, float yaw) { }
    /// <summary>Changes nothing; the animation time is not kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectTime(effect? whichEffect, float time) { }
    /// <summary>Changes nothing; the animation speed is not kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSetSpecialEffectTimeScale(effect? whichEffect, float timeScale) { }
    /// <summary>Changes nothing; no animation plays.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzPlaySpecialEffect(effect? whichEffect, animtype? whichAnim) { }
    /// <summary>Changes nothing; no animation plays.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzPlaySpecialEffectWithTimeScale(effect? whichEffect, animtype? whichAnim, float timeScale) { }
    /// <summary>Changes nothing; no animation is queued.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzQueueSpecialEffectAnimation(effect? whichEffect, string? whichAnimation) { }
    /// <summary>Changes nothing; no matrix is kept to reset.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzResetSpecialEffectMatrix(effect? whichEffect) { }
    /// <summary>Changes nothing; no sub-animation is kept.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSpecialEffectAddSubAnimation(effect? whichEffect, subanimtype? whichSubAnim) { }
    /// <summary>Changes nothing; none is kept to clear.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSpecialEffectClearSubAnimations(effect? whichEffect) { }
    /// <summary>Changes nothing; none is kept to remove.</summary>
    /// <remarks>Recorded: How the effect is drawn (colour, turn, animation, time); no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = EffectDrawn)] public void BlzSpecialEffectRemoveSubAnimation(effect? whichEffect, subanimtype? whichSubAnim) { }

    // A bolt is made for any code (the game draws only the ones it knows); it reads its colour back as set, though it draws plain
    lightning? MakeBolt(string? codeName) => string.IsNullOrEmpty(codeName) ? null : Make(new lightning());

    /// <summary>A bolt for any non-empty code, drawn plain, its colour starting at 1, 1, 1, 1; null for a null or empty code.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: SetLightningColor reads back (11-06)); the places and the visibility check are ignored, and a null or empty code answering null is not measured.</remarks>
    [Native(Level.Close, Fact = LightningFacts)]
    public lightning? AddLightning(string? codeName, bool checkVisibility, float x1, float y1, float x2, float y2) => MakeBolt(codeName);

    /// <summary>As <c>AddLightning</c>, with a z for each end.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: SetLightningColor reads back (11-06)); the places and the visibility check are ignored, and a null or empty code answering null is not measured.</remarks>
    [Native(Level.Close, Fact = LightningFacts)]
    public lightning? AddLightningEx(string? codeName, bool checkVisibility, float x1, float y1, float z1, float x2, float y2, float z2) => MakeBolt(codeName);

    /// <summary>True when the bolt was alive, false for one already destroyed.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: SetLightningColor reads back (11-06)); the game's answer for a null bolt is not measured.</remarks>
    [Native(Level.Close, Fact = LightningFacts)]
    public bool DestroyLightning(lightning? whichBolt)
    {
        if (whichBolt is null || whichBolt.Destroyed) return false;
        whichBolt.Destroyed = true;
        return true;
    }

    /// <summary>True while the bolt is alive; the new places are not kept.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: SetLightningColor reads back (11-06)); the new places and the visibility check are not kept, and the answer for a destroyed bolt is not measured.</remarks>
    [Native(Level.Close, Fact = LightningFacts)]
    public bool MoveLightning(lightning? whichBolt, bool checkVisibility, float x1, float y1, float x2, float y2) => whichBolt is { Destroyed: false };

    /// <summary>As <c>MoveLightning</c>, with a z for each end.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: SetLightningColor reads back (11-06)); the new places and the visibility check are not kept, and the answer for a destroyed bolt is not measured.</remarks>
    [Native(Level.Close, Fact = LightningFacts)]
    public bool MoveLightningEx(lightning? whichBolt, bool checkVisibility, float x1, float y1, float z1, float x2, float y2, float z2) => whichBolt is { Destroyed: false };

    /// <summary>Answers true and reads the colour back, also after a move; the game draws the bolt plain all the same (facts, Presentation).</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: SetLightningColor reads back (11-06)).</remarks>
    [Native(Level.Exact, Fact = LightningFacts)]
    public bool SetLightningColor(lightning? whichBolt, float r, float g, float b, float a)
    {
        if (whichBolt is not { Destroyed: false }) return false;
        (whichBolt.Red, whichBolt.Green, whichBolt.Blue, whichBolt.Alpha) = (r, g, b, a);
        return true;
    }

    /// <summary>The red of the bolt's colour, 1 until set; 0 for a null bolt.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: SetLightningColor reads back (11-06)).</remarks>
    [Native(Level.Exact, Fact = LightningFacts)]
    public float GetLightningColorR(lightning? whichBolt) => whichBolt?.Red ?? 0;

    /// <summary>The green of the bolt's colour, 1 until set; 0 for a null bolt.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: SetLightningColor reads back (11-06)).</remarks>
    [Native(Level.Exact, Fact = LightningFacts)]
    public float GetLightningColorG(lightning? whichBolt) => whichBolt?.Green ?? 0;

    /// <summary>The blue of the bolt's colour, 1 until set; 0 for a null bolt.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: SetLightningColor reads back (11-06)).</remarks>
    [Native(Level.Exact, Fact = LightningFacts)]
    public float GetLightningColorB(lightning? whichBolt) => whichBolt?.Blue ?? 0;

    /// <summary>The alpha of the bolt's colour, 1 until set; 0 for a null bolt.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Presentation: SetLightningColor reads back (11-06)).</remarks>
    [Native(Level.Exact, Fact = LightningFacts)]
    public float GetLightningColorA(lightning? whichBolt) => whichBolt?.Alpha ?? 0;

    /// <summary>Keeps the flag <c>IsCineFilterDisplayed</c> reads; nothing is drawn.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: nothing else reads the filter); the filter itself is not drawn, and the flag's value before any call is not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Presentation: nothing else reads the filter")]
    public void DisplayCineFilter(bool flag) => _cineFilter = flag;

    /// <summary>Whether <c>DisplayCineFilter</c> last turned the filter on; false before any call.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: nothing else reads the filter); the value before any call is the model's, not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Presentation: nothing else reads the filter")]
    public bool IsCineFilterDisplayed() => _cineFilter;

    /// <summary>Lets the unit show a hero glow again; only the setting is kept.</summary>
    /// <remarks>Close: the setting is kept per unit and no glow is drawn; the game's own effect is not measured.</remarks>
    [Native(Level.Close)]
    public void AllowHeroGlowOnUnit(unit? whichUnit)
    {
        if (whichUnit is not null) _noGlow.Remove(whichUnit);
    }

    /// <summary>Stops the unit showing a hero glow; only the setting is kept.</summary>
    /// <remarks>Close: the setting is kept per unit and no glow is drawn; the game's own effect is not measured.</remarks>
    [Native(Level.Close)]
    public void DisallowHeroGlowOnUnit(unit? whichUnit)
    {
        if (whichUnit is not null) _noGlow.Add(whichUnit);
    }

    /// <summary>True until DisallowHeroGlowOnUnit; Close: the game's default for a unit that isn't a hero isn't measured.</summary>
    /// <remarks>Close: the setting is kept per unit and no glow is drawn; the game's own effect is not measured. A null unit gives false.</remarks>
    [Native(Level.Close)]
    public bool HeroGlowIsAllowedOnUnit(unit? whichUnit) => whichUnit is not null && !_noGlow.Contains(whichUnit);

    /// <summary>Answers true; the minimap is not drawn.</summary>
    /// <remarks>Recorded: The minimap's terrain picture is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The minimap's terrain picture is drawn; no native reads it back")]
    public bool BlzChangeMinimapTerrainTex(string? texFile) => true;
}
