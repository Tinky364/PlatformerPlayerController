using Godot;
using Manager;
using PlayerStateMachine;

namespace Other
{
    [Tool]
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
        [Export]
        private Rect2 _limits;
        [Export]
        private Vector2 _screenSize;
        
        private enum SmoothType { Pixel, Normal, None }
        private Player _player;
        private Vector2 _destinationPos;
        private Vector2 _curPos;
        private Vector2 _subPixel = Vector2.Zero;
        private Vector2 _newPos = Vector2.Zero;
        
        public override void _Ready()
        {
            if (Engine.EditorHint) return;
            SmoothingEnabled = _curSmoothType == SmoothType.Normal;

            _player = GetNode<Player>(_playerPath);
            GlobalPosition = _player.GlobalPosition;
        }

        public override void _Process(float delta)
        {
            Update();            
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint) return;
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
            //SetCamOffset(delta);
            ForceUpdateScroll();
        }

        private void PixelSmoothing(float delta)
        {
            _destinationPos = _player.GlobalPosition;
            if (_destinationPos.x - 160 > _limits.Position.x && 
                _destinationPos.x + 160 < _limits.End.x)
            {
                _curPos.x += (_destinationPos.x - _curPos.x) * (1 / _smoothingDur * delta);
                _subPixel.x = _curPos.Round().x - _curPos.x;
                _newPos.x = _curPos.Round().x;
            }
            else
            {
                _subPixel.x = 0;
                _newPos.x = _limits.Position.x + _screenSize.x / 2;
            }
            if (_destinationPos.y - 90 > _limits.Position.y &&
                _destinationPos.y + 90 < _limits.End.y)
            {
                _curPos.y += (_destinationPos.y - _curPos.y) * (1 / _smoothingDur * delta);
                _subPixel.y = _curPos.Round().y - _curPos.y;
                _newPos.y = _curPos.Round().y;
            }
            else
            {
                _subPixel.y = 0;
                _newPos.y = _limits.Position.y + _screenSize.y / 2;
            }
            if (GM.S.CurrentScene.World.Material is ShaderMaterial shader)
                shader.SetShaderParam("cam_offset", _subPixel);
            GlobalPosition = _newPos;
        }

        public override void _Draw()
        {
            // Limits
            var localPos = ToLocal(_limits.Position);
            var localRect = new Rect2(localPos, _limits.Size);
            DrawRect(localRect, Colors.Red, false, 2f);
            
            // Camera Area
            DrawRect(
                new Rect2(-_screenSize.x / 2, -_screenSize.y / 2, _screenSize), Colors.Purple,
                false, 2f
            );
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
