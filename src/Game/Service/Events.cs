using Game.Abstraction;
using Godot;
using NavTool;
using Game.Level;

namespace Game.Service
{
    public class Events : Node, ISingleton
    {
        public static Events Singleton { get; private set; }
        
        [Signal]
        public delegate void Damaged(NavBody2D target, int damageValue, NavBody2D attacker, Vector2 hitNormal);
        [Signal]
        public delegate void CoinCollected(Node collector, Coin coin);

        public Events Init()
        {
            Singleton = this;
            return this;
        }
    }
}
