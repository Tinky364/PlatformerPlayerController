using Godot;
using PlayerStateMachine;

namespace Other
{
    public class CamController : Camera2D
    {
        [Export]
        private NodePath _playerPath = default;
        [Export(PropertyHint.Range, "0")]
        private float _offsetAcceleration = 2f;
        [Export]
        private bool _smoothing;
        [Export]
        private bool _pixelSmoothing;
        [Export]
        private float _smoothingDur = 0.15f;
        
        private Player _player;
        private Vector2 _destPos;
        private Vector2 _curPos;
        
        public override void _Ready()
        {
            _player = GetNode<Player>(_playerPath);
            _curPos = _player.GlobalPosition;
            if (_smoothing && !_pixelSmoothing) SmoothingEnabled = true;
            else SmoothingEnabled = false;
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_pixelSmoothing) PixelPerfectSmoothing(delta);
            else GlobalPosition = _player.GlobalPosition;
            //SetCamOffset(delta);
            ForceUpdateScroll();
        }

        private void PixelPerfectSmoothing(float delta)
        {
            _destPos = _player.GlobalPosition;
            _curPos += new Vector2(
                _destPos.x - _curPos.x, _destPos.y - _curPos.y) * (1 / _smoothingDur * delta);
            GlobalPosition = _curPos.Round();
        }

        private void SetCamOffset(float delta)
        {
            if (_player.Velocity.x == 0f) return;
            switch (_player.Direction.x)
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
