using Godot;

namespace Other
{
    public class CamController : Camera2D
    {
        [Export]
        private NodePath _playerPath;
        [Export(PropertyHint.Range, "0")]
        private float _offsetAcceleration = 2f;
        
        private Player _player;

        public override void _Ready()
        {
            _player = GetNode<Player>(_playerPath);
            SetCamPosition(_player.Position);
        }

        public override void _PhysicsProcess(float delta)
        {
            SetCamPosition(_player.Position);
            SetCamOffset(delta);
        }

        private void SetCamPosition(Vector2 targetPos)
        {
            Position = targetPos;
        }

        private void SetCamOffset(float delta)
        {
            if (_player.Velocity.x == 0f) return;
            switch (_player.Direction)
            {
                case 1:
                    OffsetH = Mathf.MoveToward(OffsetH, 1f, _offsetAcceleration * delta);
                    break;
                case -1:
                    OffsetH = Mathf.MoveToward(OffsetH, -1f, _offsetAcceleration * delta);
                    break;
            }
        }
    }
}
