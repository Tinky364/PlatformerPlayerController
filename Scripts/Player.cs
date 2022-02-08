using Godot;

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
    private bool _isUnhurtable;

    public override void _Ready()
    {
        base._Ready();
        
        Health = _maxHealth;
        
        Events.Singleton.Connect("Damaged", this, nameof(OnDamaged));
        Events.Singleton.Connect("CoinCollected", this, nameof(AddCoin));
    }

    public void OnDamaged(Node target, int damageValue, Node attacker, Vector2 hitNormal)
    {
        if (_isUnhurtable) return;
        
        LockInputs(true);
        SetRecoil(true, hitNormal);
        UnhurtableDuration();
        GD.Print($"{target.Name} was damaged value {damageValue} by {attacker.Name}.");
        Health -= damageValue;
        Events.Singleton.EmitSignal("HealthChanged", Health, _maxHealth);
    }

    private async void UnhurtableDuration()
    {
        _isUnhurtable = true;
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
        _isUnhurtable = false;
    }

    private void AddCoin(Node target, int coinValue, Coin coin)
    {
        GD.Print($"{coinValue} coin was added.");
        CoinCount += coinValue;
        Events.Singleton.EmitSignal("CoinCountChanged", CoinCount);
    }
}
