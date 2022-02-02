using Godot;

namespace PlatformerPlayerController.Scripts
{
    public class Player : PlayerController
    {
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private int _maxHealth = 3;
        
        private int _health;
        public int Health
        {
            get => _health;
            private set
            {
                if (value < 0) 
                    _health = 0;
                else if (value > _maxHealth)
                    _health = _maxHealth;
                else
                    _health = value;
            }
        } 
        public int CoinCount { get; private set; } = 0;

        public override void _Ready()
        {
            base._Ready();

            Events.Singleton.Connect("Damaged", this, nameof(OnDamaged));
            Events.Singleton.Connect("CoinCollected", this, nameof(AddCoin));
        }

        public void OnDamaged(Node target, int damageValue, Node attacker)
        {
            GD.Print($"{target.Name} was damaged by {attacker.Name}.");
            Health -= damageValue;
        }

        public void AddCoin(Node target, int coinValue, Coin coin)
        {
            GD.Print($"{coinValue} coin was added.");
            CoinCount += coinValue;
        }
    }
}
