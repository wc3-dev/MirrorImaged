namespace MirrorImaged.Natives;

// What the game draws and nothing reads back (the facts; game-facts.md, Presentation): floating text, weather, images, splats, minimap icons, cinematics, animations and indicators. Recorded: kept by their handles, not simulated.
public sealed partial class GameNatives
{
    /// <summary>Makes a floating text handle; nothing is drawn.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public texttag? CreateTextTag() => Make(new texttag());

    /// <summary>Changes nothing; the handle is not destroyed.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void DestroyTextTag(texttag? t) { }

    /// <summary>Changes nothing; the age is not kept.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagAge(texttag? t, float age) { }

    /// <summary>Changes nothing; the colour is not kept.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagColor(texttag? t, int red, int green, int blue, int alpha) { }

    /// <summary>Changes nothing; the fade point is not kept.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagFadepoint(texttag? t, float fadepoint) { }

    /// <summary>Changes nothing; the lifespan is not kept.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagLifespan(texttag? t, float lifespan) { }

    /// <summary>Changes nothing; the flag is not kept.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagPermanent(texttag? t, bool flag) { }

    /// <summary>Changes nothing; the place is not kept.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagPos(texttag? t, float x, float y, float heightOffset) { }

    /// <summary>Changes nothing; the place is not kept.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagPosUnit(texttag? t, unit? whichUnit, float heightOffset) { }

    /// <summary>Changes nothing; the flag is not kept.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagSuspended(texttag? t, bool flag) { }

    /// <summary>Changes nothing; the text is not kept.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagText(texttag? t, string? s, float height) { }

    /// <summary>Changes nothing; the speed is not kept.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagVelocity(texttag? t, float xvel, float yvel) { }

    /// <summary>Changes nothing; the flag is not kept.</summary>
    /// <remarks>Recorded: Floating text is drawn on the screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagVisibility(texttag? t, bool flag) { }

    /// <summary>Makes a weather handle; no weather is drawn.</summary>
    /// <remarks>Recorded: Weather is drawn over a rect; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Weather is drawn over a rect; no native reads it back")]
    public weathereffect? AddWeatherEffect(rect? where, int effectID) => Make(new weathereffect());

    /// <summary>Changes nothing; no weather is switched.</summary>
    /// <remarks>Recorded: Weather is drawn over a rect; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Weather is drawn over a rect; no native reads it back")]
    public void EnableWeatherEffect(weathereffect? whichEffect, bool enable) { }

    /// <summary>Changes nothing; the handle is not removed.</summary>
    /// <remarks>Recorded: Weather is drawn over a rect; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Weather is drawn over a rect; no native reads it back")]
    public void RemoveWeatherEffect(weathereffect? whichEffect) { }

    /// <summary>Makes an image handle; nothing is drawn.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public image? CreateImage(string? file, float sizeX, float sizeY, float sizeZ, float posX, float posY, float posZ, float originX, float originY, float originZ, int imageType) => Make(new image());

    /// <summary>Changes nothing; the handle is not destroyed.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void DestroyImage(image? whichImage) { }

    /// <summary>Changes nothing; the flag is not kept.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void ShowImage(image? whichImage, bool flag) { }

    /// <summary>Changes nothing; the setting is not kept.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageAboveWater(image? whichImage, bool flag, bool useWaterAlpha) { }

    /// <summary>Changes nothing; the colour is not kept.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageColor(image? whichImage, int red, int green, int blue, int alpha) { }

    /// <summary>Changes nothing; the height is not kept.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageConstantHeight(image? whichImage, bool flag, float height) { }

    /// <summary>Changes nothing; the place is not kept.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImagePosition(image? whichImage, float x, float y, float z) { }

    /// <summary>Changes nothing; the flag is not kept.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageRender(image? whichImage, bool flag) { }

    /// <summary>Changes nothing; the flag is not kept.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageRenderAlways(image? whichImage, bool flag) { }

    /// <summary>Changes nothing; the type is not kept.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageType(image? whichImage, int imageType) { }

    /// <summary>Makes a splat handle; nothing is drawn.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public ubersplat? CreateUbersplat(float x, float y, string? name, int red, int green, int blue, int alpha, bool forcePaused, bool noBirthTime) => Make(new ubersplat());

    /// <summary>Changes nothing; the handle is not destroyed.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void DestroyUbersplat(ubersplat? whichSplat) { }

    /// <summary>Changes nothing; the splat is not finished.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void FinishUbersplat(ubersplat? whichSplat) { }

    /// <summary>Changes nothing; the splat is not reset.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void ResetUbersplat(ubersplat? whichSplat) { }

    /// <summary>Changes nothing; the flag is not kept.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void ShowUbersplat(ubersplat? whichSplat, bool flag) { }

    /// <summary>Changes nothing; the flag is not kept.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetUbersplatRender(ubersplat? whichSplat, bool flag) { }

    /// <summary>Changes nothing; the flag is not kept.</summary>
    /// <remarks>Recorded: Images and ubersplats are drawn on the ground; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetUbersplatRenderAlways(ubersplat? whichSplat, bool flag) { }

    /// <summary>Makes an icon handle; nothing is drawn.</summary>
    /// <remarks>Recorded: The minimap is drawn on the screen; no native reads its icons or pings back.</remarks>
    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public minimapicon? CreateMinimapIcon(float x, float y, int red, int green, int blue, string? pingPath, fogstate? fogVisibility) => Make(new minimapicon());

    /// <summary>Makes an icon handle; nothing is drawn.</summary>
    /// <remarks>Recorded: The minimap is drawn on the screen; no native reads its icons or pings back.</remarks>
    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public minimapicon? CreateMinimapIconAtLoc(location? where, int red, int green, int blue, string? pingPath, fogstate? fogVisibility) => Make(new minimapicon());

    /// <summary>Makes an icon handle; nothing is drawn.</summary>
    /// <remarks>Recorded: The minimap is drawn on the screen; no native reads its icons or pings back.</remarks>
    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public minimapicon? CreateMinimapIconOnUnit(unit? whichUnit, int red, int green, int blue, string? pingPath, fogstate? fogVisibility) => Make(new minimapicon());

    /// <summary>Changes nothing; the handle is not destroyed.</summary>
    /// <remarks>Recorded: The minimap is drawn on the screen; no native reads its icons or pings back.</remarks>
    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void DestroyMinimapIcon(minimapicon? pingId) { }

    /// <summary>Changes nothing; the setting is not kept.</summary>
    /// <remarks>Recorded: The minimap is drawn on the screen; no native reads its icons or pings back.</remarks>
    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void SetMinimapIconOrphanDestroy(minimapicon? whichMinimapIcon, bool doDestroy) { }

    /// <summary>Changes nothing; the flag is not kept.</summary>
    /// <remarks>Recorded: The minimap is drawn on the screen; no native reads its icons or pings back.</remarks>
    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void SetMinimapIconVisible(minimapicon? whichMinimapIcon, bool visible) { }

    /// <summary>Changes nothing; the icon is not kept.</summary>
    /// <remarks>Recorded: The minimap is drawn on the screen; no native reads its icons or pings back.</remarks>
    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void SetAltMinimapIcon(string? iconPath) { }

    /// <summary>Changes nothing; no ping is drawn.</summary>
    /// <remarks>Recorded: The minimap is drawn on the screen; no native reads its icons or pings back.</remarks>
    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void PingMinimap(float x, float y, float duration) { }

    /// <summary>Changes nothing; no ping is drawn.</summary>
    /// <remarks>Recorded: The minimap is drawn on the screen; no native reads its icons or pings back.</remarks>
    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void PingMinimapEx(float x, float y, float duration, int red, int green, int blue, bool extraEffects) { }

    /// <summary>Changes nothing; the mode is not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterBlendMode(blendmode? whichMode) { }

    /// <summary>Changes nothing; the duration is not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterDuration(float duration) { }

    /// <summary>Changes nothing; the colour is not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterEndColor(int red, int green, int blue, int alpha) { }

    /// <summary>Changes nothing; the coordinates are not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterEndUV(float minu, float minv, float maxu, float maxv) { }

    /// <summary>Changes nothing; the colour is not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterStartColor(int red, int green, int blue, int alpha) { }

    /// <summary>Changes nothing; the coordinates are not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterStartUV(float minu, float minv, float maxu, float maxv) { }

    /// <summary>Changes nothing; the flags are not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterTexMapFlags(texmapflags? whichFlags) { }

    /// <summary>Changes nothing; the texture is not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterTexture(string? filename) { }

    /// <summary>Changes nothing; no movie plays.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void PlayCinematic(string? movieName) { }

    /// <summary>Changes nothing; no model plays.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void PlayModelCinematic(string? modelName) { }

    /// <summary>Changes nothing; the setting is not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCinematicAudio(bool cinematicAudio) { }

    /// <summary>Changes nothing; no scene is shown.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCinematicScene(int portraitUnitId, playercolor? color, string? speakerTitle, string? text, float sceneDuration, float voiceoverDuration) { }

    /// <summary>Changes nothing; no scene ends.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void EndCinematicScene() { }

    /// <summary>Changes nothing; the setting is not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void ForceCinematicSubtitles(bool flag) { }

    /// <summary>Changes nothing; the model is not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetIntroShotModel(string? introModelPath) { }

    /// <summary>Changes nothing; the text is not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetIntroShotText(string? introText) { }

    /// <summary>Changes nothing; the light is not kept.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetPortraitLight(string? portraitDNCFile) { }

    /// <summary>Changes nothing; no messages are cleared.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void ClearTextMessages() { }

    /// <summary>Changes nothing; no text is shown.</summary>
    /// <remarks>Recorded: The cinematic filter, scenes and the screen's text are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void DisplayTimedTextFromPlayer(player? toPlayer, float x, float y, float duration, string? message) { }

    /// <summary>Changes nothing; no animation plays.</summary>
    /// <remarks>Recorded: Animations are drawn on the model; no native reads which one plays back (the durations come with the models).</remarks>
    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitAnimation(unit? whichUnit, string? whichAnimation) { }

    /// <summary>Changes nothing; no animation plays.</summary>
    /// <remarks>Recorded: Animations are drawn on the model; no native reads which one plays back (the durations come with the models).</remarks>
    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitAnimationByIndex(unit? whichUnit, int whichAnimation) { }

    /// <summary>Changes nothing; no animation plays.</summary>
    /// <remarks>Recorded: Animations are drawn on the model; no native reads which one plays back (the durations come with the models).</remarks>
    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitAnimationWithRarity(unit? whichUnit, string? whichAnimation, raritycontrol? rarity) { }

    /// <summary>Changes nothing; no animation is queued.</summary>
    /// <remarks>Recorded: Animations are drawn on the model; no native reads which one plays back (the durations come with the models).</remarks>
    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void QueueUnitAnimation(unit? whichUnit, string? whichAnimation) { }

    /// <summary>Changes nothing; the blend time is not kept.</summary>
    /// <remarks>Recorded: Animations are drawn on the model; no native reads which one plays back (the durations come with the models).</remarks>
    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitBlendTime(unit? whichUnit, float blendTime) { }

    /// <summary>Changes nothing; the properties are not kept.</summary>
    /// <remarks>Recorded: Animations are drawn on the model; no native reads which one plays back (the durations come with the models).</remarks>
    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void AddUnitAnimationProperties(unit? whichUnit, string? animProperties, bool add) { }

    /// <summary>Changes nothing; the speed is not kept.</summary>
    /// <remarks>Recorded: Animations are drawn on the model; no native reads which one plays back (the durations come with the models).</remarks>
    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitTimeScale(unit? whichUnit, float timeScale) { }

    /// <summary>Changes nothing; the model does not turn.</summary>
    /// <remarks>Recorded: Animations are drawn on the model; no native reads which one plays back (the durations come with the models).</remarks>
    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitLookAt(unit? whichUnit, string? whichBone, unit? lookAtTarget, float offsetX, float offsetY, float offsetZ) { }

    /// <summary>Changes nothing; there is no look-at to reset.</summary>
    /// <remarks>Recorded: Animations are drawn on the model; no native reads which one plays back (the durations come with the models).</remarks>
    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void ResetUnitLookAt(unit? whichUnit) { }

    /// <summary>Changes nothing; no flash is drawn.</summary>
    /// <remarks>Recorded: Indicators, command button effects and tints are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public void AddIndicator(widget? whichWidget, int red, int green, int blue, int alpha) { }

    /// <summary>Changes nothing; no flash is drawn.</summary>
    /// <remarks>Recorded: Indicators, command button effects and tints are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public void UnitAddIndicator(unit? whichUnit, int red, int green, int blue, int alpha) { }

    /// <summary>Makes an effect handle; nothing is drawn.</summary>
    /// <remarks>Recorded: Indicators, command button effects and tints are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public commandbuttoneffect? CreateCommandButtonEffect(int abilityId, string? order) => Make(new commandbuttoneffect());

    /// <summary>Makes an effect handle; nothing is drawn.</summary>
    /// <remarks>Recorded: Indicators, command button effects and tints are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public commandbuttoneffect? CreateUpgradeCommandButtonEffect(int whichUprgade) => Make(new commandbuttoneffect());

    /// <summary>Makes an effect handle; nothing is drawn.</summary>
    /// <remarks>Recorded: Indicators, command button effects and tints are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public commandbuttoneffect? CreateLearnCommandButtonEffect(int abilityId) => Make(new commandbuttoneffect());

    /// <summary>Changes nothing; the handle is not destroyed.</summary>
    /// <remarks>Recorded: Indicators, command button effects and tints are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public void DestroyCommandButtonEffect(commandbuttoneffect? whichEffect) { }

    /// <summary>Changes nothing; the tint is not kept.</summary>
    /// <remarks>Recorded: Indicators, command button effects and tints are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public void SetItemColor(item? whichItem, playercolor? whichColor) { }

    /// <summary>Changes nothing; the tint is not kept.</summary>
    /// <remarks>Recorded: Indicators, command button effects and tints are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public void BlzSetItemColor(item? whichItem, playercolor? whichColor) { }

    /// <summary>Makes a trackable handle; no event ever fires for it.</summary>
    /// <remarks>Recorded: Its hit and track events come from the mouse over the model: the input model's mouse (Simulation.Mouse) gives a place on the ground, and what is missing is the model's hit area under it, which no probe has measured; no event fires for it (D795).</remarks>
    [Native(Level.Recorded, Reason = "Its hit and track events come from the mouse over the model: the input model's mouse (Simulation.Mouse) gives a place on the ground, and what is missing is the model's hit area under it, which no probe has measured; no event fires for it (D795)")]
    public trackable? CreateTrackable(string? trackableModelPath, float x, float y, float facing) => Make(new trackable());
}
