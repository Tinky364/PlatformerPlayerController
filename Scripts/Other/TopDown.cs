using Godot;
using Manager;
using NavTool;

namespace Other
{
    public class TopDown : Area2D
    {
        [Export]
        private NodePath _navAreaPath = default;
        
        public NavArea2D NavArea { get; private set; }
        public NavTween NavTween { get; private set; }
        
        public override void _EnterTree()
        {
            NavTween = new NavTween();
            AddChild(NavTween);
            NavTween.Name = "NavTween";
            NavTween.ConnectTween(this);
        }
    
        public override void _Ready()
        {
            if (_navAreaPath != null) NavArea = GetNodeOrNull<NavArea2D>(_navAreaPath);
            if (NavArea != null && !NavArea.IsPositionInArea(GlobalPosition))
                GlobalPosition = NavArea.GlobalPosition;
        }

        public override void _PhysicsProcess(float delta)
        {
            MoveMouseClickPos();

            if (NavTween.IsPlaying) GlobalPosition = NavTween.EqualizePosition(GlobalPosition);
        }
        
        private void MoveMouseClickPos()
        {
            if (InputManager.IsJustPressed("mouse_left_click"))
            {
                Vector2 targetPos = GetTree().Root.GetMousePosition();
                if (NavArea != null && !NavArea.IsPositionInArea(targetPos)) return;
                GD.Print("TargetPos: " + targetPos);
                if (NavTween.IsPlaying)
                {
                    NavTween.StopMove();
                    NavTween.MoveToward(
                        NavTween.TweenMode.Vector2, null, targetPos, 100f,
                        Tween.TransitionType.Cubic, Tween.EaseType.Out
                    );
                }
                else
                {
                    NavTween.MoveToward(
                        NavTween.TweenMode.Vector2, null, targetPos, 100f,
                        Tween.TransitionType.Cubic, Tween.EaseType.Out
                    );
                }
            }

            if (InputManager.IsJustPressed("mouse_right_click") && NavTween.IsPlaying)
                NavTween.StopMove();
        }
    }
}

