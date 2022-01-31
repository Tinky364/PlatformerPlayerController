using Godot;

namespace PlatformerPlayerController.Scripts
{
    public class Coin : Area2D
    {
        private AnimationPlayer _animationPlayer;
        private CollisionShape2D _shape;
        
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
            if (body is Player player)
            {
                player.AddCoin(1);
                _animationPlayer.Stop();
                _animationPlayer.Play("collect");
                _shape.SetDeferred("disabled", true);
            }
        }

        private void OnAnimationFinished(string animName)
        {
            if (animName.Equals("collect"))
            {
                QueueFree();
            }
        }
    }
}
