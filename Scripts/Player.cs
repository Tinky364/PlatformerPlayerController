using Godot;
using System;

public class Player : PlayerController
{
    [Signal]
    private delegate void CoinCountChanged(int coinCount);
    
    private int _coinCount = 0;
    
    public void AddCoin(int addCoinCount)
    {
        GD.Print($"{addCoinCount} coin is added.");
        _coinCount += addCoinCount;
        EmitSignal(nameof(CoinCountChanged), _coinCount);
    }
}
