using Moonwell;

namespace Gold
{
    /// <summary>Pays the first player 10 gold every 2 seconds, and shows their gold when they type -purse.</summary>
    public sealed class Pay : GameComponent
    {
        public override void OnGameStart()
        {
            Every(2f, () => Player.One.Gold += 10);
            Command("purse", (player, rest) => Game.Print("gold " + player.Gold));
        }
    }
}
