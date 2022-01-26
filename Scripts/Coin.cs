using Godot;

namespace PlatformerPlayerController.Scripts
{
    public class Coin : Area2D
    {
        private AnimationPlayer _animationPlayer;
        private CollisionShape2D _collisionShape;
        
        public override void _Ready()
        {
            _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
            _animationPlayer.Play("flip");
        }

        private void OnCoinBodyEntered(Node body)
        {
            if (body is Player player)
            {
                player.AddCoin(1);
                _animationPlayer.Stop();
                _animationPlayer.Play("collect");
                _collisionShape.SetDeferred("disabled", true);
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
