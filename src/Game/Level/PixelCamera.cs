using System;
using Game.Level.Players;
using Godot;
using Game.Level.Players.States;

namespace Game.Level
{
    [Tool]
    public class PixelCamera : Camera2D
    {
        [Export]
        private NodePath _playerPath = default;
        [Export(PropertyHint.Range, "0, 10")]
        private float _offsetSmoothingDur = 2f;
        [Export(PropertyHint.Range, "0, 10")]
        private float _followSmoothingDur = 0.2f;
        [Export]
        private Rect2 _limitRect;
        [Export(PropertyHint.Range, "-1,1")]
        private float _offsetH;
        [Export(PropertyHint.Range, "-1,1")]
        private float _offsetV;
        [Export]
        private bool _smoothFollow;
        [Export]
        private bool _drawDebug;
        
        private Tween _tween;
        private Player _player;
        private Rect2 ScreenRect => new Rect2(
            -_screenSizeHalf.x + OffsetH * _screenSizeHalf.x * DragMarginLeft,
            -_screenSizeHalf.y + OffsetV * _screenSizeHalf.y * DragMarginTop,
            _screenSize
        );
        private Rect2 DragRect => new Rect2(
            -_screenSizeHalf.x * DragMarginLeft + OffsetH * _screenSizeHalf.x * DragMarginLeft,
            -_screenSizeHalf.y * DragMarginTop + OffsetV * _screenSizeHalf.y * DragMarginTop,
            _screenSizeHalf.x * DragMarginLeft + _screenSizeHalf.x * DragMarginRight,
            _screenSizeHalf.y * DragMarginTop + _screenSizeHalf.y * DragMarginBottom
        );
        private Vector2 _followPos = Vector2.Zero;
        private Vector2 _curPos;
        private Vector2 _subPixel = Vector2.Zero;
        private Vector2 _newPos = Vector2.Zero;
        private Vector2 _screenSize;
        private Vector2 _screenSizeHalf;
        private float _preTargetOffsetH;
        
        public PixelCamera Init()
        {
            _tween = new Tween();
            AddChild(_tween);
            _tween.Name = "Tween"; 
            _player = GetNode<Player>(_playerPath);
            _screenSize = App.Singleton.Viewport.Size;
            _screenSizeHalf = _screenSize / 2f;
            CalculateFollowPosition();
            _curPos = Vector2.Zero;
            _newPos = Vector2.Zero;
            GlobalPosition = Vector2.Zero;
            return this;
        }
        
        public override void _Process(float delta)
        {
            Update();            
        }
        
        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint) return;
            
            CalculateCamOffset();
            CalculateFollowPosition();
            PixelFollowSmoothing(delta);
              
            ForceUpdateScroll();
        }
        
        private void CalculateFollowPosition()
        {
            _followPos.y = 
                _player.GlobalPosition.y + _screenSizeHalf.y * _offsetV * DragMarginBottom;
            switch (_player.Direction.x)
            {
                case 1:
                    _followPos.x =
                        _player.GlobalPosition.x + _screenSizeHalf.x * DragMarginLeft * _offsetH;
                    break;
                case -1:
                    _followPos.x =
                        _player.GlobalPosition.x + _screenSizeHalf.x * DragMarginRight * _offsetH;
                    break;
            }
        }
        
        private void PixelFollowSmoothing(float delta)
        {
            if (_smoothFollow)
                _curPos += (_followPos - _curPos) * (1 / _followSmoothingDur * delta);
            else _curPos += _followPos - _curPos;
            
            if (_curPos.x - _screenSizeHalf.x > _limitRect.Position.x && 
                _curPos.x + _screenSizeHalf.x < _limitRect.End.x)
            {
                _subPixel.x = _curPos.Round().x - _curPos.x;
                _newPos.x = _curPos.Round().x;
            }
            if (_curPos.y - _screenSizeHalf.y > _limitRect.Position.y &&
                _curPos.y + _screenSizeHalf.y < _limitRect.End.y)
            {
                _subPixel.y = _curPos.Round().y - _curPos.y;
                _newPos.y = _curPos.Round().y;
            }
            
            if (App.Singleton.ViewportContainer.Material is ShaderMaterial shader)
                shader.SetShaderParam("cam_offset", _subPixel);
            GlobalPosition = _newPos;
        }
        
        private void CalculateCamOffset()
        {
            float targetOffsetH;
            if (_player.MoveDirectionAvg.x < -0.85f) targetOffsetH = -1f;
            else if (_player.MoveDirectionAvg.x > 0.85f) targetOffsetH = 1f;
            else targetOffsetH = 0;
        
            if (Math.Abs(_preTargetOffsetH - targetOffsetH) < 0.1f) return;
            
            var globalDragRect = new Rect2(ToGlobal(DragRect.Position), DragRect.Size);
            if (_player.GlobalPosition.x > globalDragRect.End.x ||
                _player.GlobalPosition.x < globalDragRect.Position.x)
            {
                Vector2 dirToPlayer = GlobalPosition.DirectionTo(_player.GlobalPosition);
                if (dirToPlayer.x < 0 && _player.Direction.x > 0 && _offsetH <= 0.99f) return;
                if (dirToPlayer.x > 0 && _player.Direction.x < 0 && _offsetH >= -0.99f) return;
            }
        
            _preTargetOffsetH = targetOffsetH;
        
            _tween.Stop(this, "_offsetH");
            _tween.InterpolateProperty(
                this, "_offsetH", _offsetH, targetOffsetH, _offsetSmoothingDur,
                Tween.TransitionType.Quad
            );
            _tween.Start();
        }
        
        public override void _Draw()
        {
            if (!_drawDebug) return;
            
            // Limit area
            var localPos = ToLocal(_limitRect.Position);
            var localRect = new Rect2(localPos, _limitRect.Size);
            DrawRect(localRect, Colors.Red, false, 2f);
            
            // Camera area
            DrawRect(ScreenRect, Colors.Purple, false);
            
            // Drag area
            DrawRect(DragRect, Colors.Purple, false);
            
            // direction
            DrawLine(
                ToLocal(_player.GlobalPosition),
                ToLocal(_player.GlobalPosition) + _player.MoveDirectionAvg * 10f,
                Colors.Chocolate
            );
        }
    }
}
