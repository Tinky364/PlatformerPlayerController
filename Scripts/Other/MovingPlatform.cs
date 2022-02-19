using Godot;
using Manager;
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
    
        public override void _Ready()
        {
            _body = GetNode<StaticBody2D>("StaticBody2D");
            _line = GetNode<Line2D>("Line2D");
            _navTween = new NavTween();
            _body.AddChild(_navTween);
            _navTween.Name = "NavTween";
            _navTween.ConnectTween(_body, "constant_linear_velocity");
            Loop(0, 1);
        }
        
        private async void Loop(int point, int add)
        {
            while (true)
            {
                if (point + add == -1 || point + add == _line.Points.Length) add *= -1;
                _navTween.MoveToward(
                    NavTween.TweenMode.Vector2,
                    _line.ToGlobal(_line.Points[point]),
                    _line.ToGlobal(_line.Points[point + add]),
                    _speed, Tween.TransitionType.Cubic
                );
                await ToSignal(_navTween, "MoveCompleted");
                await TreeTimer.S.Wait(_idleDur);
                if (!IsInstanceValid(this)) return;
                point += add;
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

