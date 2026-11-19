using Moonwell;

namespace Gold
{
    /// <summary>The map's settings, and the component the game starts with.</summary>
    public sealed class Settings : Map
    {
        public Settings()
        {
            Name = "Gold";
            Components.Add(new Pay());
        }
    }
}
