using Godot;
using NavTool;

public class Door : StaticBody2D
{
    private Area2D _triggerArea;
    private NavTween _navTween;

    private bool _isOpen;

    public override void _EnterTree()
    {
        _navTween = new NavTween();
        AddChild(_navTween);
        _navTween.Name = "TweenPhy";
    }
    
    public override void _Ready()
    {
        _triggerArea = GetNode<Area2D>("../TriggerArea");
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
            GlobalPosition + new Vector2(0, targetPosY), 2f,
            Tween.TransitionType.Cubic
        );
    }
    
    public override void _PhysicsProcess(float delta)
    {
        if (_navTween.IsPlaying)
        {
            ConstantLinearVelocity = _navTween.EqualizeVelocity(ConstantLinearVelocity);
            GlobalPosition = _navTween.EqualizePosition(GlobalPosition);
        }
        else
        {
            ConstantLinearVelocity = Vector2.Zero;
        }
    }
}
