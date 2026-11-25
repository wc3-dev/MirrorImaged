namespace MirrorImaged.Natives;

// What main and InitBlizzard set for the view, Recorded as the view isn't simulated, and the fog flags (probe cameras-4).
public sealed partial class GameNatives
{
    const string Cameras4 = "facts, Cameras: probe cameras-4 (11-08)";

    bool _fog = true, _fogMask = true;

    /// <summary>Kept for IsFogEnabled; with the fog off, a place a player has seen reads as visible to them (IsVisibleToPlayer and the like).</summary>
    /// <remarks>Close: Only the flag is kept; what it does to the view isn't modelled or measured.</remarks>
    [Native(Level.Close)]
    public void FogEnable(bool enable) => _fog = enable;

    /// <summary>The flag FogEnable set; on at the start.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Cameras: probe cameras-4 (11-08)).</remarks>
    [Native(Level.Exact, Fact = Cameras4)]
    public bool IsFogEnabled() => _fog;

    /// <summary>Kept for IsFogMaskEnabled; with the mask off, a place a player has never seen reads as fogged to them, or visible with the fog off too.</summary>
    /// <remarks>Close: Only the flag is kept; what it does to the view isn't modelled or measured.</remarks>
    [Native(Level.Close)]
    public void FogMaskEnable(bool enable) => _fogMask = enable;

    /// <summary>The flag FogMaskEnable set; on at the start.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Cameras: probe cameras-4 (11-08)).</remarks>
    [Native(Level.Exact, Fact = Cameras4)]
    public bool IsFogMaskEnabled() => _fogMask;

    /// <summary>Changes nothing; no lighting is changed.</summary>
    /// <remarks>Recorded: The lighting models of the day and night; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The lighting models of the day and night; no native reads them back")]
    public void SetDayNightModels(string? terrainDNCFile, string? unitDNCFile) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void SetTerrainFog(float a, float b, float c, float d, float e) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void SetTerrainFogEx(int style, float zstart, float zend, float density, float red, float green, float blue) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void SetTerrainFogExV(int style, float zstart, float zend, float density, float heightStart, float heightEnd, float linearStart, float linearEnd, float red, float green, float blue) { }

    /// <summary>Changes nothing; no fog is drawn or cleared.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void ResetTerrainFog() { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void SetUnitFog(float a, float b, float c, float d, float e) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogColor(float red, float green, float blue) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogDensity(float density) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogDrawOverSky(bool drawOverSky) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogHeightEnd(float heightEnd) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogHeightStart(float heightStart) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogLinearEnd(float linearEnd) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogLinearStart(float linearStart) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogMaxLinearDensity(float maxLinearDensity) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogStyle(fogstyle? style) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogZEnd(float ZEnd) { }

    /// <summary>Changes nothing; no fog is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void BlzSetTerrainFogZStart(float zStart) { }

    /// <summary>Changes nothing; no fog boundary is drawn.</summary>
    /// <remarks>Recorded: The terrain's fog in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The terrain's fog in the view; no native reads it back")]
    public void EnableWorldFogBoundary(bool b) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void SetWaterBaseColor(int red, int green, int blue, int alpha) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void SetWaterDeforms(bool val) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void SetHDWaterParams(int red, int green, int blue, bool useColor, int vertexDisplacement, int minOpacity, int maxOpacity, int reflectivity, int emissivity, int edgeSoftness, int waveStrength) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void SetHDWaterParamsEx(int red, int green, int blue, bool @override, int vertexDisplacement, int minOpacity, int maxOpacity, int reflectivity, int emissivity, int edgeSoftness, int waveStrength, int envMapStrength) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterColor(int red, int green, int blue) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterColorOverride(bool @override) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterEdgeSoftness(int edgeSoftness) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterEmissivity(int emissivity) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterEnvMapStrength(int envMapStrengthy) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterMaxOpacity(int maxOpacity) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterMinOpacity(int minOpacity) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterReflectivity(int reflectivity) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterVertexDisplacement(int vertexDisplacement) { }

    /// <summary>Changes nothing; no water is drawn.</summary>
    /// <remarks>Recorded: How water is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "How water is drawn; no native reads it back")]
    public void BlzSetHDWaterWaveStrength(int waveStrength) { }

    /// <summary>Changes nothing; no sky is drawn.</summary>
    /// <remarks>Recorded: The sky in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The sky in the view; no native reads it back")]
    public void SetSkyModel(string? skyModelFile) { }

    /// <summary>Changes nothing; no sky box is shown or hidden.</summary>
    /// <remarks>Recorded: The sky in the view; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The sky in the view; no native reads it back")]
    public void BlzShowSkyBox(bool show) { }

    /// <summary>Changes nothing; no terrain is shown or hidden.</summary>
    /// <remarks>Recorded: Whether the terrain is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Whether the terrain is drawn; no native reads it back")]
    public void BlzShowTerrain(bool show) { }
}
