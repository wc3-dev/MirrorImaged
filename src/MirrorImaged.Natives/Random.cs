namespace MirrorImaged.Natives;

// The game's generator isn't known, so its numbers aren't the game's; a seed gives the same numbers every run. Lua's
// math.random and math.randomseed share it, as the game's do (facts, "Lua runtime").
public sealed partial class GameNatives
{
    /// <summary>A random integer from the first bound up to the bounds' distance above it, both included: GetRandomInt(5, 1) is from 5 to 9.</summary>
    /// <remarks>Close: The game's generator isn't known, so its numbers aren't the game's; the same seed gives the same ones every run.</remarks>
    [Native(Level.Close)]
    public int GetRandomInt(int lowBound, int highBound) => _game.RandomInt(lowBound, highBound);

    /// <summary>A random number from the first bound up to the bounds' distance above it: GetRandomReal(2, 1) is from 2 to 3.</summary>
    /// <remarks>Close: The game's generator isn't known, so its numbers aren't the game's; the same seed gives the same ones every run.</remarks>
    [Native(Level.Close)]
    public float GetRandomReal(float lowBound, float highBound) => _game.RandomReal(lowBound, highBound);

    /// <summary>Starts the random numbers over; the same seed gives the same numbers.</summary>
    /// <remarks>Close: The game's generator isn't known, so a seed doesn't give the game's numbers.</remarks>
    [Native(Level.Close)]
    public void SetRandomSeed(int seed) => _game.Seed(seed);
}
