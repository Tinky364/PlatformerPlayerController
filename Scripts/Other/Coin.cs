using Godot;
using Manager;

namespace Other
{
    public class Coin : Area2D
    {
        private AnimationPlayer _animationPlayer;
        private CollisionShape2D _shape;

        [Export(PropertyHint.Range, "0,10,or_greater")]
        public int Value { get; private set; } = 1;

        public override void _Ready()
        {
            _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            _shape = GetNode<CollisionShape2D>("Shape");

            Connect("body_entered", this, nameof(OnBodyEntered));
            _animationPlayer.Connect("animation_finished", this, nameof(OnAnimationFinished));

            _animationPlayer.Play("flip");
        }

        private void OnBodyEntered(Node body)
        {
            Events.S.EmitSignal("CoinCollected", body, this);
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
