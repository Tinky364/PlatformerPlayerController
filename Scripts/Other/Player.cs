using System.Threading.Tasks;
using Godot;
using Manager;
using NavTool;

namespace Other
{
    public class Player : PlayerController
    {
        [Export(PropertyHint.Range, "0,10,or_greater")]
        private int _maxHealth = 6;
        [Export(PropertyHint.Range, "0,3,or_greater")]
        private float _unhurtableDur = 1f;
        [Export(PropertyHint.Range, "0,3,or_greater")]
        private float _inputLockDurOnDmg = 0.5f;

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

        public override void _EnterTree()
        {
            base._EnterTree();
            AddToGroup("Player");
        }

        public override void _Ready()
        {
            base._Ready();
            Health = _maxHealth;
            Events.Singleton.Connect("Damaged", this, nameof(OnDamaged));
            Events.Singleton.Connect("CoinCollected", this, nameof(AddCoin));
        }

        public void OnDamaged(NavBody2D target, int damageValue, NavBody2D attacker, Vector2 hitNormal)
        {
            if (target != this) return;
            if (IsUnhurtable) return;
            IsUnhurtable = true;
            Health -= damageValue;
            Events.Singleton.EmitSignal("PlayerHealthChanged", Health, _maxHealth, attacker);
            if (Health == 0)
            {
                OnDie(hitNormal);
                Events.Singleton.EmitSignal("PlayerDied");
            }
            else
            {
                OnHit(hitNormal);
            }
        }

        private async void OnHit(Vector2 hitNormal)
        {
            SetRecoil(hitNormal);
            LockInputs(true);
            LockInputs(false, _inputLockDurOnDmg);
            IsUnhurtable = true;
            await WhileUnhurtable();
            IsUnhurtable = false;
        }
        
        private async void OnDie(Vector2 hitNormal)
        {
            SetRecoil(hitNormal);
            LockInputs(true);
            IsUnhurtable = true;
            await WhileUnhurtable();
            await ToSignal(GetTree().CreateTimer(2f), "timeout");
            IsInactive = true;
        }

        private async Task WhileUnhurtable()
        {
            float count = 0f;
            while (count < _unhurtableDur)
            {
                AnimSprite.SelfModulate = 
                    AnimSprite.SelfModulate == Colors.Red ? Colors.White : Colors.Red;
                float t = count / _unhurtableDur;
                t = 1 - Mathf.Pow(1 - t, 5);
                float waitTime = Mathf.Lerp(0.01f, 0.2f, t);
                count += waitTime;
                await ToSignal(GetTree().CreateTimer(waitTime), "timeout");
            }
            AnimSprite.SelfModulate = Colors.White;
        }

        private void AddCoin(Node collector, Coin coin)
        {
            if (collector != this) return;
            CoinCount += coin.Value;
            Events.Singleton.EmitSignal("PlayerCoinCountChanged", CoinCount);
        }
    }
}
