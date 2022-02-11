using Godot;
using NavTool;

[Tool]
public class Player : PlayerController
{
    [Export(PropertyHint.Range, "0,10,or_greater")]
    private int _maxHealth = 6;
    [Export(PropertyHint.Range, "0,3,or_greater")]
    private float _unhurtableDur = 0.55f;
    
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
        if (Engine.EditorHint) return;

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
            OnDie();
            SetRecoil(true, hitNormal);
            Events.Singleton.EmitSignal("PlayerDied");
        }
        else
        {
            LockInputs(true);
            SetRecoil(true, hitNormal);
            UnhurtableDuration();
        }
    }

    protected override void OnDie()
    {
        IsInactive = true;
        IsUnhurtable = true;
        base.OnDie();
    }

    private async void UnhurtableDuration()
    {
        IsUnhurtable = true;
        float count = 0f;
        while (count < _unhurtableDur)
        {
            if (count > RecoilDur) LockInputs(false);
            AnimSprite.SelfModulate = AnimSprite.SelfModulate == Colors.Red ? Colors.White : Colors.Red;
            
            float t = count / _unhurtableDur;
            t = 1 - Mathf.Pow(1 - t, 5);
            float waitTime = Mathf.Lerp(0.01f, 0.2f, t);
            count += waitTime;
            await ToSignal(GetTree().CreateTimer(waitTime), "timeout");
        }
        AnimSprite.SelfModulate = Colors.White;
        IsUnhurtable = false;
        SetRecoil(false, null);
    }

    private void AddCoin(Node target, int coinValue, Coin coin)
    {
        GD.Print($"{coinValue} coin was added.");
        CoinCount += coinValue;
        Events.Singleton.EmitSignal("CoinCountChanged", CoinCount);
    }
}
