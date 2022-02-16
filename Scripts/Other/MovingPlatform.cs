using Godot;
using System;
using NavTool;

namespace Other
{
    public class MovingPlatform : Node2D
    {
        private Line2D _line;
        private StaticBody2D _body;
        private NavTween _navTween;

        [Export]
        private float _idleDur = 2f;
        [Export]
        private float _speed = 20f;
        [Export]
        private bool _repeat = true;
    
        public override void _Ready()
        {
            _body = GetNode<StaticBody2D>("StaticBody2D");
            _line = GetNode<Line2D>("Line2D");
            _navTween = new NavTween();
            _body.AddChild(_navTween);
            _navTween.Name = "NavTween";
            _navTween.ConnectTween(_body, "constant_linear_velocity");
            Loop();
        }

        private async void Loop()
        {
            int pointIndex = 0;
            int pointsCount = _line.Points.Length;
            int increment = 1;
            while (pointIndex >= 0 && pointIndex <= pointsCount - 1)
            {
                _navTween.MoveToward(
                    NavTween.TweenMode.Vector2,
                    _line.ToGlobal(_line.Points[pointIndex]),
                    _line.ToGlobal(_line.Points[pointIndex + increment]),
                    _speed,
                    Tween.TransitionType.Cubic
                );
                await ToSignal(_navTween, "MoveCompleted");
                await ToSignal(GetTree().CreateTimer(_idleDur), "timeout");
                pointIndex += increment;
                if (pointIndex == pointsCount - 1 || pointIndex == 0)
                    increment *= -1;
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_navTween.IsPlaying)
            {
                _body.ConstantLinearVelocity = _navTween.EqualizeVelocity(_body.ConstantLinearVelocity, delta);
                _body.GlobalPosition = _navTween.EqualizePosition(_body.GlobalPosition);
            }
        }
    }
}

