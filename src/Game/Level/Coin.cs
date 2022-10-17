using Godot;
using Game.Service;

namespace Game.Level
{
    public class Coin : Area2D
    {
        [Export(PropertyHint.Range, "0,10,or_greater")]
        public int Value { get; private set; } = 1;

        private AnimationPlayer _animationPlayer;
        private CollisionShape2D _shape;

        public Coin Init()
        {
            _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            _shape = GetNode<CollisionShape2D>("Shape");
            Connect("body_entered", this, nameof(OnBodyEntered));
            _animationPlayer.Connect("animation_finished", this, nameof(OnAnimationFinished));
            _animationPlayer.Play("flip");
            return this;
        }
        
        private void OnBodyEntered(Node body)
        {
            Events.Singleton.EmitSignal(nameof(Events.CoinCollected), body, this);
            _animationPlayer.Stop();
            _animationPlayer.Play("collect");
            _shape.SetDeferred("disabled", true);
        }

        private void OnAnimationFinished(string animName)
        {
            if (animName.Equals("collect")) QueueFree();
        }
    }
}
