using Godot;

namespace PlatformerPlayerController.Scripts
{
    public class Events : Node
    {
        private static Events _singleton;
        public static Events Singleton => _singleton;

        [Signal]
        private delegate void Damaged(Node target, int damageValue, Node attacker);
        [Signal]
        private delegate void CoinCollected(Node target, int coinValue, Coin coin);
        [Signal]
        private delegate void CoinCountChanged(int newCoinCount);
        [Signal]
        private delegate void HealthChanged(int newHealth, int maxHealth);

        public override void _EnterTree()
        {
            if (_singleton == null) _singleton = this;
            else GD.Print($"Multiple instances of singleton class named {Name}!");
        }
    }
}
