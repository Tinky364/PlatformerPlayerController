using Godot;
using NavTool;

public class TopDown : Area2D
{
    public NavTween NavTween { get; private set; }
    
    public override void _EnterTree()
    {
        NavTween = new NavTween();
        AddChild(NavTween);
        NavTween.Name = "NavTween";
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);
        
        if (Input.IsActionJustPressed("mouse_left_click"))
        {
            GD.Print("TargetPos: " + GetTree().Root.GetMousePosition());
            GD.Print("leftclicked");
            if (NavTween.IsLerping)
            {
                NavTween.StopLerp();
                NavTween.MoveLerpWithSpeed(
                    NavTween.LerpingMode.Vector2,
                    GetTree().Root.GetMousePosition(),
                    200f
                );
            }
            else
            {
                NavTween.MoveLerpWithSpeed(
                    NavTween.LerpingMode.Vector2,
                    GetTree().Root.GetMousePosition(),
                    200f
                );
            }
        }

        if (Input.IsActionJustPressed("mouse_right_click"))
        {
            GD.Print("rightclicked");
            if (NavTween.IsLerping)
                NavTween.StopLerp();
        }

        if (NavTween.IsLerping)
        {
            Vector2 dummy = GlobalPosition;
            NavTween.EqualizePosition(ref dummy);
            GlobalPosition = dummy;
        }
    }
}
