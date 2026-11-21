namespace MirrorImaged.Natives;

// Music and the volume groups: nothing is played, and no native reads any of it back, so they keep nothing (decisions
// D525). GetSoundFileDuration reads a music file's length (Sounds.cs).
public sealed partial class GameNatives
{
    const string NotHeard = "nothing is played; no native reads music or a volume back";

    /// <summary>
    /// A file, or a list of files split by <c>;</c>, to play once, then the map's music. Where the game starts in a list
    /// isn't steady (facts, "Presentation": the first file, then the second and third when played again); nothing here
    /// reads it.
    /// </summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void PlayMusic(string? musicName) { }

    /// <summary>As PlayMusic, from a place and with a fade in.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void PlayMusicEx(string? musicName, int frommsecs, int fadeinmsecs) { }

    /// <summary>Changes nothing; there is no music to stop.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void StopMusic(bool fadeOut) { }

    /// <summary>Changes nothing; no music is resumed.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void ResumeMusic() { }

    /// <summary>Changes nothing; the map's music list is not kept.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void SetMapMusic(string? musicName, bool random, int index) { }

    /// <summary>Changes nothing; the map's music is not changed.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void ClearMapMusic() { }

    /// <summary>Changes nothing; the volume is not kept.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void SetMusicVolume(int volume) { }

    /// <summary>Changes nothing; the position is not kept.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void SetMusicPlayPosition(int millisecs) { }

    /// <summary>Changes nothing; nothing is played.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void PlayThematicMusic(string? musicFileName) { }

    /// <summary>Changes nothing; nothing is played.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void PlayThematicMusicEx(string? musicFileName, int frommsecs) { }

    /// <summary>Changes nothing; there is no music to end.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void EndThematicMusic() { }

    /// <summary>Changes nothing; the volume is not kept.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void SetThematicMusicVolume(int volume) { }

    /// <summary>Changes nothing; the position is not kept.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void SetThematicMusicPlayPosition(int millisecs) { }

    /// <summary>Changes nothing; the volume is not kept.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void BlzSetThematicMusicAbsoluteVolume(int volume) { }

    /// <summary>Changes nothing; the setting is not kept.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void BlzPauseThematicMusicOnFocusLost(bool pause) { }

    /// <summary>Changes nothing; the group's volume is not kept.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void VolumeGroupSetVolume(volumegroup? vgroup, float scale) { }

    /// <summary>Changes nothing; no volume changes.</summary>
    /// <remarks>Recorded: nothing is played; no native reads music or a volume back.</remarks>
    [Native(Level.Recorded, Reason = NotHeard)]
    public void VolumeGroupReset() { }
}
