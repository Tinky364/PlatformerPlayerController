using Godot;
using NavTool;

namespace Other
{
    public class Door : Node2D
    {
        private Area2D _triggerArea;
        private NavTween _navTween;
        private StaticBody2D _staticBody;

        private bool _isOpen;

        public override void _Ready()
        {
            _triggerArea = GetNode<Area2D>("TriggerArea");
            _staticBody = GetNode<StaticBody2D>("DoorBody");
            _navTween = new NavTween();
            _staticBody.AddChild(_navTween);
            _navTween.Name = "TweenPhy";
            _navTween.ConnectTween(_staticBody, "constant_linear_velocity");
            _triggerArea.Connect("body_entered", this, nameof(OnTriggerred));
        }

        private void OnTriggerred(Node node)
        {
            if (!(node is Player)) return;
            if (_navTween.IsPlaying) return;
            float targetPosY = _isOpen ? 16 : -16;
            _isOpen = !_isOpen;
            _navTween.MoveLerp(
                NavTween.TweenMode.Y,
                null,
                _staticBody.GlobalPosition + new Vector2(0, targetPosY),
                2f,
                Tween.TransitionType.Cubic
            );
        }

        public override void _PhysicsProcess(float delta)
        {
            if (_navTween.IsPlaying)
            {
                _staticBody.ConstantLinearVelocity = _navTween.EqualizeVelocity(_staticBody.ConstantLinearVelocity, delta);
                _staticBody.GlobalPosition = _navTween.EqualizePosition(_staticBody.GlobalPosition);
            }
        }
    }
}
