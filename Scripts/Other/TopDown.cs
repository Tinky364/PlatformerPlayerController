using Godot;
using NavTool;

namespace Other
{
    public class TopDown : Area2D
    {
        public NavArea2D NavArea { get; private set; }
        public NavTween NavTween { get; private set; }
        
        [Export]
        private NodePath _navAreaPath = default;
        
        public override void _EnterTree()
        {
            NavTween = new NavTween();
            AddChild(NavTween);
            NavTween.Name = "NavTween";
            NavTween.ConnectTween(this);
        }
    
        public override void _Ready()
        {
            if (_navAreaPath != null) 
                NavArea = GetNodeOrNull<NavArea2D>(_navAreaPath);
            if (NavArea != null && !NavArea.IsPositionInArea(GlobalPosition))
                GlobalPosition = NavArea.GlobalPosition;
        }
    
        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            
            if (Input.IsActionJustPressed("mouse_left_click"))
            {
                GD.Print("left clicked");
                Vector2 targetPos = GetTree().Root.GetMousePosition();
                if (!NavArea.IsPositionInArea(targetPos)) return;
                GD.Print("TargetPos: " + targetPos);
                if (NavTween.IsPlaying)
                {
                    NavTween.StopMove();
                    NavTween.MoveToward(
                        NavTween.TweenMode.Vector2, null, targetPos, 100f, Tween.TransitionType.Cubic
                    );
                }
                else
                {
                    NavTween.MoveToward(
                        NavTween.TweenMode.Vector2, null, targetPos, 100f, Tween.TransitionType.Cubic
                    );
                }
            }
    
            if (Input.IsActionJustPressed("mouse_right_click"))
            {
                GD.Print("right clicked");
                if (NavTween.IsPlaying)
                    NavTween.StopMove();
            }
    
            if (NavTween.IsPlaying)
            {
                GlobalPosition = NavTween.EqualizePosition(GlobalPosition);
            }
        }
    }
}
