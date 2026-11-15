namespace MirrorImaged.Natives;

// What the game draws and nothing reads back (docs/facts.md; game-facts.md, Presentation): floating text, weather, images, splats, minimap icons, cinematics, animations and indicators. Recorded: kept by their handles, not simulated.
public sealed partial class GameNatives
{
    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public texttag? CreateTextTag() => Make(new texttag());

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void DestroyTextTag(texttag? t) { }

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagAge(texttag? t, float age) { }

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagColor(texttag? t, int red, int green, int blue, int alpha) { }

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagFadepoint(texttag? t, float fadepoint) { }

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagLifespan(texttag? t, float lifespan) { }

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagPermanent(texttag? t, bool flag) { }

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagPos(texttag? t, float x, float y, float heightOffset) { }

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagPosUnit(texttag? t, unit? whichUnit, float heightOffset) { }

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagSuspended(texttag? t, bool flag) { }

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagText(texttag? t, string? s, float height) { }

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagVelocity(texttag? t, float xvel, float yvel) { }

    [Native(Level.Recorded, Reason = "Floating text is drawn on the screen; no native reads it back")]
    public void SetTextTagVisibility(texttag? t, bool flag) { }

    [Native(Level.Recorded, Reason = "Weather is drawn over a rect; no native reads it back")]
    public weathereffect? AddWeatherEffect(rect? where, int effectID) => Make(new weathereffect());

    [Native(Level.Recorded, Reason = "Weather is drawn over a rect; no native reads it back")]
    public void EnableWeatherEffect(weathereffect? whichEffect, bool enable) { }

    [Native(Level.Recorded, Reason = "Weather is drawn over a rect; no native reads it back")]
    public void RemoveWeatherEffect(weathereffect? whichEffect) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public image? CreateImage(string? file, float sizeX, float sizeY, float sizeZ, float posX, float posY, float posZ, float originX, float originY, float originZ, int imageType) => Make(new image());

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void DestroyImage(image? whichImage) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void ShowImage(image? whichImage, bool flag) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageAboveWater(image? whichImage, bool flag, bool useWaterAlpha) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageColor(image? whichImage, int red, int green, int blue, int alpha) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageConstantHeight(image? whichImage, bool flag, float height) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImagePosition(image? whichImage, float x, float y, float z) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageRender(image? whichImage, bool flag) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageRenderAlways(image? whichImage, bool flag) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetImageType(image? whichImage, int imageType) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public ubersplat? CreateUbersplat(float x, float y, string? name, int red, int green, int blue, int alpha, bool forcePaused, bool noBirthTime) => Make(new ubersplat());

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void DestroyUbersplat(ubersplat? whichSplat) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void FinishUbersplat(ubersplat? whichSplat) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void ResetUbersplat(ubersplat? whichSplat) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void ShowUbersplat(ubersplat? whichSplat, bool flag) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetUbersplatRender(ubersplat? whichSplat, bool flag) { }

    [Native(Level.Recorded, Reason = "Images and ubersplats are drawn on the ground; no native reads them back")]
    public void SetUbersplatRenderAlways(ubersplat? whichSplat, bool flag) { }

    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public minimapicon? CreateMinimapIcon(float x, float y, int red, int green, int blue, string? pingPath, fogstate? fogVisibility) => Make(new minimapicon());

    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public minimapicon? CreateMinimapIconAtLoc(location? where, int red, int green, int blue, string? pingPath, fogstate? fogVisibility) => Make(new minimapicon());

    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public minimapicon? CreateMinimapIconOnUnit(unit? whichUnit, int red, int green, int blue, string? pingPath, fogstate? fogVisibility) => Make(new minimapicon());

    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void DestroyMinimapIcon(minimapicon? pingId) { }

    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void SetMinimapIconOrphanDestroy(minimapicon? whichMinimapIcon, bool doDestroy) { }

    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void SetMinimapIconVisible(minimapicon? whichMinimapIcon, bool visible) { }

    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void SetAltMinimapIcon(string? iconPath) { }

    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void PingMinimap(float x, float y, float duration) { }

    [Native(Level.Recorded, Reason = "The minimap is drawn on the screen; no native reads its icons or pings back")]
    public void PingMinimapEx(float x, float y, float duration, int red, int green, int blue, bool extraEffects) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterBlendMode(blendmode? whichMode) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterDuration(float duration) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterEndColor(int red, int green, int blue, int alpha) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterEndUV(float minu, float minv, float maxu, float maxv) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterStartColor(int red, int green, int blue, int alpha) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterStartUV(float minu, float minv, float maxu, float maxv) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterTexMapFlags(texmapflags? whichFlags) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCineFilterTexture(string? filename) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void PlayCinematic(string? movieName) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void PlayModelCinematic(string? modelName) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCinematicAudio(bool cinematicAudio) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetCinematicScene(int portraitUnitId, playercolor? color, string? speakerTitle, string? text, float sceneDuration, float voiceoverDuration) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void EndCinematicScene() { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void ForceCinematicSubtitles(bool flag) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetIntroShotModel(string? introModelPath) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetIntroShotText(string? introText) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void SetPortraitLight(string? portraitDNCFile) { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void ClearTextMessages() { }

    [Native(Level.Recorded, Reason = "The cinematic filter, scenes and the screen's text are drawn; no native reads them back")]
    public void DisplayTimedTextFromPlayer(player? toPlayer, float x, float y, float duration, string? message) { }

    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitAnimation(unit? whichUnit, string? whichAnimation) { }

    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitAnimationByIndex(unit? whichUnit, int whichAnimation) { }

    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitAnimationWithRarity(unit? whichUnit, string? whichAnimation, raritycontrol? rarity) { }

    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void QueueUnitAnimation(unit? whichUnit, string? whichAnimation) { }

    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitBlendTime(unit? whichUnit, float blendTime) { }

    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void AddUnitAnimationProperties(unit? whichUnit, string? animProperties, bool add) { }

    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitTimeScale(unit? whichUnit, float timeScale) { }

    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void SetUnitLookAt(unit? whichUnit, string? whichBone, unit? lookAtTarget, float offsetX, float offsetY, float offsetZ) { }

    [Native(Level.Recorded, Reason = "Animations are drawn on the model; no native reads which one plays back (the durations come with the models)")]
    public void ResetUnitLookAt(unit? whichUnit) { }

    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public void AddIndicator(widget? whichWidget, int red, int green, int blue, int alpha) { }

    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public void UnitAddIndicator(unit? whichUnit, int red, int green, int blue, int alpha) { }

    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public commandbuttoneffect? CreateCommandButtonEffect(int abilityId, string? order) => Make(new commandbuttoneffect());

    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public commandbuttoneffect? CreateUpgradeCommandButtonEffect(int whichUprgade) => Make(new commandbuttoneffect());

    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public commandbuttoneffect? CreateLearnCommandButtonEffect(int abilityId) => Make(new commandbuttoneffect());

    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public void DestroyCommandButtonEffect(commandbuttoneffect? whichEffect) { }

    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public void SetItemColor(item? whichItem, playercolor? whichColor) { }

    [Native(Level.Recorded, Reason = "Indicators, command button effects and tints are drawn; no native reads them back")]
    public void BlzSetItemColor(item? whichItem, playercolor? whichColor) { }

    [Native(Level.Recorded, Reason = "Its hit events need the mouse (wave 6); the handle is made and nothing else")]
    public trackable? CreateTrackable(string? trackableModelPath, float x, float y, float facing) => Make(new trackable());
}
