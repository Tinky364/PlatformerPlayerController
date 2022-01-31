using Godot;

namespace PlatformerPlayerController.Scripts
{
    public class Player : PlayerController
    {
        [Signal]
        private delegate void CoinCountChanged(int coinCount);

        [Signal]
        private delegate void PlayerTookDamage(int health);

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
        public int CoinCount { get; private set; }= 0;

        public void OnTookDamage()
        {
            Health -= 1;
            EmitSignal(nameof(PlayerTookDamage), Health);
        }

        public void AddCoin(int addCoinCount)
        {
            GD.Print($"{addCoinCount} coin is added.");
            CoinCount += addCoinCount;
            EmitSignal(nameof(CoinCountChanged), CoinCount);
        }
    }
}
