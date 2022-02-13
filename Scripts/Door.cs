using Godot;
using NavTool;

public class Door : StaticBody2D
{
    private Area2D _triggerArea;
    public NavTween NavTween { get; private set; }

    public override void _EnterTree()
    {
        NavTween = new NavTween();
        AddChild(NavTween);
        NavTween.Name = "TweenPhy";
    }
    
    public override void _Ready()
    {
        _triggerArea = GetNode<Area2D>("../TriggerArea");
        _triggerArea.Connect("body_entered", this, nameof(OnTriggerred));
    }

    private void OnTriggerred(Node node)
    {
        if (!(node is Player)) return;
        if (NavTween.IsLerping) return;
        NavTween.MoveLerp(NavTween.LerpingMode.Vector2, GlobalPosition - new Vector2(0, 16), 2f);
    }
    
    public override void _PhysicsProcess(float delta)
    {
        if (NavTween.IsLerping)
        {
            Vector2 dummy = ConstantLinearVelocity;
            NavTween.EqualizeVelocity(ref dummy);
            ConstantLinearVelocity = dummy;
            dummy = GlobalPosition;
            NavTween.EqualizePosition(ref dummy);
            GlobalPosition = dummy;
        }
        else
        {
            ConstantLinearVelocity = Vector2.Zero;
        }
    }
}
