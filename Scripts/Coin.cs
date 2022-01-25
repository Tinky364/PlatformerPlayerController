using Godot;
using System;

public class Coin : Area2D
{
    private void OnCoinBodyEntered(Node body)
    {
        if (body is Player player)
        {
            player.AddCoin(1);
        }
        QueueFree();
    }
}
