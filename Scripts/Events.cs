using Godot;
using AI;

public class Events : Node
{
    private static Events _singleton;
    public static Events Singleton => _singleton;

    [Signal]
    private delegate void Damaged(Node2D target, int damageValue, Enemy attacker, Vector2 hitNormal);
    [Signal]
    private delegate void CoinCollected(Node target, int coinValue, Coin coin);
    [Signal]
    private delegate void CoinCountChanged(int newCoinCount);
    [Signal]
    private delegate void PlayerHealthChanged(int newHealth, int maxHealth, Enemy attacker);
    [Signal]
    private delegate void PlayerDied();

    public override void _EnterTree()
    {
        if (_singleton == null) _singleton = this;
        else GD.Print($"Multiple instances of singleton class named {Name}!");
    }
}
