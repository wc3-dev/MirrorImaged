namespace MirrorImaged;

/// <summary>
/// The player's graphics mode, chosen in the game's options (War3Preferences <c>hd</c>): the game's data answers follow
/// it, not the map (decisions D680).
/// </summary>
public enum GraphicsMode
{
    /// <summary>Reforged graphics (<c>hd=2</c>), the default.</summary>
    Reforged,

    /// <summary>Classic graphics (<c>hd=0</c>).</summary>
    Classic,
}
