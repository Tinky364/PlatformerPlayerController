using Godot;
using NavTool;
using PlayerStateMachine;

namespace Other
{
    public class Door : Node2D
    {
        [Export]
        private int _moveDistance;
        
        private Area2D _triggerArea;
        private StaticBody2D _body;
        private NavTween _navTween;

        private bool _isOpen;

        public override void _Ready()
        {
            _triggerArea = GetNode<Area2D>("TriggerArea");
            _body = GetNode<StaticBody2D>("DoorBody");
            _navTween = new NavTween();
            _body.AddChild(_navTween);
            _navTween.Name = "NavTween";
            _navTween.ConnectTween(_body, "constant_linear_velocity");
            _triggerArea.Connect("body_entered", this, nameof(OnTriggerred));
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_navTween.IsPlaying)
            {
                _body.ConstantLinearVelocity = _navTween.EqualizeVelocity(
                    _body.ConstantLinearVelocity, delta
                );
                _body.GlobalPosition = _navTween.EqualizePosition(_body.GlobalPosition);
            }
        }
        
        private void OnTriggerred(Node node)
        {
            if (!(node is Player)) return;
            if (_navTween.IsPlaying) return;
            float targetPosY = _isOpen ? -_moveDistance : _moveDistance;
            _isOpen = !_isOpen;
            _navTween.MoveLerp(
                NavTween.TweenMode.Y, null, _body.GlobalPosition + new Vector2(0, targetPosY), 2f,
                Tween.TransitionType.Cubic
            );
        }
    }
}
