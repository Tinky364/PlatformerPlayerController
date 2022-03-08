using Godot;
using Manager;
using PlayerStateMachine;

namespace Other
{
    public class CamController : Camera2D
    {
        [Export]
        private SmoothType _curSmoothType;
        [Export]
        private NodePath _playerPath = default;
        [Export(PropertyHint.Range, "0")]
        private float _offsetAcceleration = 2f;
        [Export]
        private float _smoothingDur = 0.2f;
        
        private enum SmoothType { Pixel, Normal, None }
        private Player _player;
        private Vector2 _destinationPos;
        private Vector2 _curPos;
        
        public override void _Ready()
        {
            SmoothingEnabled = _curSmoothType == SmoothType.Normal;

            _player = GetNode<Player>(_playerPath);
        }

        public override void _PhysicsProcess(float delta)
        {
            switch (_curSmoothType)
            {
                case SmoothType.Pixel: 
                    PixelSmoothing(delta);
                    break;
                case SmoothType.Normal:
                    GlobalPosition = _player.GlobalPosition;
                    break;
                case SmoothType.None:
                    GlobalPosition = _player.GlobalPosition;
                    break;
            }
            SetCamOffset(delta);
            ForceUpdateScroll();
        }

        //TODO arrange limits
        private void PixelSmoothing(float delta)
        {
            _destinationPos = _player.GlobalPosition;
            if (_destinationPos.x - 160 > LimitLeft && _destinationPos.x + 160 < LimitRight)
            {
                _curPos.x += (_destinationPos.x - _curPos.x) * (1 / _smoothingDur * delta);
            }
            if (_destinationPos.y - 90 > LimitTop && _destinationPos.y + 90 < LimitBottom)
            {
                _curPos.y += (_destinationPos.y - _curPos.y) * (1 / _smoothingDur * delta);
            }
            var subPixel = _curPos.Round() - _curPos;
            if (GM.S.CurrentScene.World.Material is ShaderMaterial shader)
                shader.SetShaderParam("cam_offset", subPixel.Clamped(2f));
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
